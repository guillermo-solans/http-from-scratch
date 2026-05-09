using TlsLib.State;

namespace TlsLib;

public static class TlsClientFactory
{
    public static TlsStream Connect(Stream innerStream, string serverName, TlsClientOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(innerStream);
        ArgumentNullException.ThrowIfNull(serverName);

        options ??= new TlsClientOptions();

        var connState = HandshakeStateMachine.PerformClientHandshake(
            innerStream,
            serverName,
            options.Logger,
            options.ServerCertificateValidator,
            options.AllowInvalidCertificates);

        return new TlsStream(innerStream, connState, options.Logger);
    }
}
