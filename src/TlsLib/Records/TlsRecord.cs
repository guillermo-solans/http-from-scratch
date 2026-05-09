using TlsLib.Protocol;

namespace TlsLib.Records;

internal readonly struct TlsRecord
{
    public ContentType ContentType { get; init; }

    public ushort Version { get; init; }

    public byte[] Payload { get; init; }
}
