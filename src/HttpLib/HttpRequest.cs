namespace HttpLib;

public class HttpRequest
{
    public string Method { get; set; } = "GET";
    public string Path { get; set; } = "/";
    public string Version { get; set; } = "HTTP/1.1";
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string Body { get; set; } = "";

    public byte[] Serialize()
    {
        var builder = new System.Text.StringBuilder();
        builder.Append($"{Method} {Path} {Version}\r\n");

        foreach (var header in Headers)
            builder.Append($"{header.Key}: {header.Value}\r\n");

        builder.Append("\r\n");
        builder.Append(Body);

        return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
    }

    public static HttpRequest Parse(string raw)
    {
        var request = new HttpRequest();
        var lines = raw.Split("\r\n");

        if (lines.Length == 0)
            throw new FormatException("Empty request");

        var requestLine = lines[0].Split(' ', 3);
        if (requestLine.Length < 3)
            throw new FormatException("Malformed request line");

        request.Method = requestLine[0].ToUpperInvariant();
        request.Path = requestLine[1];
        request.Version = requestLine[2];

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
            request.Headers[name] = value;
        }

        if (i < lines.Length)
            request.Body = string.Join("\r\n", lines[i..]);

        return request;
    }
}
