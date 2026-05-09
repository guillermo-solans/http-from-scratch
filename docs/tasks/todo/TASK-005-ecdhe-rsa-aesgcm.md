---
id: TASK-005
title: EcdheP256 + RsaSigner + AesGcmCipher
status: todo
priority: critical
phase: fase-1
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Crear las 3 primitivas criptográficas en `src/TlsLib/Crypto/`:

### `EcdheP256.cs`
```csharp
public sealed class EcdheP256 : IDisposable
{
    public byte[] PublicKeyUncompressed { get; }   // 65 bytes: 0x04 || X(32) || Y(32)
    public EcdheP256();                            // Genera nuevo par
    public byte[] DeriveSharedSecret(byte[] peerPublicKeyUncompressed); // 32 bytes (X coord)
    public void Dispose();
}
```
Usar `ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256)`. Exportar la clave pública en formato uncompressed (0x04 || X || Y) — TLS exige ese formato. Para derivar shared secret usar `DeriveRawSecretAgreement()` (devuelve la coord X cruda).

### `RsaSigner.cs`
```csharp
public static class RsaSigner
{
    public static byte[] SignSha256Pkcs1(RSA privateKey, byte[] data);
    public static bool VerifySha256Pkcs1(RSA publicKey, byte[] data, byte[] signature);
}
```

### `AesGcmCipher.cs`
```csharp
public sealed class AesGcmCipher : IDisposable
{
    public AesGcmCipher(byte[] key); // key = 16 bytes
    public byte[] Encrypt(byte[] nonce, byte[] plaintext, byte[] aad); // returns ciphertext||tag
    public byte[] Decrypt(byte[] nonce, byte[] ciphertextWithTag, byte[] aad); // throws TlsException(BadRecordMac) on tag mismatch
    public void Dispose();
}
```
Usar `System.Security.Cryptography.AesGcm`. Tag size = 16 bytes. Nonce size = 12 bytes.

**Nota TLS 1.2 GCM (RFC 5288):**
- Nonce = 4 bytes implicit_IV (de key_block) || 8 bytes explicit_nonce (en wire, primeros 8 bytes del payload cifrado)
- Convención simple: explicit_nonce = sequence number big-endian (8 bytes)
- AAD = seqNum(8) || ContentType(1) || Version(2) || PlaintextLength(2) — 13 bytes

## Criterios de Aceptación
- [ ] EcdheP256: dos instancias derivan el mismo shared secret cruzando claves públicas
- [ ] RsaSigner: SignSha256Pkcs1 + VerifySha256Pkcs1 round-trip OK
- [ ] AesGcmCipher: encrypt+decrypt round-trip OK; alterar 1 byte del ciphertext lanza TlsException
- [ ] Compila sin warnings

## Dependencias
- Bloqueada por: TASK-001

## Comentarios
### 2026-05-09 - project-manager
> Estas tres primitivas son las únicas operaciones criptográficas que necesitamos. Prohibido usar SslStream.
