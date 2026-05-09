using System.Net.Sockets;
using HttpLib;
using TlsLib;

namespace HttpClientApp;

public static class SocketHttpClient
{
    public static (HttpResponse Response, TimeSpan Elapsed) Send(
        HttpRequest request,
        ParsedUrl url,
        int timeoutMs = 30000,
        Action<string>? tlsLogger = null)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        using var tcp = new TcpClient
        {
            ReceiveTimeout = timeoutMs,
            SendTimeout = timeoutMs
        };

        tcp.Connect(url.Host, url.Port);

        var networkStream = tcp.GetStream();
        Stream stream = networkStream;
        TlsStream? tlsStream = null;

        try
        {
            if (string.Equals(url.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                var options = new TlsClientOptions
                {
                    Logger = tlsLogger,
                    AllowInvalidCertificates = true
                };
                tlsStream = TlsClientFactory.Connect(networkStream, url.Host, options);
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
            networkStream.Dispose();
        }
    }
}
