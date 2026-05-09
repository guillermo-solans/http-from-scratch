---
id: TASK-006
title: TlsRecord + TlsRecordReader + TlsRecordWriter (capa Record sin cifrar)
status: todo
priority: critical
phase: fase-2
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Crear la capa Record de TLS en `src/TlsLib/Records/`:

### `TlsRecord.cs`
```csharp
public readonly struct TlsRecord
{
    public ContentType Type { get; }
    public ushort Version { get; }   // 0x0303 = TLS 1.2
    public byte[] Payload { get; }   // longitud máxima 16384 (más overhead si cifrado)
}
```

Header de record TLS = 5 bytes: ContentType(1) || Version(2) || Length(2). Payload máximo en claro = 2^14 = 16384 bytes.

### `TlsRecordReader.cs`
```csharp
public sealed class TlsRecordReader
{
    public TlsRecordReader(Stream innerStream);
    public async Task<TlsRecord> ReadAsync(CancellationToken ct);
    // Lee 5 bytes header, valida ContentType y Version, lee Length bytes payload.
    // Si Length > 16384 + 2048 (slack para cifrado), lanza TlsException(RecordOverflow).
}
```

### `TlsRecordWriter.cs`
```csharp
public sealed class TlsRecordWriter
{
    public TlsRecordWriter(Stream innerStream);
    public async Task WriteAsync(ContentType type, ReadOnlyMemory<byte> payload, CancellationToken ct);
    // Si payload > 16384, fragmentar en múltiples records.
}
```

**Importante:** Esta capa NO sabe nada de cifrado. El cifrado lo aplica `TlsStream` por encima envolviendo el payload antes de pasarlo a `WriteAsync`. Después de ChangeCipherSpec, el "payload" entregado a `WriteAsync` ya viene cifrado (con su nonce explícito + ciphertext + tag).

## Criterios de Aceptación
- [ ] Round-trip: WriteAsync(Handshake, [1,2,3]) y ReadAsync devuelve TlsRecord{Type=Handshake, Payload=[1,2,3]}
- [ ] Fragmentación: WriteAsync con payload de 20000 bytes produce 2 records (16384 + 3616)
- [ ] Lectura de Length=20000 lanza TlsException antes de OOM
- [ ] Compila

## Dependencias
- Bloqueada por: TASK-001, TASK-002

## Comentarios
### 2026-05-09 - project-manager
> Capa más baja del protocolo. No tocar cifrado aquí.
