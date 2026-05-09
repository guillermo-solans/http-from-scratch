namespace TlsLib.Handshake;

internal sealed class ServerHelloDone
{
    public byte[] Serialize() => Array.Empty<byte>();

    public static ServerHelloDone Parse(byte[] body)
    {
        if (body.Length != 0)
            throw new TlsException(
                $"ServerHelloDone must have empty body, got {body.Length} bytes",
                Protocol.AlertDescription.DecodeError);
        return new ServerHelloDone();
    }
}
