---
id: TASK-022
title: CLI flags --tls en HttpClientApp y HttpServerApp
status: todo
priority: medium
phase: fase-4
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

### `src/HttpServerApp/Program.cs`
Aceptar argumentos:
- `--tls` o `--https`: activa TLS, genera cert autofirmado en arranque
- `--port N`: puerto (default: 8080 si http, 8443 si https)
- `--cert path`: opcional, cargar cert desde PFX (default: generar autofirmado)

Implementación mínima:
```csharp
bool useTls = args.Contains("--tls") || args.Contains("--https");
int port = ParseIntFlag(args, "--port") ?? (useTls ? 8443 : 8080);
X509Certificate2? cert = null;
if (useTls)
{
    cert = SelfSignedCertificateGenerator.Generate("CN=localhost");
    Console.WriteLine($"[server] Generated self-signed cert: {cert.Thumbprint}");
}
Action<string> tlsLogger = msg => Console.WriteLine(msg);
var server = new HttpServer(port, router, fallback, cert, useTls ? tlsLogger : null);
await server.RunAsync(cts.Token);
```

### `src/HttpClientApp/Program.cs`
Aceptar:
- URLs `https://...` activan TLS automáticamente
- Flag global `--tls` para forzar (útil en REPL)
- Logger TLS cuando hay flag `--verbose-tls`

Cambio mínimo en el dispatch del REPL:
```csharp
bool useTls = url.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
var (response, elapsed) = SocketHttpClient.Send(request, url, timeoutMs, useTls,
    verboseTls ? (msg => Console.WriteLine(msg)) : null);
```

Asegurar que `UrlParser` ya soporta `https://` (probablemente sí, solo cambia scheme y default port a 443). Si default port para https no está implementado, asignar 443 cuando scheme=https.

## Criterios de Aceptación
- [ ] `dotnet run --project src/HttpServerApp -- --tls --port 8443` arranca con banner "Listening on https://localhost:8443"
- [ ] `dotnet run --project src/HttpClientApp` con URL "https://localhost:8443/cats" funciona
- [ ] Modo HTTP (sin --tls) sigue funcionando idéntico

## Dependencias
- Bloqueada por: TASK-020, TASK-021

## Comentarios
### 2026-05-09 - project-manager
> UX para la demo. Que arrancar la demo sea de un solo comando.
