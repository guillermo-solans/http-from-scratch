using TlsLib.Protocol;
using TlsLib.Util;

namespace TlsLib.Handshake;

internal sealed class ClientHello
{
    public ushort ClientVersion { get; init; } = TlsConstants.TlsVersion;
    public required byte[] ClientRandom { get; init; }
    public byte[] SessionId { get; init; } = Array.Empty<byte>();
    public required ushort[] CipherSuites { get; init; }
    public byte[] CompressionMethods { get; init; } = new byte[] { TlsConstants.CompressionMethodNull };
    public string? ServerName { get; init; }

    public byte[] Serialize()
    {
        var w = new BigEndianWriter();
        w.WriteUInt16(ClientVersion);
        w.WriteBytes(ClientRandom);

        w.WriteVector8(inner =>
        {
            inner.WriteBytes(SessionId);
        });

        w.WriteVector16(inner =>
        {
            foreach (var cs in CipherSuites)
                inner.WriteUInt16(cs);
        });

        w.WriteVector8(inner =>
        {
            inner.WriteBytes(CompressionMethods);
        });

        w.WriteVector16(inner =>
        {
            // supported_groups extension: secp256r1
            inner.WriteUInt16(0x000A);
            inner.WriteVector16(ext =>
            {
                ext.WriteVector16(list =>
                {
                    list.WriteUInt16(TlsConstants.NamedCurveSecp256r1);
                });
            });

            // ec_point_formats extension: uncompressed
            inner.WriteUInt16(0x000B);
            inner.WriteVector16(ext =>
            {
                ext.WriteVector8(list =>
                {
                    list.WriteUInt8(0x00);
                });
            });

            // signature_algorithms extension: rsa_pkcs1_sha256
            inner.WriteUInt16(0x000D);
            inner.WriteVector16(ext =>
            {
                ext.WriteVector16(list =>
                {
                    list.WriteUInt8(TlsConstants.HashAlgorithmSha256);
                    list.WriteUInt8(TlsConstants.SignatureAlgorithmRsa);
                });
            });

            // server_name extension (optional, SNI)
            if (!string.IsNullOrEmpty(ServerName))
            {
                inner.WriteUInt16(0x0000);
                inner.WriteVector16(ext =>
                {
                    ext.WriteVector16(list =>
                    {
                        list.WriteUInt8(0x00); // host_name
                        list.WriteVector16(name =>
                        {
                            name.WriteBytes(System.Text.Encoding.ASCII.GetBytes(ServerName));
                        });
                    });
                });
            }
        });

        return w.ToArray();
    }

    public static ClientHello Parse(byte[] body)
    {
        var r = new BigEndianReader(body);

        ushort clientVersion = r.ReadUInt16();
        byte[] clientRandom = r.ReadBytes(TlsConstants.RandomLength).ToArray();
        byte[] sessionId = r.ReadVector8().ToArray();

        var csSpan = r.ReadVector16();
        if ((csSpan.Length & 1) != 0)
            throw new TlsException("Invalid cipher_suites vector length", AlertDescription.DecodeError);

        var cipherSuites = new ushort[csSpan.Length / 2];
        for (int i = 0; i < cipherSuites.Length; i++)
        {
            cipherSuites[i] = (ushort)((csSpan[i * 2] << 8) | csSpan[i * 2 + 1]);
        }

        byte[] compressionMethods = r.ReadVector8().ToArray();

        // We don't care about extensions parsing here. They are optional for our flow.
        return new ClientHello
        {
            ClientVersion = clientVersion,
            ClientRandom = clientRandom,
            SessionId = sessionId,
            CipherSuites = cipherSuites,
            CompressionMethods = compressionMethods
        };
    }
}
