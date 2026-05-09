using System.Text;
using HttpLib;

namespace HttpClientApp;

public static class RequestBuilder
{
    private static readonly HashSet<string> MethodsAllowingBody = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "PUT", "PATCH", "DELETE"
    };

    public static HttpRequest Build(string method, ParsedUrl url, IEnumerable<(string Name, string Value)> customHeaders, string body)
    {
        var request = new HttpRequest
        {
            Method = method.ToUpperInvariant(),
            Path = string.IsNullOrEmpty(url.Path) ? "/" : url.Path,
            Version = "HTTP/1.1",
            Body = MethodsAllowingBody.Contains(method) ? (body ?? "") : ""
        };

        foreach (var (name, value) in customHeaders)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            request.Headers[name.Trim()] = value?.Trim() ?? "";
        }

        EnsureHeader(request, "Host", BuildHostHeader(url));
        EnsureHeader(request, "User-Agent", "HttpFromScratch/1.0");
        EnsureHeader(request, "Accept", "*/*");
        EnsureHeader(request, "Connection", "close");

        if (!string.IsNullOrEmpty(request.Body))
        {
            var bodyByteCount = Encoding.UTF8.GetByteCount(request.Body);
            request.Headers["Content-Length"] = bodyByteCount.ToString();
            EnsureHeader(request, "Content-Type", "application/json");
        }

        return request;
    }

    private static void EnsureHeader(HttpRequest request, string name, string value)
    {
        if (!request.Headers.ContainsKey(name))
            request.Headers[name] = value;
    }

    private static string BuildHostHeader(ParsedUrl url)
    {
        return url.Port == 80 ? url.Host : $"{url.Host}:{url.Port}";
    }
}
