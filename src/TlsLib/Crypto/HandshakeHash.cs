using System;
using System.Security.Cryptography;

namespace TlsLib.Crypto;

internal sealed class HandshakeHash : IDisposable
{
    private readonly IncrementalHash _hash;

    private bool _disposed;

    public HandshakeHash()
    {
        _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    }

    public void Append(byte[] handshakeMessageBytes)
    {
        ArgumentNullException.ThrowIfNull(handshakeMessageBytes);
        ObjectDisposedException.ThrowIf(_disposed, this);
        _hash.AppendData(handshakeMessageBytes);
    }

    public void Append(ReadOnlySpan<byte> handshakeMessageBytes)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _hash.AppendData(handshakeMessageBytes);
    }

    public byte[] ComputeCurrentHash()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // GetHashAndReset would clear; we want non-destructive snapshots.
        return _hash.GetCurrentHash();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _hash.Dispose();
    }
}
