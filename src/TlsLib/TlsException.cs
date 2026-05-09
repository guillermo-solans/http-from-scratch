using TlsLib.Protocol;

namespace TlsLib;

public class TlsException : Exception
{
    internal AlertDescription? AlertDescription { get; }

    public TlsException(string message) : base(message) { }

    public TlsException(string message, Exception? inner) : base(message, inner) { }

    internal TlsException(string message, AlertDescription alert) : base(message)
    {
        AlertDescription = alert;
    }

    internal TlsException(string message, AlertDescription alert, Exception? inner) : base(message, inner)
    {
        AlertDescription = alert;
    }
}
