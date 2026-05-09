using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using TlsLib.Protocol;
using TlsLib.Util;

namespace TlsLib.Handshake;

internal sealed class ServerKeyExchange
{
    public byte CurveType { get; init; } = TlsConstants.EcCurveTypeNamedCurve;
    public ushort NamedCurve { get; init; } = TlsConstants.NamedCurveSecp256r1;
    public required byte[] EcPointUncompressed { get; init; }
    public byte HashAlgorithm { get; init; } = TlsConstants.HashAlgorithmSha256;
    public byte SignatureAlgorithm { get; init; } = TlsConstants.SignatureAlgorithmRsa;
    public required byte[] Signature { get; init; }

    public byte[] Serialize()
    {
        var w = new BigEndianWriter();
        WriteParams(w);
        w.WriteUInt8(HashAlgorithm);
        w.WriteUInt8(SignatureAlgorithm);
        w.WriteVector16(inner =>
        {
            inner.WriteBytes(Signature);
        });
        return w.ToArray();
    }

    private void WriteParams(BigEndianWriter w)
    {
        w.WriteUInt8(CurveType);
        w.WriteUInt16(NamedCurve);
        w.WriteVector8(inner =>
        {
            inner.WriteBytes(EcPointUncompressed);
        });
    }

    public static ServerKeyExchange Parse(byte[] body)
    {
        var r = new BigEndianReader(body);

        byte curveType = r.ReadUInt8();
        ushort namedCurve = r.ReadUInt16();
        byte[] ecPoint = r.ReadVector8().ToArray();
        byte hashAlg = r.ReadUInt8();
        byte sigAlg = r.ReadUInt8();
        byte[] signature = r.ReadVector16().ToArray();

        return new ServerKeyExchange
        {
            CurveType = curveType,
            NamedCurve = namedCurve,
            EcPointUncompressed = ecPoint,
            HashAlgorithm = hashAlg,
            SignatureAlgorithm = sigAlg,
            Signature = signature
        };
    }

    public byte[] BuildSignedData(byte[] clientRandom, byte[] serverRandom)
    {
        var w = new BigEndianWriter();
        w.WriteBytes(clientRandom);
        w.WriteBytes(serverRandom);
        WriteParams(w);
        return w.ToArray();
    }

    public static ServerKeyExchange CreateAndSign(
        byte[] clientRandom,
        byte[] serverRandom,
        byte[] ecPointUncompressed,
        X509Certificate2 serverCertificate)
    {
        var unsigned = new ServerKeyExchange
        {
            EcPointUncompressed = ecPointUncompressed,
            Signature = Array.Empty<byte>()
        };

        byte[] signedData = unsigned.BuildSignedData(clientRandom, serverRandom);

        using var rsa = serverCertificate.GetRSAPrivateKey()
            ?? throw new TlsException("Server certificate has no RSA private key", AlertDescription.InternalError);

        byte[] signature = rsa.SignData(signedData, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return new ServerKeyExchange
        {
            EcPointUncompressed = ecPointUncompressed,
            Signature = signature
        };
    }

    public bool VerifySignature(byte[] clientRandom, byte[] serverRandom, X509Certificate2 serverCertificate)
    {
        if (HashAlgorithm != TlsConstants.HashAlgorithmSha256)
            throw new TlsException(
                $"Unsupported hash algorithm 0x{HashAlgorithm:X2} (expected SHA-256)",
                AlertDescription.IllegalParameter);

        if (SignatureAlgorithm != TlsConstants.SignatureAlgorithmRsa)
            throw new TlsException(
                $"Unsupported signature algorithm 0x{SignatureAlgorithm:X2} (expected RSA)",
                AlertDescription.IllegalParameter);

        if (CurveType != TlsConstants.EcCurveTypeNamedCurve || NamedCurve != TlsConstants.NamedCurveSecp256r1)
            throw new TlsException(
                "Unsupported curve in ServerKeyExchange",
                AlertDescription.IllegalParameter);

        byte[] signedData = BuildSignedData(clientRandom, serverRandom);

        using var rsa = serverCertificate.GetRSAPublicKey()
            ?? throw new TlsException("Server certificate has no RSA public key", AlertDescription.UnsupportedCertificate);

        return rsa.VerifyData(signedData, Signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }
}
