using TlsLib.Crypto;

namespace TlsLib.State;

internal sealed class TlsConnectionState : IDisposable
{
    public required byte[] MasterSecret { get; init; }
    public required AesGcmCipher WriteCipher { get; init; }
    public required AesGcmCipher ReadCipher { get; init; }
    public ulong WriteSequenceNumber { get; set; }
    public ulong ReadSequenceNumber { get; set; }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        WriteCipher.Dispose();
        ReadCipher.Dispose();
    }
}
