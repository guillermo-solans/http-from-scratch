using System.Security.Cryptography;
using TlsLib.Protocol;

namespace TlsLib.Handshake;

internal sealed class FinishedMessage
{
    public required byte[] VerifyData { get; init; }

    public byte[] Serialize() => VerifyData;

    public static FinishedMessage Parse(byte[] body)
    {
        if (body.Length != TlsConstants.VerifyDataLength)
            throw new TlsException(
                $"Finished verify_data must be {TlsConstants.VerifyDataLength} bytes, got {body.Length}",
                AlertDescription.DecodeError);

        return new FinishedMessage { VerifyData = body };
    }

    public bool VerifyAgainst(byte[] expectedVerifyData)
    {
        if (expectedVerifyData.Length != VerifyData.Length)
            return false;
        return CryptographicOperations.FixedTimeEquals(VerifyData, expectedVerifyData);
    }
}
