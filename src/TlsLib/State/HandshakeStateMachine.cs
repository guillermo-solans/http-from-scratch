using System.Security.Cryptography.X509Certificates;
using TlsLib.Crypto;
using TlsLib.Handshake;
using TlsLib.Protocol;
using TlsLib.Records;
using TlsLib.Util;

namespace TlsLib.State;

internal static class HandshakeStateMachine
{
    public static TlsConnectionState PerformClientHandshake(
        Stream stream,
        string serverName,
        Action<string>? logger,
        Func<X509Certificate2, bool>? certificateValidator,
        bool allowInvalidCertificates)
    {
        using var ctx = new HandshakeContext();
        var hsReader = new HandshakeReader(stream);

        // 1. Send ClientHello
        ctx.ClientRandom = SecureRandom.GenerateClientRandom();
        ctx.LocalEcdhe = new EcdheP256();

        var clientHello = new ClientHello
        {
            ClientRandom = ctx.ClientRandom,
            CipherSuites = new[] { CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256 },
            ServerName = serverName
        };
        SendHandshake(stream, ctx, HandshakeType.ClientHello, clientHello.Serialize(), logger, "ClientHello");

        // 2. Receive ServerHello
        var sh = ReceiveHandshake(hsReader, ctx, HandshakeType.ServerHello, logger, "ServerHello");
        var serverHello = ServerHello.Parse(sh);
        if (serverHello.CipherSuite != CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256)
            throw new TlsException(
                $"Server selected unsupported cipher suite 0x{serverHello.CipherSuite:X4}",
                AlertDescription.HandshakeFailure);
        if (serverHello.CompressionMethod != TlsConstants.CompressionMethodNull)
            throw new TlsException(
                "Server selected non-null compression",
                AlertDescription.HandshakeFailure);
        ctx.ServerRandom = serverHello.ServerRandom;

        // 3. Receive Certificate
        var certBytes = ReceiveHandshake(hsReader, ctx, HandshakeType.Certificate, logger, "Certificate");
        var certMsg = CertificateMessage.Parse(certBytes);
        ctx.ServerCertificate = certMsg.GetEndEntityCertificate();
        ValidateServerCertificate(ctx.ServerCertificate, certificateValidator, allowInvalidCertificates);

        // 4. Receive ServerKeyExchange and verify signature
        var skeBytes = ReceiveHandshake(hsReader, ctx, HandshakeType.ServerKeyExchange, logger, "ServerKeyExchange");
        var ske = ServerKeyExchange.Parse(skeBytes);
        if (!ske.VerifySignature(ctx.ClientRandom, ctx.ServerRandom, ctx.ServerCertificate))
            throw new TlsException(
                "Server signature on ECDHE parameters is invalid",
                AlertDescription.DecryptError);
        ctx.PeerPublicKey = ske.EcPointUncompressed;

        // 5. Receive ServerHelloDone
        var shdBytes = ReceiveHandshake(hsReader, ctx, HandshakeType.ServerHelloDone, logger, "ServerHelloDone");
        ServerHelloDone.Parse(shdBytes);

        // 6. Send ClientKeyExchange
        var cke = new ClientKeyExchange { EcPointUncompressed = ctx.LocalEcdhe.PublicKeyBytes };
        SendHandshake(stream, ctx, HandshakeType.ClientKeyExchange, cke.Serialize(), logger, "ClientKeyExchange");

        // 7-8. Compute pre_master_secret + master_secret + key_block
        byte[] preMaster = ctx.LocalEcdhe.DeriveSharedSecret(ctx.PeerPublicKey);
        ctx.MasterSecret = KeyDerivation.ComputeMasterSecret(preMaster, ctx.ClientRandom, ctx.ServerRandom);
        ctx.KeyMaterial = KeyDerivation.DeriveKeys(ctx.MasterSecret, ctx.ServerRandom, ctx.ClientRandom);
        Array.Clear(preMaster, 0, preMaster.Length);

        // Build connection state
        var clientWriteCipher = new AesGcmCipher(ctx.KeyMaterial.ClientWriteKey, ctx.KeyMaterial.ClientWriteIv);
        var serverReadCipher = new AesGcmCipher(ctx.KeyMaterial.ServerWriteKey, ctx.KeyMaterial.ServerWriteIv);
        var connState = new TlsConnectionState
        {
            MasterSecret = ctx.MasterSecret,
            WriteCipher = clientWriteCipher,
            ReadCipher = serverReadCipher
        };

        // 9. Send ChangeCipherSpec (plaintext)
        TlsRecordWriter.Write(stream, ContentType.ChangeCipherSpec, new[] { (byte)0x01 });
        logger?.Invoke("[tls] sent ChangeCipherSpec");

        // 10. Send Finished (encrypted with WriteCipher)
        byte[] handshakeHashForClient = ctx.HandshakeHash.ComputeCurrentHash();
        byte[] clientVerifyData = KeyDerivation.ComputeVerifyData(
            ctx.MasterSecret, TlsConstants.ClientFinishedLabel, handshakeHashForClient);

        var clientFinished = new FinishedMessage { VerifyData = clientVerifyData };
        byte[] clientFinishedFull = HandshakeWriter.BuildMessage(HandshakeType.Finished, clientFinished.Serialize());
        ctx.HandshakeHash.Append(clientFinishedFull);

        byte[] encryptedFinished = connState.WriteCipher.Encrypt(
            connState.WriteSequenceNumber, (byte)ContentType.Handshake, clientFinishedFull);
        TlsRecordWriter.Write(stream, ContentType.Handshake, encryptedFinished);
        connState.WriteSequenceNumber++;
        logger?.Invoke("[tls] sent Finished (encrypted)");

        // 11. Receive ChangeCipherSpec from server
        var ccsRecord = TlsRecordReader.Read(stream);
        if (ccsRecord.ContentType != ContentType.ChangeCipherSpec)
            throw new TlsException(
                $"Expected ChangeCipherSpec, got {ccsRecord.ContentType}",
                AlertDescription.UnexpectedMessage);
        if (ccsRecord.Payload.Length != 1 || ccsRecord.Payload[0] != 0x01)
            throw new TlsException("Malformed ChangeCipherSpec", AlertDescription.IllegalParameter);
        logger?.Invoke("[tls] received ChangeCipherSpec");

        // 12. Receive encrypted Finished from server
        var serverFinishedRec = TlsRecordReader.Read(stream);
        if (serverFinishedRec.ContentType != ContentType.Handshake)
            throw new TlsException(
                $"Expected encrypted Handshake (Finished), got {serverFinishedRec.ContentType}",
                AlertDescription.UnexpectedMessage);

        byte[] decrypted = connState.ReadCipher.Decrypt(
            connState.ReadSequenceNumber, (byte)ContentType.Handshake, serverFinishedRec.Payload);
        connState.ReadSequenceNumber++;

        // Parse decrypted Finished
        var fr = new BigEndianReader(decrypted);
        byte fType = fr.ReadUInt8();
        if ((HandshakeType)fType != HandshakeType.Finished)
            throw new TlsException(
                $"Expected Finished, got {(HandshakeType)fType}",
                AlertDescription.UnexpectedMessage);
        int fLen = (int)fr.ReadUInt24();
        var fBody = fr.ReadBytes(fLen).ToArray();
        var serverFinished = FinishedMessage.Parse(fBody);

        byte[] expectedServerVerifyData = KeyDerivation.ComputeVerifyData(
            ctx.MasterSecret, TlsConstants.ServerFinishedLabel, ctx.HandshakeHash.ComputeCurrentHash());

        if (!serverFinished.VerifyAgainst(expectedServerVerifyData))
            throw new TlsException(
                "Server Finished verify_data mismatch",
                AlertDescription.DecryptError);

        logger?.Invoke("[tls] handshake complete (client)");
        return connState;
    }

    public static TlsConnectionState PerformServerHandshake(
        Stream stream,
        X509Certificate2 serverCertificate,
        Action<string>? logger)
    {
        using var ctx = new HandshakeContext { ServerCertificate = serverCertificate };
        var hsReader = new HandshakeReader(stream);

        // 1. Receive ClientHello
        var chBytes = ReceiveHandshake(hsReader, ctx, HandshakeType.ClientHello, logger, "ClientHello");
        var ch = ClientHello.Parse(chBytes);
        if (!ch.CipherSuites.Contains(CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256))
            throw new TlsException(
                "Client did not offer TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256",
                AlertDescription.HandshakeFailure);
        ctx.ClientRandom = ch.ClientRandom;

        // 2. Send ServerHello
        ctx.ServerRandom = SecureRandom.GenerateServerRandom();
        var serverHello = new ServerHello { ServerRandom = ctx.ServerRandom };
        SendHandshake(stream, ctx, HandshakeType.ServerHello, serverHello.Serialize(), logger, "ServerHello");

        // 3. Send Certificate
        byte[] certDer = serverCertificate.Export(X509ContentType.Cert);
        var certMsg = new CertificateMessage { CertificatesDer = new[] { certDer } };
        SendHandshake(stream, ctx, HandshakeType.Certificate, certMsg.Serialize(), logger, "Certificate");

        // 4. Send ServerKeyExchange
        ctx.LocalEcdhe = new EcdheP256();
        var ske = ServerKeyExchange.CreateAndSign(
            ctx.ClientRandom, ctx.ServerRandom, ctx.LocalEcdhe.PublicKeyBytes, serverCertificate);
        SendHandshake(stream, ctx, HandshakeType.ServerKeyExchange, ske.Serialize(), logger, "ServerKeyExchange");

        // 5. Send ServerHelloDone
        SendHandshake(stream, ctx, HandshakeType.ServerHelloDone, Array.Empty<byte>(), logger, "ServerHelloDone");

        // 6. Receive ClientKeyExchange
        var ckeBytes = ReceiveHandshake(hsReader, ctx, HandshakeType.ClientKeyExchange, logger, "ClientKeyExchange");
        var cke = ClientKeyExchange.Parse(ckeBytes);
        ctx.PeerPublicKey = cke.EcPointUncompressed;

        // 7. Compute master_secret + key block
        byte[] preMaster = ctx.LocalEcdhe.DeriveSharedSecret(ctx.PeerPublicKey);
        ctx.MasterSecret = KeyDerivation.ComputeMasterSecret(preMaster, ctx.ClientRandom, ctx.ServerRandom);
        ctx.KeyMaterial = KeyDerivation.DeriveKeys(ctx.MasterSecret, ctx.ServerRandom, ctx.ClientRandom);
        Array.Clear(preMaster, 0, preMaster.Length);

        // From server's perspective, write=server_write, read=client_write
        var serverWriteCipher = new AesGcmCipher(ctx.KeyMaterial.ServerWriteKey, ctx.KeyMaterial.ServerWriteIv);
        var clientReadCipher = new AesGcmCipher(ctx.KeyMaterial.ClientWriteKey, ctx.KeyMaterial.ClientWriteIv);
        var connState = new TlsConnectionState
        {
            MasterSecret = ctx.MasterSecret,
            WriteCipher = serverWriteCipher,
            ReadCipher = clientReadCipher
        };

        // 8. Receive ChangeCipherSpec
        var ccsRecord = TlsRecordReader.Read(stream);
        if (ccsRecord.ContentType != ContentType.ChangeCipherSpec)
            throw new TlsException(
                $"Expected ChangeCipherSpec, got {ccsRecord.ContentType}",
                AlertDescription.UnexpectedMessage);
        if (ccsRecord.Payload.Length != 1 || ccsRecord.Payload[0] != 0x01)
            throw new TlsException("Malformed ChangeCipherSpec", AlertDescription.IllegalParameter);
        logger?.Invoke("[tls] received ChangeCipherSpec");

        // 9. Receive encrypted Finished from client
        var clientFinishedRec = TlsRecordReader.Read(stream);
        if (clientFinishedRec.ContentType != ContentType.Handshake)
            throw new TlsException(
                $"Expected encrypted Handshake (Finished), got {clientFinishedRec.ContentType}",
                AlertDescription.UnexpectedMessage);

        byte[] decrypted = connState.ReadCipher.Decrypt(
            connState.ReadSequenceNumber, (byte)ContentType.Handshake, clientFinishedRec.Payload);
        connState.ReadSequenceNumber++;

        var fr = new BigEndianReader(decrypted);
        byte fType = fr.ReadUInt8();
        if ((HandshakeType)fType != HandshakeType.Finished)
            throw new TlsException(
                $"Expected Finished, got {(HandshakeType)fType}",
                AlertDescription.UnexpectedMessage);
        int fLen = (int)fr.ReadUInt24();
        var fBody = fr.ReadBytes(fLen).ToArray();
        var clientFinishedFull = new byte[4 + fLen];
        clientFinishedFull[0] = (byte)HandshakeType.Finished;
        clientFinishedFull[1] = (byte)((fLen >> 16) & 0xFF);
        clientFinishedFull[2] = (byte)((fLen >> 8) & 0xFF);
        clientFinishedFull[3] = (byte)(fLen & 0xFF);
        Buffer.BlockCopy(fBody, 0, clientFinishedFull, 4, fLen);

        byte[] expectedClientVerifyData = KeyDerivation.ComputeVerifyData(
            ctx.MasterSecret, TlsConstants.ClientFinishedLabel, ctx.HandshakeHash.ComputeCurrentHash());
        var clientFinished = FinishedMessage.Parse(fBody);
        if (!clientFinished.VerifyAgainst(expectedClientVerifyData))
            throw new TlsException(
                "Client Finished verify_data mismatch",
                AlertDescription.DecryptError);

        ctx.HandshakeHash.Append(clientFinishedFull);
        logger?.Invoke("[tls] received Finished (decrypted ok)");

        // 10. Send ChangeCipherSpec
        TlsRecordWriter.Write(stream, ContentType.ChangeCipherSpec, new[] { (byte)0x01 });
        logger?.Invoke("[tls] sent ChangeCipherSpec");

        // 11. Send Finished (encrypted)
        byte[] serverVerifyData = KeyDerivation.ComputeVerifyData(
            ctx.MasterSecret, TlsConstants.ServerFinishedLabel, ctx.HandshakeHash.ComputeCurrentHash());
        var serverFinished = new FinishedMessage { VerifyData = serverVerifyData };
        byte[] serverFinishedFull = HandshakeWriter.BuildMessage(HandshakeType.Finished, serverFinished.Serialize());

        byte[] encryptedFinished = connState.WriteCipher.Encrypt(
            connState.WriteSequenceNumber, (byte)ContentType.Handshake, serverFinishedFull);
        TlsRecordWriter.Write(stream, ContentType.Handshake, encryptedFinished);
        connState.WriteSequenceNumber++;
        logger?.Invoke("[tls] sent Finished (encrypted)");
        logger?.Invoke("[tls] handshake complete (server)");

        return connState;
    }

    private static void SendHandshake(
        Stream stream,
        HandshakeContext ctx,
        HandshakeType type,
        byte[] body,
        Action<string>? logger,
        string label)
    {
        byte[] full = HandshakeWriter.BuildMessage(type, body);
        ctx.HandshakeHash.Append(full);
        TlsRecordWriter.Write(stream, ContentType.Handshake, full);
        logger?.Invoke($"[tls] sent {label} ({full.Length}B)");
    }

    private static byte[] ReceiveHandshake(
        HandshakeReader reader,
        HandshakeContext ctx,
        HandshakeType expected,
        Action<string>? logger,
        string label)
    {
        var (type, body, full) = reader.Read();
        if (type != expected)
            throw new TlsException(
                $"Expected {expected}, got {type}",
                AlertDescription.UnexpectedMessage);
        ctx.HandshakeHash.Append(full);
        logger?.Invoke($"[tls] received {label} ({full.Length}B)");
        return body;
    }

    private static void ValidateServerCertificate(
        X509Certificate2 cert,
        Func<X509Certificate2, bool>? validator,
        bool allowInvalid)
    {
        if (validator is not null)
        {
            if (!validator(cert))
                throw new TlsException(
                    "Server certificate rejected by custom validator",
                    AlertDescription.BadCertificate);
            return;
        }

        if (allowInvalid)
            return;

        // Default: build chain and verify
        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        if (!chain.Build(cert))
            throw new TlsException(
                "Server certificate failed chain validation",
                AlertDescription.BadCertificate);
    }
}
