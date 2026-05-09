---
id: TASK-010
title: ServerHello — serializar y deserializar
status: todo
priority: critical
phase: fase-3
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Crear `src/TlsLib/Handshake/ServerHello.cs`:

```csharp
public sealed class ServerHello
{
    public ushort ServerVersion { get; init; } = 0x0303;
    public byte[] Random { get; init; } = default!;       // 32 bytes
    public byte[] SessionId { get; init; } = Array.Empty<byte>();
    public ushort CipherSuite { get; init; } = 0xC02F;
    public byte CompressionMethod { get; init; } = 0x00;

    // Extensions opcionales en respuesta — las omitimos (vacías)

    public byte[] Serialize();
    public static ServerHello Parse(byte[] body);
}
```

Layout (RFC 5246 §7.4.1.3):
```
ServerVersion       2
Random             32
SessionId      var[1]
CipherSuite         2  (uint16, no es vector — un único valor)
CompressionMethod   1  (un único byte)
Extensions     var[2] (puede estar vacío o ausente)
```

Validaciones en Parse:
- ServerVersion == 0x0303 (si no, lanzar TlsException(ProtocolVersion))
- CipherSuite == 0xC02F (si no, lanzar TlsException(HandshakeFailure))
- CompressionMethod == 0x00 (si no, lanzar TlsException(IllegalParameter))

## Criterios de Aceptación
- [ ] Round-trip Serialize/Parse OK
- [ ] Rechaza ServerVersion 0x0301 con TlsException
- [ ] Rechaza CipherSuite distinto de 0xC02F
- [ ] Compila

## Dependencias
- Bloqueada por: TASK-002

## Comentarios
### 2026-05-09 - project-manager
> Cliente lo parsea, servidor lo emite. Asumimos que solo soportamos un cipher suite — si el cliente no lo ofrece, fallar con HandshakeFailure.
