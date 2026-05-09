---
id: TASK-008
title: HandshakeHash — acumulador SHA256 de mensajes de handshake
status: todo
priority: high
phase: fase-2
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Crear `src/TlsLib/Crypto/HandshakeHash.cs`:

```csharp
public sealed class HandshakeHash : IDisposable
{
    public HandshakeHash();   // Internamente IncrementalHash.CreateHash(SHA256)
    // Acumula los bytes RAW del mensaje de handshake (incluyendo su header de 4 bytes type+length)
    public void Append(ReadOnlySpan<byte> handshakeMessageBytes);
    // GetCurrentHash NO debe consumir el estado (poder seguir añadiendo después)
    public byte[] GetCurrentHash();
    public void Dispose();
}
```

**MUY IMPORTANTE:**
- El hash se calcula sobre los bytes EXACTOS de los mensajes de handshake según fueron transmitidos en el wire (incluido su header de 4 bytes type+length).
- NO incluye los headers de Record (5 bytes).
- NO incluye ChangeCipherSpec ni alerts.
- El cliente y el servidor deben llegar al mismo hash en cada punto.

`IncrementalHash` con SHA256 permite obtener un snapshot intermedio sin destruir el estado vía `GetHashAndReset()` y luego volver a empezar — pero eso resetea. Mejor: clonar el estado o llevar buffer interno y rehashearlo en `GetCurrentHash`. Implementación recomendada: buffer interno `List<byte>` + recalcular SHA256 al pedir el hash. Sencillo y suficiente para handshake (pocos KB).

## Criterios de Aceptación
- [ ] Append+GetCurrentHash devuelve SHA256 de la concatenación de todos los mensajes appendeados
- [ ] GetCurrentHash llamado dos veces seguidas devuelve lo mismo (idempotente, no destructivo)
- [ ] Compila

## Dependencias
- Bloqueada por: TASK-001

## Comentarios
### 2026-05-09 - project-manager
> Crítico para Finished verify_data. Si los hashes divergen, Finished falla y no hay forma de debuggear.
