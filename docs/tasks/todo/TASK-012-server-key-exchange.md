---
id: TASK-012
title: ServerKeyExchange — ECDHE params + firma RSA
status: todo
priority: critical
phase: fase-3
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Crear `src/TlsLib/Handshake/ServerKeyExchange.cs`. ESTE ES EL MENSAJE MÁS COMPLEJO.

```csharp
public sealed class ServerKeyExchange
{
    // ECParameters
    public byte CurveType { get; init; } = 0x03;  // named_curve
    public ushort NamedCurve { get; init; } = 0x0017; // secp256r1
    public byte[] PublicPointUncompressed { get; init; } = default!; // 65 bytes (0x04 || X || Y)

    // SignatureAndHashAlgorithm (TLS 1.2)
    public byte HashAlgorithm { get; init; } = 0x04;  // sha256
    public byte SignatureAlgorithm { get; init; } = 0x01; // rsa
    // = 0x0401

    public byte[] Signature { get; init; } = default!;

    public byte[] Serialize();
    public static ServerKeyExchange Parse(byte[] body);

    // Helper para construir y firmar (servidor)
    public static ServerKeyExchange CreateAndSign(
        byte[] publicPointUncompressed,
        byte[] clientRandom,
        byte[] serverRandom,
        RSA serverPrivateKey);

    // Helper para verificar (cliente)
    public bool VerifySignature(byte[] clientRandom, byte[] serverRandom, RSA serverPublicKey);
}
```

Layout (RFC 5246 §7.4.3 + RFC 4492 §5.4 ServerECDHParams):
```
CurveType            1   (0x03 = named_curve)
NamedCurve           2   (0x0017 = secp256r1)
PublicPoint    var[1]    (1-byte length + 65 bytes uncompressed)
HashAlgorithm        1
SignatureAlgorithm   1
Signature      var[2]    (2-byte length + RSA signature, normalmente 256 bytes para RSA-2048)
```

**La firma se calcula sobre:**
```
clientRandom (32) || serverRandom (32) || ECParameters serializados (es decir, los primeros bytes hasta antes de SignatureAndHashAlgorithm)
```

= `clientRandom + serverRandom + curveType(1) + namedCurve(2) + publicPointVector(1+65)`

Para firmar: `RsaSigner.SignSha256Pkcs1(serverPrivateKey, blob)`.
Para verificar: `RsaSigner.VerifySha256Pkcs1(serverPublicKey, blob, signature)`.

## Criterios de Aceptación
- [ ] Round-trip Serialize/Parse OK
- [ ] CreateAndSign + VerifySignature en lados opuestos verifica TRUE
- [ ] Si se altera 1 byte del PublicPoint, VerifySignature devuelve FALSE
- [ ] Compila

## Dependencias
- Bloqueada por: TASK-002, TASK-005

## Comentarios
### 2026-05-09 - project-manager
> Si la firma no verifica en el cliente, el handshake muere con DecryptError. Atención al orden exacto del blob a firmar.
