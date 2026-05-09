using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using HttpLib;
using TlsLib;

namespace HttpServerApp.Server;

public class HttpServer
{
    private readonly int _port;
    private readonly Router _router;
    private readonly Func<HttpRequest, Task<HttpResponse>>? _fallback;
    private readonly X509Certificate2? _tlsCertificate;

    public HttpServer(
        int port,
        Router router,
        Func<HttpRequest, Task<HttpResponse>>? fallback = null,
        X509Certificate2? tlsCertificate = null)
    {
        _port = port;
        _router = router;
        _fallback = fallback;
        _tlsCertificate = tlsCertificate;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        var scheme = _tlsCertificate is not null ? "https" : "http";
        Console.WriteLine($"[server] Listening on {scheme}://localhost:{_port}");

        cancellationToken.Register(listener.Stop);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
            }
        }
        finally
        {
            listener.Stop();
            Console.WriteLine("[server] Stopped");
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var remoteEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";

        try
        {
            using (client)
            await using (var rawStream = client.GetStream())
            {
                Stream stream = rawStream;
                TlsStream? tlsStream = null;

                if (_tlsCertificate is not null)
                {
                    try
                    {
                        var tlsOptions = new TlsServerOptions
                        {
                            ServerCertificate = _tlsCertificate,
                            Logger = msg => Console.WriteLine($"[server][tls][{remoteEndPoint}] {msg}")
                        };
                        tlsStream = TlsServerFactory.Accept(rawStream, tlsOptions);
                        stream = tlsStream;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[server] TLS handshake failed from {remoteEndPoint}: {ex.Message}");
                        return;
                    }
                }

                try
                {
                    HttpRequest? request;
                    try
                    {
                        request = await RequestReader.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is InvalidDataException or IOException or FormatException)
                    {
                        var bad = HttpResponse.Create(400, "Bad Request");
                        await SendAsync(stream, bad, cancellationToken).ConfigureAwait(false);
                        Console.WriteLine($"[server] {remoteEndPoint} BAD_REQUEST 400 ({ex.Message})");
                        return;
                    }

                    if (request is null)
                        return;

                    HttpResponse response;
                    try
                    {
                        response = await DispatchAsync(request).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[server] Unhandled error: {ex}");
                        response = HttpResponse.Create(500, "Internal Server Error");
                    }

                    Decorate(response);
                    await SendAsync(stream, response, cancellationToken).ConfigureAwait(false);
                    Console.WriteLine($"[server] {remoteEndPoint} {request.Method} {request.Path} -> {response.StatusCode}");
                }
                finally
                {
                    tlsStream?.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[server] Connection error from {remoteEndPoint}: {ex.Message}");
        }
    }

    private async Task<HttpResponse> DispatchAsync(HttpRequest request)
    {
        var match = _router.Resolve(request.Method, request.Path);

        if (match is null)
        {
            if (_fallback is not null)
                return await _fallback(request).ConfigureAwait(false);

            return HttpResponse.Create(404, "Not Found");
        }

        if (match.Kind == MatchKind.MethodNotAllowed)
            return HttpResponse.Create(405, "Method Not Allowed");

        return await match.Handler(request, match.Parameters).ConfigureAwait(false);
    }

    private static void Decorate(HttpResponse response)
    {
        response.Headers["Date"] = DateTime.UtcNow.ToString("R");
        response.Headers["Server"] = "HttpFromScratch/1.0";

        if (!response.Headers.ContainsKey("Connection"))
            response.Headers["Connection"] = "close";

        if (response.StatusCode == 204)
        {
            response.Body = "";
            response.BodyBytes = null;
            response.Headers.Remove("Content-Type");
            response.Headers.Remove("Content-Length");
        }
    }

    private static async Task SendAsync(Stream stream, HttpResponse response, CancellationToken cancellationToken)
    {
        var bytes = response.Serialize();
        await stream.WriteAsync(bytes.AsMemory(0, bytes.Length), cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
