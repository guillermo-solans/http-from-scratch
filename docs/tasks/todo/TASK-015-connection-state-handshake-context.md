---
id: TASK-015
title: TlsConnectionState + HandshakeContext
status: todo
priority: critical
phase: fase-4
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Crear estructuras de estado en `src/TlsLib/State/`:

### `TlsConnectionState.cs`
Estado tras handshake completo. Guarda lo necesario para Read/Write encriptados.

```csharp
public sealed class TlsConnectionState : IDisposable
{
    public bool IsClient { get; init; }
    public byte[] MasterSecret { get; init; } = default!;

    public AesGcmCipher ReadCipher { get; init; } = default!;
    public AesGcmCipher WriteCipher { get; init; } = default!;
    public byte[] ReadIv { get; init; } = default!;   // 4 bytes implicit IV
    public byte[] WriteIv { get; init; } = default!;

    public ulong ReadSeqNum;     // mutable, incrementa con cada record cifrado leído
    public ulong WriteSeqNum;    // mutable, incrementa con cada record cifrado escrito

    public bool ReadEncryptionActive;   // se activa al recibir ChangeCipherSpec del peer
    public bool WriteEncryptionActive;  // se activa al enviar ChangeCipherSpec

    public void Dispose() { ReadCipher?.Dispose(); WriteCipher?.Dispose(); }
}
```

### `HandshakeContext.cs`
Estado durante el handshake (transient).

```csharp
public sealed class HandshakeContext : IDisposable
{
    public bool IsClient { get; init; }
    public byte[] ClientRandom { get; set; } = default!;
    public byte[] ServerRandom { get; set; } = default!;
    public EcdheP256? Ecdhe { get; set; }   // nuestro keypair
    public byte[]? PeerEcdhePublicPoint { get; set; }
    public X509Certificate2? ServerCertificate { get; set; } // server: own cert; client: parsed peer cert
    public RSA? ServerPrivateKey { get; set; } // server only
    public RSA? ServerPublicKey { get; set; }  // client: extracted from cert
    public HandshakeHash HandshakeHash { get; init; } = new();
    public string? Sni { get; set; }
    public Action<string>? Logger { get; set; }

    public byte[]? PreMasterSecret { get; set; }  // 32 bytes ECDH shared secret
    public byte[]? MasterSecret { get; set; }     // 48 bytes
    public KeyBlock? KeyBlock { get; set; }

    public void Dispose() { Ecdhe?.Dispose(); HandshakeHash?.Dispose(); }
}
```

## Criterios de Aceptación
- [ ] Compila
- [ ] Dispose libera ECDHE y HandshakeHash
- [ ] No hay logica más allá de almacenamiento

## Dependencias
- Bloqueada por: TASK-004, TASK-005, TASK-008

## Comentarios
### 2026-05-09 - project-manager
> Contenedores de datos. Toda la lógica va en HandshakeStateMachine.
