using HttpClientApp;
using HttpLib;

PrintBanner();

while (true)
{
    try
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 60));
        Console.WriteLine("New request (type 'q' as method to exit)");
        Console.WriteLine(new string('=', 60));

        var method = AskMethod();
        if (method == null) break;

        var url = AskUrl();
        if (url == null) continue;

        var headers = AskHeaders();
        var body = AskBody(method);

        var request = RequestBuilder.Build(method, url, headers, body);

        PrintRequestPreview(request, url);

        Console.WriteLine();
        Console.WriteLine($"Sending request to {url.Host}:{url.Port}{url.Path} ...");

        try
        {
            var (response, elapsed) = SocketHttpClient.Send(request, url);
            PrintResponse(response, elapsed);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] Request failed: {ex.Message}");
            Console.ResetColor();
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] {ex.Message}");
        Console.ResetColor();
    }
}

Console.WriteLine("Bye.");

static void PrintBanner()
{
    Console.WriteLine();
    Console.WriteLine("HttpFromScratch - Interactive HTTP/1.1 Client");
    Console.WriteLine("Pure TCP sockets. No HttpClient under the hood.");
}

static string? AskMethod()
{
    while (true)
    {
        Console.Write("Method [GET/HEAD/POST/PUT/DELETE/q]: ");
        var input = (Console.ReadLine() ?? "").Trim();
        if (string.IsNullOrEmpty(input)) input = "GET";

        if (input.Equals("q", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            return null;

        var upper = input.ToUpperInvariant();
        if (upper is "GET" or "HEAD" or "POST" or "PUT" or "DELETE" or "PATCH" or "OPTIONS")
            return upper;

        Console.WriteLine($"  Unsupported method '{input}'. Try GET, HEAD, POST, PUT, DELETE.");
    }
}

static ParsedUrl? AskUrl()
{
    while (true)
    {
        Console.Write("URL (e.g. http://localhost:8080/path): ");
        var raw = (Console.ReadLine() ?? "").Trim();
        if (string.IsNullOrEmpty(raw))
        {
            Console.WriteLine("  URL is required.");
            return null;
        }

        try
        {
            return UrlParser.Parse(raw);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Invalid URL: {ex.Message}");
            return null;
        }
    }
}

static List<(string Name, string Value)> AskHeaders()
{
    var headers = new List<(string, string)>();
    Console.WriteLine("Custom headers (empty line to finish). Format: Name: Value");
    while (true)
    {
        Console.Write("  header> ");
        var line = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(line)) break;

        var idx = line.IndexOf(':');
        if (idx <= 0)
        {
            Console.WriteLine("    Bad format. Use 'Name: Value' or empty line to finish.");
            continue;
        }

        var name = line[..idx].Trim();
        var value = line[(idx + 1)..].Trim();
        headers.Add((name, value));
    }
    return headers;
}

static string AskBody(string method)
{
    var bodyMethods = new[] { "POST", "PUT", "PATCH", "DELETE" };
    if (!bodyMethods.Contains(method)) return "";

    Console.WriteLine("Body (end input with a single line containing only '.', empty body = press '.' immediately):");
    var lines = new List<string>();
    while (true)
    {
        Console.Write("  body> ");
        var line = Console.ReadLine();
        if (line == null) break;
        if (line == ".") break;
        lines.Add(line);
    }
    return string.Join("\n", lines);
}

static void PrintRequestPreview(HttpRequest request, ParsedUrl url)
{
    Console.WriteLine();
    Console.WriteLine("--- Request preview ---");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"{request.Method} {request.Path} {request.Version}");
    Console.ResetColor();
    foreach (var h in request.Headers)
        Console.WriteLine($"{h.Key}: {h.Value}");
    if (!string.IsNullOrEmpty(request.Body))
    {
        Console.WriteLine();
        Console.WriteLine(request.Body);
    }
}

static void PrintResponse(HttpResponse response, TimeSpan elapsed)
{
    Console.WriteLine();
    Console.WriteLine("--- Response ---");

    Console.ForegroundColor = StatusColor(response.StatusCode);
    Console.WriteLine($"{response.Version} {response.StatusCode} {response.ReasonPhrase}");
    Console.ResetColor();

    Console.WriteLine();
    Console.WriteLine("Headers:");
    foreach (var h in response.Headers)
        Console.WriteLine($"  {h.Key}: {h.Value}");

    Console.WriteLine();
    Console.WriteLine("Body:");
    if (string.IsNullOrEmpty(response.Body))
    {
        Console.WriteLine("  (empty)");
    }
    else
    {
        var contentType = response.Headers.TryGetValue("Content-Type", out var ct) ? ct : "";
        var shouldFormat = JsonFormatter.LooksLikeJson(contentType) || LooksLikeJsonContent(response.Body);

        if (shouldFormat && JsonFormatter.TryPrettyPrint(response.Body, out var pretty))
            Console.WriteLine(pretty);
        else
            Console.WriteLine(response.Body);
    }

    Console.WriteLine();
    Console.WriteLine($"Elapsed: {elapsed.TotalMilliseconds:F1} ms");
}

static bool LooksLikeJsonContent(string body)
{
    var trimmed = body.TrimStart();
    return trimmed.StartsWith('{') || trimmed.StartsWith('[');
}

static ConsoleColor StatusColor(int code) => code switch
{
    >= 200 and < 300 => ConsoleColor.Green,
    >= 300 and < 400 => ConsoleColor.Yellow,
    >= 400 and < 500 => ConsoleColor.Magenta,
    >= 500 => ConsoleColor.Red,
    _ => ConsoleColor.Gray
};
