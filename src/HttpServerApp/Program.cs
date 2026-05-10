using HttpLib;
using HttpServerApp.Handlers;
using HttpServerApp.Logging;
using HttpServerApp.Middleware;
using HttpServerApp.Repositories;
using HttpServerApp.Server;

var port = ResolvePort(args);
var staticRoot = ResolveStaticRoot();
var apiKey = ResolveApiKey(args);
var logFilePath = ResolveLogFile(args);

var repository = new CatRepository();
var catsHandler = new CatsHandler(repository);
var cookiesHandler = new CookiesHandler();
var staticFiles = new StaticFileHandler(staticRoot);

var router = new Router()
    .Get("/", (_, _) => staticFiles.Serve("index.html"))
    .Get("/index.html", (_, _) => staticFiles.Serve("index.html"))
    .Get("/cats", catsHandler.List)
    .Post("/cats", catsHandler.Create)
    .Get("/cats/:id", catsHandler.GetById)
    .Put("/cats/:id", catsHandler.Update)
    .Delete("/cats/:id", catsHandler.Delete)
    .Get("/cookies", cookiesHandler.List)
    .Post("/cookies", cookiesHandler.Set);

Func<HttpRequest, Task<HttpResponse>> staticFallback = async request =>
{
    if (request.Method != "GET")
        return HttpResponse.Create(405, "Method Not Allowed");

    return await staticFiles.Serve(request.Path);
};

using var fileLogger = new FileLogger(logFilePath);
var loggingMiddleware = new LoggingMiddleware(fileLogger);
var cookieMiddleware = new CookieMiddleware();

var middlewares = new MiddlewareChain()
    .Use(loggingMiddleware.InvokeAsync);

if (!string.IsNullOrEmpty(apiKey))
{
    var apiKeyMiddleware = new ApiKeyMiddleware(apiKey);
    middlewares.Use(apiKeyMiddleware.InvokeAsync);
    Console.WriteLine("[server] API key authentication enabled");
}
else
{
    Console.WriteLine("[server] API key authentication disabled (no API_KEY configured)");
}

middlewares.Use(cookieMiddleware.InvokeAsync);

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine($"[server] Static root: {staticRoot}");
Console.WriteLine($"[server] Log file: {fileLogger.FilePath}");

var server = new HttpServer(port, router, staticFallback, middlewares);
await server.RunAsync(cts.Token);

static int ResolvePort(string[] args)
{
    if (args.Length > 0 && int.TryParse(args[0], out var fromArg) && fromArg > 0)
        return fromArg;

    var fromEnv = Environment.GetEnvironmentVariable("HTTP_SERVER_PORT");
    if (!string.IsNullOrEmpty(fromEnv) && int.TryParse(fromEnv, out var parsed) && parsed > 0)
        return parsed;

    return 8080;
}

static string? ResolveApiKey(string[] args)
{
    var fromArg = TryGetOption(args, "--api-key");
    if (!string.IsNullOrEmpty(fromArg))
        return fromArg;

    var fromEnv = Environment.GetEnvironmentVariable("API_KEY");
    return string.IsNullOrEmpty(fromEnv) ? null : fromEnv;
}

static string ResolveLogFile(string[] args)
{
    var fromArg = TryGetOption(args, "--log-file");
    if (!string.IsNullOrEmpty(fromArg))
        return fromArg;

    var fromEnv = Environment.GetEnvironmentVariable("LOG_FILE");
    if (!string.IsNullOrEmpty(fromEnv))
        return fromEnv;

    return Path.Combine(Environment.CurrentDirectory, "server.log");
}

static string? TryGetOption(string[] args, string name)
{
    for (int i = 0; i < args.Length; i++)
    {
        if (!string.Equals(args[i], name, StringComparison.Ordinal))
            continue;

        if (i + 1 < args.Length)
            return args[i + 1];
    }

    var prefix = name + "=";
    foreach (var a in args)
    {
        if (a.StartsWith(prefix, StringComparison.Ordinal))
            return a[prefix.Length..];
    }
    return null;
}

static string ResolveStaticRoot()
{
    var candidates = new[]
    {
        Path.Combine(AppContext.BaseDirectory, "static"),
        Path.Combine(Environment.CurrentDirectory, "static"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "static"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "static")
    };

    foreach (var candidate in candidates)
    {
        var resolved = Path.GetFullPath(candidate);
        if (Directory.Exists(resolved))
            return resolved;
    }

    return Path.GetFullPath(candidates[0]);
}
