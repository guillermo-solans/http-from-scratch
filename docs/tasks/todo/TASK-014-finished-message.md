---
id: TASK-014
title: FinishedMessage — verify_data via PRF(handshake_hash)
status: todo
priority: critical
phase: fase-3
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Crear `src/TlsLib/Handshake/FinishedMessage.cs`:

```csharp
public sealed class FinishedMessage
{
    public byte[] VerifyData { get; init; } = default!; // 12 bytes

    public byte[] Serialize() => VerifyData; // body = exactamente los 12 bytes
    public static FinishedMessage Parse(byte[] body)
    {
        if (body.Length != 12)
            throw new TlsException("Finished verify_data debe ser 12 bytes", AlertDescription.DecodeError);
        return new() { VerifyData = body };
    }

    public static FinishedMessage Build(byte[] masterSecret, bool isClient, byte[] handshakeHashUpToNow)
    {
        var label = isClient ? "client finished" : "server finished";
        var data = KeyDerivation.DeriveFinishedVerifyData(masterSecret, label, handshakeHashUpToNow);
        return new() { VerifyData = data };
    }

    public bool Verify(byte[] masterSecret, bool isPeerClient, byte[] expectedHandshakeHashUpToNow)
    {
        var expected = Build(masterSecret, isPeerClient, expectedHandshakeHashUpToNow);
        return CryptographicOperations.FixedTimeEquals(VerifyData, expected.VerifyData);
    }
}
```

**OJO con el handshake hash:**
- El cliente calcula su Finished sobre `SHA256(ClientHello..ClientKeyExchange)` (NO incluye ChangeCipherSpec, NO incluye su propio Finished aún).
- El servidor verifica el Finished del cliente contra `SHA256(ClientHello..ClientKeyExchange)`.
- El servidor calcula su Finished sobre `SHA256(ClientHello..ClientFinished)` (SÍ incluye el Finished del cliente).
- El cliente verifica el Finished del servidor contra `SHA256(ClientHello..ClientFinished)`.

ChangeCipherSpec NO se hashea. Es ContentType 20, no 22.

## Criterios de Aceptación
- [ ] Build + Verify en lados opuestos con mismas entradas devuelve TRUE
- [ ] Si el hash difiere en 1 byte, Verify devuelve FALSE
- [ ] Usa FixedTimeEquals (no `==` ni SequenceEqual)
- [ ] Compila

## Dependencias
- Bloqueada por: TASK-004, TASK-008

## Comentarios
### 2026-05-09 - project-manager
> Confirmación criptográfica del handshake. Si esto pasa, las claves coinciden.
