namespace HttpLib;

public class HttpResponse
{
    public string Version { get; set; } = "HTTP/1.1";
    public int StatusCode { get; set; } = 200;
    public string ReasonPhrase { get; set; } = "OK";
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string Body { get; set; } = "";
    public byte[]? BodyBytes { get; set; }

    public byte[] Serialize()
    {
        var bodyBytes = BodyBytes ?? (string.IsNullOrEmpty(Body)
            ? Array.Empty<byte>()
            : System.Text.Encoding.UTF8.GetBytes(Body));

        if (bodyBytes.Length > 0 && !Headers.ContainsKey("Content-Length"))
            Headers["Content-Length"] = bodyBytes.Length.ToString();

        var builder = new System.Text.StringBuilder();
        builder.Append($"{Version} {StatusCode} {ReasonPhrase}\r\n");

        foreach (var header in Headers)
            builder.Append($"{header.Key}: {header.Value}\r\n");

        builder.Append("\r\n");

        var headerBytes = System.Text.Encoding.UTF8.GetBytes(builder.ToString());
        var output = new byte[headerBytes.Length + bodyBytes.Length];
        Buffer.BlockCopy(headerBytes, 0, output, 0, headerBytes.Length);
        if (bodyBytes.Length > 0)
            Buffer.BlockCopy(bodyBytes, 0, output, headerBytes.Length, bodyBytes.Length);

        return output;
    }

    public static HttpResponse Parse(string raw)
    {
        var response = new HttpResponse();
        var lines = raw.Split("\r\n");

        if (lines.Length == 0)
            throw new FormatException("Empty response");

        var statusLine = lines[0].Split(' ', 3);
        if (statusLine.Length < 2)
            throw new FormatException("Malformed status line");

        response.Version = statusLine[0];
        response.StatusCode = int.Parse(statusLine[1]);
        response.ReasonPhrase = statusLine.Length > 2 ? statusLine[2] : "";

        int i = 1;
        for (; i < lines.Length; i++)
        {
            if (string.IsNullOrEmpty(lines[i]))
            {
                i++;
                break;
            }

            var colonIndex = lines[i].IndexOf(':');
            if (colonIndex <= 0) continue;

            var name = lines[i][..colonIndex].Trim();
            var value = lines[i][(colonIndex + 1)..].Trim();
            response.Headers[name] = value;
        }

        if (i < lines.Length)
            response.Body = string.Join("\r\n", lines[i..]);

        return response;
    }

    public static readonly Dictionary<int, string> ReasonPhrases = new()
    {
        [200] = "OK",
        [201] = "Created",
        [204] = "No Content",
        [304] = "Not Modified",
        [400] = "Bad Request",
        [401] = "Unauthorized",
        [403] = "Forbidden",
        [404] = "Not Found",
        [405] = "Method Not Allowed",
        [500] = "Internal Server Error"
    };

    public static HttpResponse Create(int statusCode, string body = "", string contentType = "text/plain")
    {
        var reason = ReasonPhrases.GetValueOrDefault(statusCode, "Unknown");
        var response = new HttpResponse
        {
            StatusCode = statusCode,
            ReasonPhrase = reason,
            Body = body
        };

        if (!string.IsNullOrEmpty(body))
            response.Headers["Content-Type"] = contentType;

        response.Headers["Connection"] = "close";
        return response;
    }

    public static HttpResponse Json(int statusCode, string jsonBody)
    {
        return Create(statusCode, jsonBody, "application/json");
    }
}
