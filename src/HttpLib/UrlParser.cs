namespace HttpLib;

public record ParsedUrl(string Scheme, string Host, int Port, string Path);

public static class UrlParser
{
    public static ParsedUrl Parse(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new FormatException("URL is empty");

        var working = url.Trim();
        var scheme = "http";

        var schemeIdx = working.IndexOf("://", StringComparison.Ordinal);
        if (schemeIdx >= 0)
        {
            scheme = working[..schemeIdx].ToLowerInvariant();
            working = working[(schemeIdx + 3)..];
        }

        if (scheme != "http" && scheme != "https")
            throw new NotSupportedException($"Scheme '{scheme}' is not supported. Only http:// and https:// are allowed.");

        string path = "/";
        var pathIdx = working.IndexOf('/');
        if (pathIdx >= 0)
        {
            path = working[pathIdx..];
            working = working[..pathIdx];
        }

        var queryOnlyIdx = path.IndexOf('?');
        if (path.Length == 0)
            path = "/";

        string host;
        int port = scheme == "https" ? 443 : 80;

        var portIdx = working.LastIndexOf(':');
        if (portIdx >= 0)
        {
            host = working[..portIdx];
            var portStr = working[(portIdx + 1)..];
            if (!int.TryParse(portStr, out port))
                throw new FormatException($"Invalid port: '{portStr}'");
        }
        else
        {
            host = working;
        }

        if (string.IsNullOrEmpty(host))
            throw new FormatException("Host is empty");

        return new ParsedUrl(scheme, host, port, path);
    }
}
