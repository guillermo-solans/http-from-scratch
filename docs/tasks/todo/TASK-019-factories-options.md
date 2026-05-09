---
id: TASK-019
title: TlsClientFactory + TlsServerFactory + TlsClientOptions/ServerOptions
status: todo
priority: high
phase: fase-4
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Crear los puntos de entrada públicos en `src/TlsLib/`:

### `TlsClientFactory.cs`
```csharp
public static class TlsClientFactory
{
    public static TlsStream Connect(Stream innerStream, string? serverName = null, TlsClientOptions? options = null)
    {
        options ??= new TlsClientOptions();
        var state = HandshakeStateMachine.RunAsClientAsync(innerStream, options, serverName, CancellationToken.None)
                                          .GetAwaiter().GetResult();
        return new TlsStream(innerStream, state, options.Logger);
    }

    public static async Task<TlsStream> ConnectAsync(Stream innerStream, string? serverName, TlsClientOptions? options, CancellationToken ct);
}
```

### `TlsServerFactory.cs`
```csharp
public static class TlsServerFactory
{
    public static TlsStream Accept(Stream innerStream, TlsServerOptions options) { ... }
    public static async Task<TlsStream> AcceptAsync(Stream innerStream, TlsServerOptions options, CancellationToken ct);
}
```

### `TlsClientOptions.cs` / `TlsServerOptions.cs`
Según contrato (sección 3.2). Valores por defecto: `AllowSelfSignedCertificates=true`, Logger=null.

`TlsServerOptions` valida en constructor que `ServerCertificate.HasPrivateKey == true` y que la clave es RSA.

## Criterios de Aceptación
- [ ] Compila
- [ ] La API es invocable desde HttpClientApp y HttpServerApp
- [ ] Si el handshake falla, la factory lanza TlsException

## Dependencias
- Bloqueada por: TASK-018

## Comentarios
### 2026-05-09 - project-manager
> Wrapper trivial. Si TASK-018 está bien, esto son ~50 líneas.
