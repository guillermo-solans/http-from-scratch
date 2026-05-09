---
id: TASK-007
title: HandshakeReader + HandshakeWriter (reensamblado de mensajes)
status: todo
priority: high
phase: fase-2
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Un mensaje de Handshake puede:
1. Estar fragmentado en varios records.
2. Compartir un record con otros mensajes de handshake.

Crear `src/TlsLib/Handshake/HandshakeReader.cs`:

```csharp
public sealed class HandshakeReader
{
    public HandshakeReader(TlsRecordReader recordReader);
    // Devuelve (HandshakeType, body bytes SIN incluir el header de 4 bytes).
    // Lee tantos records como sean necesarios para reconstruir UN mensaje completo.
    // Si llega un record que no es Handshake, lanza TlsException(UnexpectedMessage)
    // (excepción: ChangeCipherSpec se trata fuera, no llega aquí).
    public async Task<(HandshakeType type, byte[] body)> ReadMessageAsync(CancellationToken ct);
}
```

Cada handshake message tiene header 4 bytes: HandshakeType(1) || Length(3). Body sigue.

Crear `src/TlsLib/Handshake/HandshakeWriter.cs`:

```csharp
public sealed class HandshakeWriter
{
    public HandshakeWriter(TlsRecordWriter recordWriter);
    public async Task WriteMessageAsync(HandshakeType type, byte[] body, CancellationToken ct);
    // Empaqueta type(1) || length(3) || body y lo emite como Handshake record.
}
```

Mantener un buffer interno en HandshakeReader para el caso de que un record traiga 2 mensajes (típico: Certificate + ServerKeyExchange + ServerHelloDone en un solo record).

## Criterios de Aceptación
- [ ] Round-trip de mensaje pequeño funciona
- [ ] Si el record contiene 2 handshake messages consecutivos, dos llamadas a ReadMessageAsync devuelven los dos sin leer un record nuevo
- [ ] Si un mensaje > 16384 fragmentado en 2 records, ReadMessageAsync lo reensambla
- [ ] Compila

## Dependencias
- Bloqueada por: TASK-006

## Comentarios
### 2026-05-09 - project-manager
> Punto crítico de bugs. Probar con cuidado el caso de mensajes concatenados en un mismo record.
