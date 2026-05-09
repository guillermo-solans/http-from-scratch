namespace TlsLib.Protocol;

internal static class TlsConstants
{
    public const ushort TlsVersion = 0x0303;

    public const int MaxRecordPayload = 16384;

    public const int MaxEncryptedRecordPayload = MaxRecordPayload + 2048;

    public const int RecordHeaderLength = 5;

    public const int HandshakeHeaderLength = 4;

    public const int VerifyDataLength = 12;

    public const int RandomLength = 32;

    public const int MasterSecretLength = 48;

    public const int PreMasterSecretLength = 48;

    public const int AesGcmKeyLength = 16;

    public const int AesGcmImplicitNonceLength = 4;

    public const int AesGcmExplicitNonceLength = 8;

    public const int AesGcmTagLength = 16;

    public const string MasterSecretLabel = "master secret";

    public const string KeyExpansionLabel = "key expansion";

    public const string ClientFinishedLabel = "client finished";

    public const string ServerFinishedLabel = "server finished";

    public const byte EcCurveTypeNamedCurve = 0x03;

    public const ushort NamedCurveSecp256r1 = 0x0017;

    public const byte SignatureAlgorithmRsa = 0x01;

    public const byte HashAlgorithmSha256 = 0x04;

    public const byte CompressionMethodNull = 0x00;
}
