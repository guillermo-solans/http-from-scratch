using System.Security.Cryptography.X509Certificates;
using TlsLib.Protocol;
using TlsLib.Util;

namespace TlsLib.Handshake;

internal sealed class CertificateMessage
{
    public required IReadOnlyList<byte[]> CertificatesDer { get; init; }

    public byte[] Serialize()
    {
        var w = new BigEndianWriter();
        w.WriteVector24(inner =>
        {
            foreach (var der in CertificatesDer)
            {
                inner.WriteVector24(certWriter =>
                {
                    certWriter.WriteBytes(der);
                });
            }
        });
        return w.ToArray();
    }

    public static CertificateMessage Parse(byte[] body)
    {
        var r = new BigEndianReader(body);
        var listSpan = r.ReadVector24();
        var listReader = new BigEndianReader(listSpan.ToArray());

        var certs = new List<byte[]>();
        while (listReader.Remaining > 0)
        {
            var certBytes = listReader.ReadVector24();
            certs.Add(certBytes.ToArray());
        }

        if (certs.Count == 0)
            throw new TlsException("Certificate list is empty", AlertDescription.BadCertificate);

        return new CertificateMessage { CertificatesDer = certs };
    }

    public X509Certificate2 GetEndEntityCertificate()
    {
        if (CertificatesDer.Count == 0)
            throw new TlsException("No certificates available", AlertDescription.BadCertificate);
        return X509CertificateLoader.LoadCertificate(CertificatesDer[0]);
    }
}
