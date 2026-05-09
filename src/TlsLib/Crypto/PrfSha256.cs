using System;
using System.Security.Cryptography;
using System.Text;

namespace TlsLib.Crypto;

internal static class PrfSha256
{
    public static byte[] Compute(byte[] secret, string label, byte[] seed, int length)
    {
        ArgumentNullException.ThrowIfNull(secret);
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(seed);
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        var labelBytes = Encoding.ASCII.GetBytes(label);
        var combinedSeed = new byte[labelBytes.Length + seed.Length];
        Buffer.BlockCopy(labelBytes, 0, combinedSeed, 0, labelBytes.Length);
        Buffer.BlockCopy(seed, 0, combinedSeed, labelBytes.Length, seed.Length);

        return PHash(secret, combinedSeed, length);
    }

    private static byte[] PHash(byte[] secret, byte[] seed, int length)
    {
        // P_hash(secret, seed) = HMAC_hash(secret, A(1) + seed) || HMAC_hash(secret, A(2) + seed) || ...
        // A(0) = seed, A(i) = HMAC_hash(secret, A(i-1))
        var output = new byte[length];
        int written = 0;

        byte[] a = HmacSha256(secret, seed);

        while (written < length)
        {
            var concat = new byte[a.Length + seed.Length];
            Buffer.BlockCopy(a, 0, concat, 0, a.Length);
            Buffer.BlockCopy(seed, 0, concat, a.Length, seed.Length);

            byte[] block = HmacSha256(secret, concat);

            int toCopy = Math.Min(block.Length, length - written);
            Buffer.BlockCopy(block, 0, output, written, toCopy);
            written += toCopy;

            if (written < length)
                a = HmacSha256(secret, a);
        }

        return output;
    }

    private static byte[] HmacSha256(byte[] key, byte[] data)
    {
        return HMACSHA256.HashData(key, data);
    }
}
