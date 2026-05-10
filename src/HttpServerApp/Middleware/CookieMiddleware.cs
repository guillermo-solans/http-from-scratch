using HttpLib;

namespace HttpServerApp.Middleware;

public class CookieMiddleware
{
    public async Task<HttpResponse> InvokeAsync(HttpRequest request, RequestDelegate next)
    {
        ParseCookies(request);
        return await next(request).ConfigureAwait(false);
    }

    private static void ParseCookies(HttpRequest request)
    {
        request.Cookies.Clear();

        if (!request.Headers.TryGetValue("Cookie", out var raw) || string.IsNullOrWhiteSpace(raw))
            return;

        var pairs = raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var pair in pairs)
        {
            var eq = pair.IndexOf('=');
            if (eq <= 0) continue;

            var name = pair[..eq].Trim();
            var value = pair[(eq + 1)..].Trim();
            if (name.Length == 0) continue;

            request.Cookies[name] = Uri.UnescapeDataString(value);
        }
    }
}
