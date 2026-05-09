using System;
using System.Security.Cryptography;
using TlsLib.Protocol;

namespace TlsLib.Crypto;

internal sealed class AesGcmCipher : IDisposable
{
    private readonly AesGcm _aesGcm;

    private readonly byte[] _implicitNonce;

    private bool _disposed;

    public AesGcmCipher(byte[] key, byte[] implicitNonce)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(implicitNonce);

        if (key.Length != TlsConstants.AesGcmKeyLength)
            throw new ArgumentException($"Key must be {TlsConstants.AesGcmKeyLength} bytes", nameof(key));
        if (implicitNonce.Length != TlsConstants.AesGcmImplicitNonceLength)
            throw new ArgumentException($"Implicit nonce must be {TlsConstants.AesGcmImplicitNonceLength} bytes", nameof(implicitNonce));

        _aesGcm = new AesGcm(key, TlsConstants.AesGcmTagLength);
        _implicitNonce = (byte[])implicitNonce.Clone();
    }

    public byte[] Encrypt(ulong sequenceNumber, byte contentType, byte[] plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ObjectDisposedException.ThrowIf(_disposed, this);

        Span<byte> explicitNonce = stackalloc byte[TlsConstants.AesGcmExplicitNonceLength];
        WriteUInt64BigEndian(sequenceNumber, explicitNonce);

        Span<byte> nonce = stackalloc byte[TlsConstants.AesGcmImplicitNonceLength + TlsConstants.AesGcmExplicitNonceLength];
        _implicitNonce.CopyTo(nonce);
        explicitNonce.CopyTo(nonce[TlsConstants.AesGcmImplicitNonceLength..]);

        Span<byte> aad = stackalloc byte[13];
        BuildAdditionalData(sequenceNumber, contentType, (ushort)plaintext.Length, aad);

        var output = new byte[TlsConstants.AesGcmExplicitNonceLength + plaintext.Length + TlsConstants.AesGcmTagLength];
        explicitNonce.CopyTo(output);

        var ciphertext = output.AsSpan(TlsConstants.AesGcmExplicitNonceLength, plaintext.Length);
        var tag = output.AsSpan(TlsConstants.AesGcmExplicitNonceLength + plaintext.Length, TlsConstants.AesGcmTagLength);

        _aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, aad);

        return output;
    }

    public byte[] Decrypt(ulong sequenceNumber, byte contentType, byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ObjectDisposedException.ThrowIf(_disposed, this);

        int minLength = TlsConstants.AesGcmExplicitNonceLength + TlsConstants.AesGcmTagLength;
        if (payload.Length < minLength)
            throw new ArgumentException($"Encrypted payload too short: {payload.Length} bytes (minimum {minLength})", nameof(payload));

        int ciphertextLength = payload.Length - TlsConstants.AesGcmExplicitNonceLength - TlsConstants.AesGcmTagLength;

        Span<byte> nonce = stackalloc byte[TlsConstants.AesGcmImplicitNonceLength + TlsConstants.AesGcmExplicitNonceLength];
        _implicitNonce.CopyTo(nonce);
        payload.AsSpan(0, TlsConstants.AesGcmExplicitNonceLength)
            .CopyTo(nonce[TlsConstants.AesGcmImplicitNonceLength..]);

        Span<byte> aad = stackalloc byte[13];
        BuildAdditionalData(sequenceNumber, contentType, (ushort)ciphertextLength, aad);

        var ciphertext = payload.AsSpan(TlsConstants.AesGcmExplicitNonceLength, ciphertextLength);
        var tag = payload.AsSpan(TlsConstants.AesGcmExplicitNonceLength + ciphertextLength, TlsConstants.AesGcmTagLength);

        var plaintext = new byte[ciphertextLength];
        _aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, aad);
        return plaintext;
    }

    private static void BuildAdditionalData(ulong sequenceNumber, byte contentType, ushort plaintextLength, Span<byte> destination)
    {
        // RFC 5246 §6.2.3.3 + RFC 5288 §3:
        // additional_data = seq_num(8) || TLSCompressed.type(1) || TLSCompressed.version(2) || TLSCompressed.length(2)
        WriteUInt64BigEndian(sequenceNumber, destination[..8]);
        destination[8] = contentType;
        destination[9] = (byte)(TlsConstants.TlsVersion >> 8);
        destination[10] = (byte)(TlsConstants.TlsVersion & 0xFF);
        destination[11] = (byte)(plaintextLength >> 8);
        destination[12] = (byte)plaintextLength;
    }

    private static void WriteUInt64BigEndian(ulong value, Span<byte> destination)
    {
        destination[0] = (byte)(value >> 56);
        destination[1] = (byte)(value >> 48);
        destination[2] = (byte)(value >> 40);
        destination[3] = (byte)(value >> 32);
        destination[4] = (byte)(value >> 24);
        destination[5] = (byte)(value >> 16);
        destination[6] = (byte)(value >> 8);
        destination[7] = (byte)value;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _aesGcm.Dispose();
    }
}
