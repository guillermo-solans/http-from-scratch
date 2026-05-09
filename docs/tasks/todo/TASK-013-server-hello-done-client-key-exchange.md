---
id: TASK-013
title: ServerHelloDone + ClientKeyExchange
status: todo
priority: high
phase: fase-3
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

### `src/TlsLib/Handshake/ServerHelloDone.cs`

```csharp
public sealed class ServerHelloDone
{
    public byte[] Serialize() => Array.Empty<byte>();
    public static ServerHelloDone Parse(byte[] body)
    {
        if (body.Length != 0) throw new TlsException("ServerHelloDone debe ser vacío", AlertDescription.DecodeError);
        return new();
    }
}
```

Cuerpo vacío. Header del handshake quedará: type=14 || length=0.

### `src/TlsLib/Handshake/ClientKeyExchange.cs`

Para cipher suite ECDHE_RSA_*, el ClientKeyExchange contiene el ECPoint público del cliente:

```csharp
public sealed class ClientKeyExchange
{
    public byte[] PublicPointUncompressed { get; init; } = default!; // 65 bytes (0x04 || X || Y)

    public byte[] Serialize();
    public static ClientKeyExchange Parse(byte[] body);
}
```

Layout (RFC 4492 §5.7):
```
ECPoint   var[1]   (1-byte length + 65 bytes)
```

**Importante:** La `pre_master_secret` NO se envía. Cliente y servidor la calculan independientemente vía ECDH(suEcdheKeyPair, otrosPublicPoint).

## Criterios de Aceptación
- [ ] ServerHelloDone.Serialize() devuelve array vacío
- [ ] ClientKeyExchange round-trip OK
- [ ] Compila

## Dependencias
- Bloqueada por: TASK-002

## Comentarios
### 2026-05-09 - project-manager
> Tarea sencilla, ideal para cerrar fase-3 si quien la coge ya tiene el ritmo.
