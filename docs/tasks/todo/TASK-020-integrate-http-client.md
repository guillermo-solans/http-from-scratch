---
id: TASK-020
title: Integrar TLS en SocketHttpClient
status: todo
priority: high
phase: fase-4
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Modificar `src/HttpClientApp/SocketHttpClient.cs`:

```csharp
public static (HttpResponse Response, TimeSpan Elapsed) Send(
    HttpRequest request,
    ParsedUrl url,
    int timeoutMs = 30000,
    bool useTls = false,
    Action<string>? tlsLogger = null)
{
    var sw = System.Diagnostics.Stopwatch.StartNew();

    using var tcp = new TcpClient { ReceiveTimeout = timeoutMs, SendTimeout = timeoutMs };
    tcp.Connect(url.Host, url.Port);

    Stream stream = tcp.GetStream();
    TlsStream? tlsStream = null;
    try
    {
        if (useTls)
        {
            tlsStream = TlsClientFactory.Connect(stream, url.Host, new TlsClientOptions { Logger = tlsLogger });
            stream = tlsStream;
        }

        var raw = request.Serialize();
        stream.Write(raw, 0, raw.Length);
        stream.Flush();

        var isHead = string.Equals(request.Method, "HEAD", StringComparison.OrdinalIgnoreCase);
        var response = HttpResponseReader.ReadFromStream(stream, isHead);

        sw.Stop();
        return (response, sw.Elapsed);
    }
    finally
    {
        tlsStream?.Dispose();
    }
}
```

**Reglas:**
- No tocar `HttpRequest`, `HttpResponse`, `HttpResponseReader`. Su API sobre Stream es exactamente la que necesitamos.
- `useTls` se pasa explícitamente desde Program.cs.
- Si `useTls=true` y `url.Port` no está explícito, debería ser 443 (pero como en este proyecto vamos a usar 8443, lo dejamos al Program.cs).

## Criterios de Aceptación
- [ ] Compila
- [ ] Tests existentes (sin TLS) siguen pasando — no hay regresión
- [ ] Llamada con useTls=true contra el servidor TLS funciona end-to-end

## Dependencias
- Bloqueada por: TASK-019

## Comentarios
### 2026-05-09 - project-manager
> Cambio mínimo. Mantener la firma compatible (parámetros nuevos opcionales con default).
