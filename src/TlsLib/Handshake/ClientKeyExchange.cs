using TlsLib.Util;

namespace TlsLib.Handshake;

internal sealed class ClientKeyExchange
{
    public required byte[] EcPointUncompressed { get; init; }

    public byte[] Serialize()
    {
        var w = new BigEndianWriter();
        w.WriteVector8(inner =>
        {
            inner.WriteBytes(EcPointUncompressed);
        });
        return w.ToArray();
    }

    public static ClientKeyExchange Parse(byte[] body)
    {
        var r = new BigEndianReader(body);
        byte[] point = r.ReadVector8().ToArray();
        return new ClientKeyExchange { EcPointUncompressed = point };
    }
}
