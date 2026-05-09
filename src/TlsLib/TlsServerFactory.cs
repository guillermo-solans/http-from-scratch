using TlsLib.State;

namespace TlsLib;

public static class TlsServerFactory
{
    public static TlsStream Accept(Stream innerStream, TlsServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(innerStream);
        ArgumentNullException.ThrowIfNull(options);

        var connState = HandshakeStateMachine.PerformServerHandshake(
            innerStream,
            options.ServerCertificate,
            options.Logger);

        return new TlsStream(innerStream, connState, options.Logger);
    }
}
