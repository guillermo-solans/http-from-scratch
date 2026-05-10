using System.Text.Json;
using HttpLib;

namespace HttpServerApp.Handlers;

public class CookiesHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public Task<HttpResponse> List(HttpRequest request, IReadOnlyDictionary<string, string> parameters)
    {
        var json = JsonSerializer.Serialize(request.Cookies, JsonOptions);
        return Task.FromResult(HttpResponse.Json(200, json));
    }

    public Task<HttpResponse> Set(HttpRequest request, IReadOnlyDictionary<string, string> parameters)
    {
        if (!IsJsonContentType(request))
            return Task.FromResult(HttpResponse.Create(400, "Content-Type must be application/json"));

        SetCookieRequest? incoming;
        try
        {
            incoming = JsonSerializer.Deserialize<SetCookieRequest>(request.Body, JsonOptions);
        }
        catch (JsonException)
        {
            return Task.FromResult(HttpResponse.Create(400, "Malformed JSON body"));
        }

        if (incoming is null || string.IsNullOrWhiteSpace(incoming.Name))
            return Task.FromResult(HttpResponse.Create(400, "Cookie name is required"));

        var cookieHeader = BuildSetCookieHeader(incoming);

        var response = HttpResponse.Json(200, JsonSerializer.Serialize(new
        {
            ok = true,
            name = incoming.Name,
            value = incoming.Value ?? ""
        }, JsonOptions));
        response.SetCookies.Add(cookieHeader);
        return Task.FromResult(response);
    }

    private static string BuildSetCookieHeader(SetCookieRequest payload)
    {
        var name = Uri.EscapeDataString(payload.Name!);
        var value = Uri.EscapeDataString(payload.Value ?? "");
        var parts = new List<string> { $"{name}={value}" };

        if (payload.MaxAge.HasValue)
            parts.Add($"Max-Age={payload.MaxAge.Value}");

        if (!string.IsNullOrWhiteSpace(payload.Path))
            parts.Add($"Path={payload.Path}");
        else
            parts.Add("Path=/");

        return string.Join("; ", parts);
    }

    private static bool IsJsonContentType(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("Content-Type", out var contentType))
            return false;
        return contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class SetCookieRequest
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
        public int? MaxAge { get; set; }
        public string? Path { get; set; }
    }
}
