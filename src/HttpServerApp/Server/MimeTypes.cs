namespace HttpServerApp.Server;

public static class MimeTypes
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html; charset=utf-8",
        [".htm"] = "text/html; charset=utf-8",
        [".css"] = "text/css; charset=utf-8",
        [".js"] = "application/javascript; charset=utf-8",
        [".mjs"] = "application/javascript; charset=utf-8",
        [".json"] = "application/json; charset=utf-8",
        [".txt"] = "text/plain; charset=utf-8",
        [".xml"] = "application/xml; charset=utf-8",
        [".svg"] = "image/svg+xml",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".ico"] = "image/x-icon",
        [".pdf"] = "application/pdf",
        [".woff"] = "font/woff",
        [".woff2"] = "font/woff2",
        [".ttf"] = "font/ttf"
    };

    public static string Resolve(string extension)
    {
        return Map.TryGetValue(extension, out var mime) ? mime : "application/octet-stream";
    }

    public static bool IsTextual(string mime)
    {
        return mime.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || mime.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)
            || mime.StartsWith("application/javascript", StringComparison.OrdinalIgnoreCase)
            || mime.StartsWith("application/xml", StringComparison.OrdinalIgnoreCase)
            || mime.StartsWith("image/svg+xml", StringComparison.OrdinalIgnoreCase);
    }
}
