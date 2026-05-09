---
id: TASK-016
title: HandshakeStateMachine — lado CLIENTE
status: todo
priority: critical
phase: fase-4
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Crear `src/TlsLib/State/HandshakeStateMachine.Client.cs` (o método dentro de HandshakeStateMachine.cs):

```csharp
public sealed class HandshakeStateMachine
{
    public static async Task<TlsConnectionState> RunAsClientAsync(
        Stream innerStream,
        TlsClientOptions options,
        string? sni,
        CancellationToken ct);
}
```

Algoritmo (lado cliente):
1. Crear `HandshakeContext { IsClient = true, ClientRandom = SecureRandom.GenerateClientServerRandom(), Ecdhe = new EcdheP256(), Sni = sni }`
2. Crear `TlsRecordReader/Writer`, `HandshakeReader/Writer` sobre `innerStream`
3. **Send ClientHello**: construir, append a HandshakeHash, write
4. **Recv ServerHello**: read, append a HandshakeHash, parse, validar version+suite, guardar ServerRandom
5. **Recv Certificate**: read, append, parse, extraer RSA public key. Si options.AllowSelfSignedCertificates=true, NO validar cadena. Si false, validar (en este proyecto siempre true).
6. **Recv ServerKeyExchange**: read, append, parse, llamar `VerifySignature(clientRandom, serverRandom, ServerPublicKey)`. Si falla → TlsException(DecryptError).
7. **Recv ServerHelloDone**: read, append, parse (debe ser vacío)
8. **Calcular pre_master_secret** = ECDH(nuestraEcdhe, peerEcdhePublicPoint) — 32 bytes (X coord)
9. **Send ClientKeyExchange**: con nuestra pública. Append a HandshakeHash.
10. **Derivar master_secret y key_block**, instanciar AesGcmCipher para read y write con las keys correctas (cliente: WriteKey=clientWriteKey, ReadKey=serverWriteKey, etc.)
11. **Send ChangeCipherSpec**: 1 byte 0x01. ContentType=20. NO se hashea. Activar `WriteEncryptionActive=true`. Reset `WriteSeqNum=0`.
12. **Send client Finished**: build con `HandshakeHash.GetCurrentHash()` ANTES del ChangeCipherSpec (es decir, hash hasta ClientKeyExchange inclusive). Append a HandshakeHash. Encrypt y send.
13. **Recv ChangeCipherSpec del servidor**: leer record con ContentType=20, contenido 0x01. Activar `ReadEncryptionActive=true`. Reset `ReadSeqNum=0`.
14. **Recv server Finished**: read encrypted record, decrypt, parse Finished. Verify contra `HandshakeHash.GetCurrentHash()` (que incluye client Finished).
15. Devolver TlsConnectionState completo.

**Manejo de errores:**
- Cualquier excepción interna → enviar Alert(Fatal, descripción) si es posible, luego rethrow como TlsException
- Timeout: parámetro CancellationToken en cada Read/Write

**Logging:**
- `options.Logger?.Invoke($"[tls/client] -> ClientHello (random={hex})")` en cada paso
- Loguear el cipher suite negociado, el peer cert subject, el verify_data hex

## Criterios de Aceptación
- [ ] Compila
- [ ] Cuando el servidor (TASK-017) está listo, dos consolas terminan handshake en localhost
- [ ] Los logs muestran cada mensaje del handshake con tamaño y resumen
- [ ] El TlsConnectionState retornado tiene IsClient=true, MasterSecret de 48 bytes, ciphers no nulos

## Dependencias
- Bloqueada por: TASK-007, TASK-009, TASK-010, TASK-011, TASK-012, TASK-013, TASK-014, TASK-015

## Comentarios
### 2026-05-09 - project-manager
> Cuidado con el ORDEN de los appends al HandshakeHash. Si te equivocas, Finished no verificará. Loguea el SHA256 hash en cada paso para debug.
