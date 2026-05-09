using System.Security.Cryptography.X509Certificates;

namespace TlsLib;

public sealed class TlsClientOptions
{
    public bool AllowInvalidCertificates { get; init; } = true;

    public Func<X509Certificate2, bool>? ServerCertificateValidator { get; init; }

    public Action<string>? Logger { get; init; }
}
