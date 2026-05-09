---
id: TASK-017
title: HandshakeStateMachine — lado SERVIDOR
status: todo
priority: critical
phase: fase-4
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Añadir a `src/TlsLib/State/HandshakeStateMachine.cs`:

```csharp
public static async Task<TlsConnectionState> RunAsServerAsync(
    Stream innerStream,
    TlsServerOptions options,
    CancellationToken ct);
```

Algoritmo (lado servidor):
1. Crear `HandshakeContext { IsClient = false, ServerRandom = SecureRandom.GenerateClientServerRandom(), Ecdhe = new EcdheP256(), ServerCertificate = options.ServerCertificate, ServerPrivateKey = options.ServerCertificate.GetRSAPrivateKey() }`
2. Crear readers/writers
3. **Recv ClientHello**: append, parse. Validar: ClientVersion soporta 0x0303, contiene cipher suite 0xC02F, contiene secp256r1 en supported_groups (o no envió la extensión), contiene rsa_pkcs1_sha256 (o no envió). Si no → Alert(HandshakeFailure).
4. Guardar ClientRandom y SNI (informativo).
5. **Send ServerHello**: con cipher suite 0xC02F, ServerRandom, sessionId vacío.
6. **Send Certificate**: con cert DER del options.ServerCertificate.
7. **Send ServerKeyExchange**: con nuestro ECDHE public point, firmado con ServerPrivateKey sobre clientRandom||serverRandom||ECDHParams.
8. **Send ServerHelloDone**.
9. **Recv ClientKeyExchange**: append, parse, guardar peer ECDHE public point.
10. Calcular `pre_master_secret = ECDH(nuestroEcdhe, peerEcdhePublicPoint)`.
11. Derivar master_secret + key_block. Instanciar ciphers (servidor: WriteKey=serverWriteKey, ReadKey=clientWriteKey).
12. **Recv ChangeCipherSpec del cliente**. Activar ReadEncryptionActive, reset ReadSeqNum=0.
13. **Recv client Finished** (cifrado). Decrypt, parse, verify contra hash hasta ClientKeyExchange. Si falla → Alert(DecryptError).
14. Append client Finished a HandshakeHash.
15. **Send ChangeCipherSpec**. Activar WriteEncryptionActive, reset WriteSeqNum=0.
16. **Send server Finished** (cifrado). Hash incluye ya el client Finished.
17. Devolver TlsConnectionState.

## Criterios de Aceptación
- [ ] Compila
- [ ] Junto con TASK-016, dos procesos en localhost completan handshake exitoso
- [ ] Logs claros con cada mensaje recibido/enviado
- [ ] Maneja correctamente cuando el cliente no soporta el cipher suite

## Dependencias
- Bloqueada por: TASK-007, TASK-009, TASK-010, TASK-011, TASK-012, TASK-013, TASK-014, TASK-015

## Comentarios
### 2026-05-09 - project-manager
> Espejo de TASK-016. Idealmente cógelo el mismo dev que TASK-016 para mantener convenciones consistentes.
