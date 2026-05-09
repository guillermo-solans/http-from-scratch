---
id: TASK-004
title: PrfSha256 (P_SHA256) + KeyDerivation (master_secret + key_block)
status: todo
priority: critical
phase: fase-1
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Crear `src/TlsLib/Crypto/PrfSha256.cs`:

Implementar PRF de TLS 1.2 según RFC 5246 §5:
```
P_SHA256(secret, seed) = HMAC_SHA256(secret, A(1) + seed) ||
                          HMAC_SHA256(secret, A(2) + seed) || ...
A(0) = seed
A(i) = HMAC_SHA256(secret, A(i-1))
```

API:
```csharp
public static class PrfSha256
{
    public static byte[] Prf(byte[] secret, string label, byte[] seed, int outputLength);
    // Internamente: PRF(secret, label, seed) = P_SHA256(secret, ASCII(label) || seed)
}
```

Crear `src/TlsLib/Crypto/KeyDerivation.cs`:

```csharp
public static class KeyDerivation
{
    // master_secret = PRF(pre_master_secret, "master secret", clientRandom||serverRandom, 48)
    public static byte[] DeriveMasterSecret(byte[] preMasterSecret, byte[] clientRandom, byte[] serverRandom);

    // key_block = PRF(master_secret, "key expansion", serverRandom||clientRandom, ...)
    // Para AES-128-GCM (AEAD): NO necesitamos MAC keys, solo:
    //   client_write_key (16), server_write_key (16), client_write_IV (4), server_write_IV (4)
    // Total: 40 bytes
    public static KeyBlock DeriveKeyBlock(byte[] masterSecret, byte[] clientRandom, byte[] serverRandom);

    // verify_data = PRF(master_secret, finished_label, SHA256(handshake_messages), 12)
    // finished_label = "client finished" o "server finished"
    public static byte[] DeriveFinishedVerifyData(byte[] masterSecret, string label, byte[] handshakeHash);
}

public sealed record KeyBlock(byte[] ClientWriteKey, byte[] ServerWriteKey, byte[] ClientWriteIv, byte[] ServerWriteIv);
```

Para AES-GCM, el "implicit IV" es de 4 bytes; los 8 bytes restantes del nonce de 12 bytes son explicit (vienen en el record o se construyen del seq num).

## Criterios de Aceptación
- [ ] PRF con vector de prueba conocido (puede usarse el de openssl o RFC 5246) coincide
- [ ] DeriveMasterSecret produce 48 bytes
- [ ] DeriveKeyBlock produce KeyBlock con las 4 cantidades correctas (16+16+4+4=40)
- [ ] DeriveFinishedVerifyData produce 12 bytes
- [ ] Compila

## Dependencias
- Bloqueada por: TASK-001

## Comentarios
### 2026-05-09 - project-manager
> Núcleo criptográfico. Probar con vector de prueba antes de seguir.
