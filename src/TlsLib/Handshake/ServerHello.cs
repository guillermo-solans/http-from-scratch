using TlsLib.Protocol;
using TlsLib.Util;

namespace TlsLib.Handshake;

internal sealed class ServerHello
{
    public ushort ServerVersion { get; init; } = TlsConstants.TlsVersion;
    public required byte[] ServerRandom { get; init; }
    public byte[] SessionId { get; init; } = Array.Empty<byte>();
    public ushort CipherSuite { get; init; } = Protocol.CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256;
    public byte CompressionMethod { get; init; } = TlsConstants.CompressionMethodNull;

    public byte[] Serialize()
    {
        var w = new BigEndianWriter();
        w.WriteUInt16(ServerVersion);
        w.WriteBytes(ServerRandom);
        w.WriteVector8(inner =>
        {
            inner.WriteBytes(SessionId);
        });
        w.WriteUInt16(CipherSuite);
        w.WriteUInt8(CompressionMethod);
        // We do not emit extensions (no negotiated extensions for our minimal subset).
        return w.ToArray();
    }

    public static ServerHello Parse(byte[] body)
    {
        var r = new BigEndianReader(body);
        ushort serverVersion = r.ReadUInt16();
        byte[] serverRandom = r.ReadBytes(TlsConstants.RandomLength).ToArray();
        byte[] sessionId = r.ReadVector8().ToArray();
        ushort cipherSuite = r.ReadUInt16();
        byte compressionMethod = r.ReadUInt8();
        // ignore any trailing extensions

        return new ServerHello
        {
            ServerVersion = serverVersion,
            ServerRandom = serverRandom,
            SessionId = sessionId,
            CipherSuite = cipherSuite,
            CompressionMethod = compressionMethod
        };
    }
}
