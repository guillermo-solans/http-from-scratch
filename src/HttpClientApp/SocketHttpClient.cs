using System.Net.Sockets;
using System.Text;
using HttpLib;

namespace HttpClientApp;

public static class SocketHttpClient
{
    public static (HttpResponse Response, TimeSpan Elapsed) Send(HttpRequest request, ParsedUrl url, int timeoutMs = 30000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        using var tcp = new TcpClient
        {
            ReceiveTimeout = timeoutMs,
            SendTimeout = timeoutMs
        };

        tcp.Connect(url.Host, url.Port);

        using var stream = tcp.GetStream();

        var raw = request.Serialize();
        stream.Write(raw, 0, raw.Length);
        stream.Flush();

        var isHead = string.Equals(request.Method, "HEAD", StringComparison.OrdinalIgnoreCase);
        var response = HttpResponseReader.ReadFromStream(stream, isHead);

        sw.Stop();
        return (response, sw.Elapsed);
    }
}
