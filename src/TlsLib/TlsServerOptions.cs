using System.Security.Cryptography.X509Certificates;

namespace TlsLib;

public sealed class TlsServerOptions
{
    public required X509Certificate2 ServerCertificate { get; init; }

    public Action<string>? Logger { get; init; }
}
