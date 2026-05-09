using System.Security.Cryptography.X509Certificates;
using TlsLib.Crypto;

namespace TlsLib.State;

internal sealed class HandshakeContext : IDisposable
{
    public byte[]? ClientRandom { get; set; }
    public byte[]? ServerRandom { get; set; }
    public X509Certificate2? ServerCertificate { get; set; }
    public EcdheP256? LocalEcdhe { get; set; }
    public byte[]? PeerPublicKey { get; set; }
    public byte[]? MasterSecret { get; set; }
    public HandshakeHash HandshakeHash { get; } = new();
    public KeyMaterial? KeyMaterial { get; set; }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        LocalEcdhe?.Dispose();
        HandshakeHash.Dispose();
    }
}
