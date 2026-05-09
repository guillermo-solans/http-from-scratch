using System;
using System.Security.Cryptography;

namespace TlsLib.Util;

internal static class SecureRandom
{
    public static byte[] GenerateBytes(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        var bytes = new byte[count];
        if (count > 0)
            RandomNumberGenerator.Fill(bytes);
        return bytes;
    }

    public static byte[] GenerateClientRandom() => GenerateRandomWithGmtTime();

    public static byte[] GenerateServerRandom() => GenerateRandomWithGmtTime();

    private static byte[] GenerateRandomWithGmtTime()
    {
        var random = new byte[32];
        uint gmtUnixTime = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        random[0] = (byte)(gmtUnixTime >> 24);
        random[1] = (byte)(gmtUnixTime >> 16);
        random[2] = (byte)(gmtUnixTime >> 8);
        random[3] = (byte)gmtUnixTime;
        RandomNumberGenerator.Fill(random.AsSpan(4, 28));
        return random;
    }
}
