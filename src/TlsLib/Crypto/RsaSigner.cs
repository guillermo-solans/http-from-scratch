using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace TlsLib.Crypto;

internal static class RsaSigner
{
    public static byte[] Sign(byte[] data, X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(certificate);

        using var rsa = certificate.GetRSAPrivateKey()
            ?? throw new InvalidOperationException("Certificate does not have an accessible RSA private key");

        return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    public static bool Verify(byte[] data, byte[] signature, X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(signature);
        ArgumentNullException.ThrowIfNull(certificate);

        using var rsa = certificate.GetRSAPublicKey()
            ?? throw new InvalidOperationException("Certificate does not expose an RSA public key");

        return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }
}
