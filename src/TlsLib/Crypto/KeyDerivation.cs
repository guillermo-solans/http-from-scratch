using System;
using TlsLib.Protocol;

namespace TlsLib.Crypto;

internal static class KeyDerivation
{
    public static byte[] ComputeMasterSecret(byte[] preMasterSecret, byte[] clientRandom, byte[] serverRandom)
    {
        ArgumentNullException.ThrowIfNull(preMasterSecret);
        ArgumentNullException.ThrowIfNull(clientRandom);
        ArgumentNullException.ThrowIfNull(serverRandom);

        var seed = Concat(clientRandom, serverRandom);
        return PrfSha256.Compute(preMasterSecret, TlsConstants.MasterSecretLabel, seed, TlsConstants.MasterSecretLength);
    }

    public static KeyMaterial DeriveKeys(byte[] masterSecret, byte[] serverRandom, byte[] clientRandom)
    {
        ArgumentNullException.ThrowIfNull(masterSecret);
        ArgumentNullException.ThrowIfNull(serverRandom);
        ArgumentNullException.ThrowIfNull(clientRandom);

        // For AEAD ciphers (AES-128-GCM): no MAC keys, only:
        // client_write_key (16) + server_write_key (16) + client_write_IV (4) + server_write_IV (4) = 40 bytes
        int totalLength =
            (TlsConstants.AesGcmKeyLength * 2) +
            (TlsConstants.AesGcmImplicitNonceLength * 2);

        // RFC 5246 §6.3: seed = server_random || client_random
        var seed = Concat(serverRandom, clientRandom);
        var keyBlock = PrfSha256.Compute(masterSecret, TlsConstants.KeyExpansionLabel, seed, totalLength);

        int offset = 0;
        var clientWriteKey = Slice(keyBlock, ref offset, TlsConstants.AesGcmKeyLength);
        var serverWriteKey = Slice(keyBlock, ref offset, TlsConstants.AesGcmKeyLength);
        var clientWriteIv = Slice(keyBlock, ref offset, TlsConstants.AesGcmImplicitNonceLength);
        var serverWriteIv = Slice(keyBlock, ref offset, TlsConstants.AesGcmImplicitNonceLength);

        return new KeyMaterial
        {
            ClientWriteKey = clientWriteKey,
            ServerWriteKey = serverWriteKey,
            ClientWriteIv = clientWriteIv,
            ServerWriteIv = serverWriteIv
        };
    }

    public static byte[] ComputeVerifyData(byte[] masterSecret, string label, byte[] handshakeHash)
    {
        ArgumentNullException.ThrowIfNull(masterSecret);
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(handshakeHash);

        return PrfSha256.Compute(masterSecret, label, handshakeHash, TlsConstants.VerifyDataLength);
    }

    private static byte[] Concat(byte[] a, byte[] b)
    {
        var result = new byte[a.Length + b.Length];
        Buffer.BlockCopy(a, 0, result, 0, a.Length);
        Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
        return result;
    }

    private static byte[] Slice(byte[] source, ref int offset, int length)
    {
        var result = new byte[length];
        Buffer.BlockCopy(source, offset, result, 0, length);
        offset += length;
        return result;
    }
}
