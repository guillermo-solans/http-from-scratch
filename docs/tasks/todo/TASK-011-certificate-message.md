---
id: TASK-011
title: CertificateMessage — lista de certificados DER
status: todo
priority: critical
phase: fase-3
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Crear `src/TlsLib/Handshake/CertificateMessage.cs`:

```csharp
public sealed class CertificateMessage
{
    public byte[][] CertificatesDer { get; init; } = default!;

    public byte[] Serialize();
    public static CertificateMessage Parse(byte[] body);

    // Helper para el cliente: parsea el primer cert y extrae la RSA public key
    public RSA ExtractServerRsaPublicKey();
}
```

Layout (RFC 5246 §7.4.2):
```
CertificateList   var[3]    // 3-byte length total
  for each cert:
    CertData      var[3]    // 3-byte length, contenido = DER
```

Para extraer la RSA pública desde el cliente:
- `var cert = X509CertificateLoader.LoadCertificate(certificatesDer[0]);` (o `new X509Certificate2(...)` que aún funciona en .NET 10 pero está obsoleto)
- `var rsa = cert.GetRSAPublicKey();`

## Criterios de Aceptación
- [ ] Round-trip Serialize/Parse con 1 cert OK
- [ ] ExtractServerRsaPublicKey devuelve RSA usable para verify
- [ ] Compila

## Dependencias
- Bloqueada por: TASK-002, TASK-003

## Comentarios
### 2026-05-09 - project-manager
> Solo enviamos el cert del servidor (cadena de 1 elemento). Sin CA intermedios.
