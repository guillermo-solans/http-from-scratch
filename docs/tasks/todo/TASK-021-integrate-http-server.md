---
id: TASK-021
title: Integrar TLS en HttpServer
status: todo
priority: high
phase: fase-4
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Modificar `src/HttpServerApp/Server/HttpServer.cs`:

```csharp
public class HttpServer
{
    private readonly int _port;
    private readonly Router _router;
    private readonly Func<HttpRequest, Task<HttpResponse>>? _fallback;
    private readonly X509Certificate2? _serverCertificate;   // NUEVO
    private readonly Action<string>? _tlsLogger;             // NUEVO

    public HttpServer(int port, Router router,
                      Func<HttpRequest, Task<HttpResponse>>? fallback = null,
                      X509Certificate2? serverCertificate = null,
                      Action<string>? tlsLogger = null)
    {
        _port = port;
        _router = router;
        _fallback = fallback;
        _serverCertificate = serverCertificate;
        _tlsLogger = tlsLogger;
    }
    // ...
}
```

En `HandleClientAsync`, reemplazar el bloque que obtiene el stream:

```csharp
using (client)
{
    Stream stream = client.GetStream();
    TlsStream? tlsStream = null;

    try
    {
        if (_serverCertificate is not null)
        {
            try
            {
                tlsStream = TlsServerFactory.Accept(stream, new TlsServerOptions
                {
                    ServerCertificate = _serverCertificate,
                    Logger = _tlsLogger
                });
                stream = tlsStream;
            }
            catch (TlsException ex)
            {
                Console.WriteLine($"[server] TLS handshake failed from {remoteEndPoint}: {ex.Message}");
                return;
            }
        }

        // ... resto idéntico (RequestReader.ReadAsync, dispatch, SendAsync) ...
    }
    finally
    {
        tlsStream?.Dispose();
    }
}
```

Banner de arranque debe indicar protocolo:
```csharp
var protocol = _serverCertificate is null ? "http" : "https";
Console.WriteLine($"[server] Listening on {protocol}://localhost:{_port}");
```

## Criterios de Aceptación
- [ ] Compila
- [ ] Si serverCertificate=null, comportamiento HTTP idéntico al actual (sin regresión)
- [ ] Si serverCertificate!=null, los clientes deben hacer TLS handshake antes de hablar HTTP
- [ ] Errores de handshake no rompen el listener (siguen aceptando otros clientes)

## Dependencias
- Bloqueada por: TASK-019

## Comentarios
### 2026-05-09 - project-manager
> Cambio mínimo. La excepción TlsException la captura aquí para no spamear stack traces.
