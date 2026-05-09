using System;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace TlsLib.Certificates;

public static class SelfSignedCertificateGenerator
{
    public static X509Certificate2 Generate(string commonName = "localhost", int validityDays = 365)
    {
        ArgumentException.ThrowIfNullOrEmpty(commonName);
        if (validityDays <= 0)
            throw new ArgumentOutOfRangeException(nameof(validityDays));

        var rsa = RSA.Create(2048);
        try
        {
            var distinguishedName = new X500DistinguishedName($"CN={commonName}");

            var request = new CertificateRequest(
                distinguishedName,
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(
                certificateAuthority: false,
                hasPathLengthConstraint: false,
                pathLengthConstraint: 0,
                critical: true));

            request.CertificateExtensions.Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                critical: true));

            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                new OidCollection
                {
                    new Oid("1.3.6.1.5.5.7.3.1") // server auth
                },
                critical: false));

            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName(commonName);
            if (!string.Equals(commonName, "localhost", StringComparison.OrdinalIgnoreCase))
                sanBuilder.AddDnsName("localhost");
            sanBuilder.AddIpAddress(IPAddress.Loopback);
            sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
            request.CertificateExtensions.Add(sanBuilder.Build());

            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(
                request.PublicKey,
                critical: false));

            var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
            var notAfter = notBefore.AddDays(validityDays);

            using var ephemeral = request.CreateSelfSigned(notBefore, notAfter);

            // CreateSelfSigned returns a cert whose private key may not survive serialization on some
            // platforms. Round-tripping through PKCS#12 produces a cert where HasPrivateKey == true
            // and the private key is reliably accessible afterwards.
            var pfxBytes = ephemeral.Export(X509ContentType.Pfx);
            return X509CertificateLoader.LoadPkcs12(
                pfxBytes,
                password: null,
                keyStorageFlags: X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        }
        finally
        {
            rsa.Dispose();
        }
    }
}
