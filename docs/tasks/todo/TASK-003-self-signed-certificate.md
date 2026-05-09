---
id: TASK-003
title: Generador de certificado X.509 RSA-2048 autofirmado
status: todo
priority: high
phase: fase-1
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Crear `src/TlsLib/Certificates/SelfSignedCertificateGenerator.cs`:

```csharp
public static class SelfSignedCertificateGenerator
{
    public static X509Certificate2 Generate(
        string subjectName = "CN=localhost",
        int rsaKeySizeBits = 2048,
        TimeSpan? validity = null);
}
```

Implementación usando BCL (no BouncyCastle):
- `RSA.Create(rsaKeySizeBits)`
- `CertificateRequest req = new(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);`
- Añadir SAN extension con `dns=localhost` y `dns=127.0.0.1`
- Añadir BasicConstraints (CA=false), KeyUsage (DigitalSignature, KeyEncipherment, DataEncipherment)
- Añadir ExtendedKeyUsage con `1.3.6.1.5.5.7.3.1` (serverAuth)
- `req.CreateSelfSigned(notBefore: DateTimeOffset.UtcNow.AddMinutes(-5), notAfter: ...)`
- Validez por defecto: 365 días
- IMPORTANTE: el certificado retornado debe contener la clave privada RSA (usable como signer en ServerKeyExchange)

## Criterios de Aceptación
- [ ] `Generate()` devuelve `X509Certificate2` con `HasPrivateKey == true`
- [ ] El certificado se puede serializar a DER vía `cert.Export(X509ContentType.Cert)`
- [ ] El certificado se puede usar para firmar y verificar con la RSA embebida
- [ ] Compila y no requiere paquetes NuGet adicionales

## Dependencias
- Bloqueada por: TASK-001

## Comentarios
### 2026-05-09 - project-manager
> Lo usará HttpServerApp en arranque para tener un cert listo.
