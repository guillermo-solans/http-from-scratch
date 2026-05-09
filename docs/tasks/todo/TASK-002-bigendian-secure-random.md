---
id: TASK-002
title: BigEndianReader / BigEndianWriter / SecureRandom
status: todo
priority: critical
phase: fase-1
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Crear utilidades de serialización en `src/TlsLib/Util/`:

**`BigEndianWriter.cs`** — wrapper sobre `MemoryStream`/`List<byte>`:
- `WriteUInt8(byte)`, `WriteUInt16(ushort)`, `WriteUInt24(uint)`, `WriteUInt32(uint)` (todos big-endian)
- `WriteBytes(ReadOnlySpan<byte>)`
- `WriteVector8(ReadOnlySpan<byte>)` — prefija longitud con uint8
- `WriteVector16(ReadOnlySpan<byte>)` — prefija longitud con uint16
- `WriteVector24(ReadOnlySpan<byte>)` — prefija longitud con uint24
- `byte[] ToArray()`
- Soporte para "reservar" un placeholder de longitud y rellenarlo después (para mensajes con longitud al inicio que no se conoce hasta el final).

**`BigEndianReader.cs`** — wrapper sobre `byte[]` + offset:
- `ReadUInt8/16/24/32`, `ReadBytes(int)`, `ReadVector8/16/24` (devuelven `byte[]`)
- `int Remaining`, `int Position`
- Lanza `TlsException(AlertDescription.DecodeError)` si se lee más allá del buffer.

**`SecureRandom.cs`**:
- `static byte[] GenerateRandom(int length)` usando `RandomNumberGenerator.Fill`
- `static byte[] GenerateClientServerRandom()` — 32 bytes (TLS 1.2: 4 bytes timestamp gmt + 28 random; pero admitido también 32 random puros — usaremos 32 random puros, conforme RFC moderno)

## Criterios de Aceptación
- [ ] Test manual: round-trip `WriteUInt16(0x1234)` → `ReadUInt16()` devuelve `0x1234`
- [ ] WriteVector16 con un array de 5 bytes produce 7 bytes totales (2 de longitud + 5)
- [ ] No hay dependencias externas, solo BCL
- [ ] Compila

## Dependencias
- Bloqueada por: TASK-001

## Comentarios
### 2026-05-09 - project-manager
> Utilidades base, las usan TODOS los demás módulos.
