using System.Text.Json;

namespace HttpClientApp;

public static class JsonFormatter
{
    public static bool TryPrettyPrint(string text, out string pretty)
    {
        pretty = text;
        if (string.IsNullOrWhiteSpace(text)) return false;

        try
        {
            using var doc = JsonDocument.Parse(text);
            pretty = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool LooksLikeJson(string contentType)
    {
        return !string.IsNullOrEmpty(contentType) &&
               contentType.Contains("json", StringComparison.OrdinalIgnoreCase);
    }
}
