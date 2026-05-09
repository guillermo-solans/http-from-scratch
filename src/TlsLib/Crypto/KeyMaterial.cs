namespace TlsLib.Crypto;

internal sealed class KeyMaterial
{
    public required byte[] ClientWriteKey { get; init; }

    public required byte[] ServerWriteKey { get; init; }

    public required byte[] ClientWriteIv { get; init; }

    public required byte[] ServerWriteIv { get; init; }
}
