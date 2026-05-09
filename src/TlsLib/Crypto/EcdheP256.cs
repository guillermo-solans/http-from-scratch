using System;
using System.Security.Cryptography;

namespace TlsLib.Crypto;

internal sealed class EcdheP256 : IDisposable
{
    private const int CoordinateLength = 32;

    private const int UncompressedPointLength = 1 + (CoordinateLength * 2);

    private readonly ECDiffieHellman _ecdh;

    private bool _disposed;

    public byte[] PublicKeyBytes { get; }

    public EcdheP256()
    {
        _ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var parameters = _ecdh.ExportParameters(includePrivateParameters: false);
        PublicKeyBytes = SerializeUncompressedPoint(parameters.Q);
    }

    public byte[] DeriveSharedSecret(byte[] peerPublicKeyBytes)
    {
        ArgumentNullException.ThrowIfNull(peerPublicKeyBytes);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (peerPublicKeyBytes.Length != UncompressedPointLength)
            throw new ArgumentException($"Expected {UncompressedPointLength} bytes (uncompressed P-256 point), got {peerPublicKeyBytes.Length}", nameof(peerPublicKeyBytes));
        if (peerPublicKeyBytes[0] != 0x04)
            throw new ArgumentException("Peer public key is not an uncompressed point (leading byte must be 0x04)", nameof(peerPublicKeyBytes));

        var qx = new byte[CoordinateLength];
        var qy = new byte[CoordinateLength];
        Buffer.BlockCopy(peerPublicKeyBytes, 1, qx, 0, CoordinateLength);
        Buffer.BlockCopy(peerPublicKeyBytes, 1 + CoordinateLength, qy, 0, CoordinateLength);

        using var peer = ECDiffieHellman.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = qx, Y = qy }
        });

        return _ecdh.DeriveRawSecretAgreement(peer.PublicKey);
    }

    private static byte[] SerializeUncompressedPoint(ECPoint q)
    {
        if (q.X is null || q.Y is null)
            throw new InvalidOperationException("Invalid EC public key: X/Y coordinates are missing");

        var bytes = new byte[UncompressedPointLength];
        bytes[0] = 0x04;
        CopyLeftPadded(q.X, bytes, 1, CoordinateLength);
        CopyLeftPadded(q.Y, bytes, 1 + CoordinateLength, CoordinateLength);
        return bytes;
    }

    private static void CopyLeftPadded(byte[] source, byte[] destination, int destinationOffset, int targetLength)
    {
        if (source.Length > targetLength)
            throw new InvalidOperationException($"EC coordinate is {source.Length} bytes, expected at most {targetLength}");

        int padding = targetLength - source.Length;
        Buffer.BlockCopy(source, 0, destination, destinationOffset + padding, source.Length);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _ecdh.Dispose();
    }
}
