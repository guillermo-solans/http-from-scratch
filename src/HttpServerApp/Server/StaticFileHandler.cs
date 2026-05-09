using HttpLib;

namespace HttpServerApp.Server;

public class StaticFileHandler
{
    private readonly string _root;

    public StaticFileHandler(string rootDirectory)
    {
        _root = Path.GetFullPath(rootDirectory);
    }

    public Task<HttpResponse> Serve(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath) || relativePath == "/")
            relativePath = "index.html";

        var sanitized = relativePath.TrimStart('/').Replace('\\', '/');
        var fullPath = Path.GetFullPath(Path.Combine(_root, sanitized));

        if (!fullPath.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(HttpResponse.Create(403, "Forbidden"));

        if (!File.Exists(fullPath))
            return Task.FromResult(HttpResponse.Create(404, "Not Found"));

        var bytes = File.ReadAllBytes(fullPath);
        var contentType = MimeTypes.Resolve(Path.GetExtension(fullPath));

        var response = new HttpResponse
        {
            StatusCode = 200,
            ReasonPhrase = "OK",
            BodyBytes = bytes
        };
        response.Headers["Content-Type"] = contentType;
        response.Headers["Connection"] = "close";

        return Task.FromResult(response);
    }
}
