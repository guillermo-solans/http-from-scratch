using HttpLib;

namespace HttpServerApp.Middleware;

public class ApiKeyMiddleware
{
    private const string HeaderName = "X-Api-Key";

    private static readonly HashSet<string> PublicPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/",
        "/index.html"
    };

    private readonly string _apiKey;

    public ApiKeyMiddleware(string apiKey)
    {
        _apiKey = apiKey;
    }

    public async Task<HttpResponse> InvokeAsync(HttpRequest request, RequestDelegate next)
    {
        if (IsPublic(request))
            return await next(request).ConfigureAwait(false);

        if (!request.Headers.TryGetValue(HeaderName, out var provided) || !string.Equals(provided, _apiKey, StringComparison.Ordinal))
        {
            var unauthorized = HttpResponse.Json(401, "{\"error\":\"Unauthorized\"}");
            unauthorized.Headers["WWW-Authenticate"] = "ApiKey";
            return unauthorized;
        }

        return await next(request).ConfigureAwait(false);
    }

    private static bool IsPublic(HttpRequest request)
    {
        if (!string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase))
            return false;

        var path = StripQuery(request.Path);
        return PublicPaths.Contains(path);
    }

    private static string StripQuery(string path)
    {
        var idx = path.IndexOf('?');
        return idx >= 0 ? path[..idx] : path;
    }
}
