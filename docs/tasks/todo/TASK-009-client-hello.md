---
id: TASK-009
title: ClientHello — serializar y deserializar
status: todo
priority: critical
phase: fase-3
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Crear `src/TlsLib/Handshake/ClientHello.cs`:

```csharp
public sealed class ClientHello
{
    public ushort ClientVersion { get; init; } = 0x0303; // TLS 1.2
    public byte[] Random { get; init; } = default!;       // 32 bytes
    public byte[] SessionId { get; init; } = Array.Empty<byte>(); // siempre vacío (sin resumption)
    public ushort[] CipherSuites { get; init; } = default!;       // ofreceremos solo 0xC02F + 0x00FF (SCSV opcional, lo omitimos)
    public byte[] CompressionMethods { get; init; } = new byte[] { 0x00 }; // null

    // Extensions:
    // - supported_groups (10): solo secp256r1 (0x0017)
    // - ec_point_formats (11): solo uncompressed (0x00)
    // - signature_algorithms (13): rsa_pkcs1_sha256 (0x0401)
    // - server_name (0): SNI con el host (opcional pero recomendado)

    public string? ServerName { get; init; }   // SNI

    public byte[] Serialize();                 // body sin el header de handshake
    public static ClientHello Parse(byte[] body); // valida y extrae
}
```

Layout serializado (RFC 5246 §7.4.1.2):
```
ClientVersion       2
Random             32
SessionId      var[1] (1 byte length + N)
CipherSuites   var[2] (2 byte length + N*2)
CompressionMethods var[1]
Extensions     var[2] (2 byte length + N) — opcional pero lo emitimos
```

Cada extension:
```
ExtensionType   2
ExtensionData var[2]
```

Para SNI (server_name extension type 0): ver RFC 6066 §3 (host_name type=0).

## Criterios de Aceptación
- [ ] Serialize() produce bytes que Parse() reconstruye igual
- [ ] El cliente que generamos, parseado, contiene CipherSuite 0xC02F y named curve secp256r1
- [ ] Test manual con dump hex revisado a ojo coincide con un ClientHello típico
- [ ] Compila

## Dependencias
- Bloqueada por: TASK-002

## Comentarios
### 2026-05-09 - project-manager
> Si no incluimos las extensions correctas, OpenSSL/curl rechazará. Para nuestro propósito (cliente nuestro vs servidor nuestro) no es bloqueante, pero lo hacemos bien.
