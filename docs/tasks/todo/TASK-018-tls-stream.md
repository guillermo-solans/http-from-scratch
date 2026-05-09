---
id: TASK-018
title: TlsStream — Read/Write con AES-GCM, ChangeCipherSpec, CloseNotify
status: todo
priority: critical
phase: fase-4
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Crear `src/TlsLib/TlsStream.cs`. Es la clase pública principal: hereda de `Stream` y se le pasan al HttpClient/HttpServer.

```csharp
public sealed class TlsStream : Stream
{
    private readonly Stream _inner;
    private readonly TlsConnectionState _state;
    private readonly TlsRecordReader _recordReader;
    private readonly TlsRecordWriter _recordWriter;
    private readonly Action<string>? _logger;

    private byte[] _readBuffer = Array.Empty<byte>();
    private int _readBufferOffset;
    private bool _peerClosed;
    private bool _disposed;

    internal TlsStream(Stream inner, TlsConnectionState state, Action<string>? logger);

    // Stream API
    public override int Read(byte[] buffer, int offset, int count);
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct);
    public override void Write(byte[] buffer, int offset, int count);
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct);
    public override void Flush() => _inner.Flush();
    public override Task FlushAsync(CancellationToken ct) => _inner.FlushAsync(ct);

    // Read interno: si _readBuffer está vacío, lee un record entero, descifra, lo guarda.
    // Luego sirve de _readBuffer hasta que se consume; entonces lee otro record.
    // Si el record es Alert: si CloseNotify, marca _peerClosed y devuelve 0 EOF.
    //   Si Fatal, lanza TlsException.

    // Write interno: chunkea por 16384 bytes max, encripta cada chunk, lo envía como AppData record.

    protected override void Dispose(bool disposing); // Envía CloseNotify, luego dispone _state e _inner.
}
```

### Construcción del nonce y AAD para AES-GCM (RFC 5288):
```
seqNum = uint64 big-endian del contador (read o write según sea)
nonce  = implicit_iv(4) || seqNum(8)              // 12 bytes
aad    = seqNum(8) || ContentType(1) || Version(2) || PlaintextLength(2)  // 13 bytes

ciphertext = AesGcm.Encrypt(key, nonce, plaintext, aad) → contiene tag de 16 bytes

Wire payload del record: explicit_nonce_part(8 = seqNum) || ciphertext_with_tag
```

Al leer:
```
explicitNonce = primeros 8 bytes del payload
ciphertext = resto
nonce = implicit_iv(4) || explicitNonce(8)
plaintextLength = totalPayload - 8 - 16   // (excluyendo nonce explicito y tag)
aad = readSeqNum(8) || ContentType(1) || Version(2) || plaintextLength(2)
plaintext = AesGcm.Decrypt(key, nonce, ciphertext, aad)
readSeqNum++
```

### Manejo de Alerts
Si llega un record con ContentType=Alert, descifrar, examinar:
- Level=Warning, Description=CloseNotify → marcar _peerClosed, futuras lecturas devuelven 0
- Level=Fatal o Level=Warning con otra descripción → TlsException

### CloseNotify on Dispose
Antes de cerrar `_inner`, enviar Alert(Warning, CloseNotify) cifrado. Best-effort: si falla, ignorar.

## Criterios de Aceptación
- [ ] Compila
- [ ] Con un servidor en TASK-017 que acepte el handshake, podemos enviar "GET / HTTP/1.1\r\n\r\n" y recibir la respuesta
- [ ] Múltiples Write+Read consecutivos funcionan (seqNum incrementando)
- [ ] CloseNotify enviado en Dispose
- [ ] Alterar 1 byte de un record cifrado lanza TlsException(BadRecordMac)

## Dependencias
- Bloqueada por: TASK-006, TASK-015, TASK-016, TASK-017

## Comentarios
### 2026-05-09 - project-manager
> Punto de exposición pública de TlsLib. La interfaz Stream es la que mantiene compatibilidad con HttpClient/HttpServer existentes.
