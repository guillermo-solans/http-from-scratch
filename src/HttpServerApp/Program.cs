using System.Security.Cryptography.X509Certificates;
using HttpLib;
using HttpServerApp.Handlers;
using HttpServerApp.Repositories;
using HttpServerApp.Server;
using TlsLib.Certificates;

var useTls = args.Any(a => a.Equals("--tls", StringComparison.OrdinalIgnoreCase));
var port = ResolvePort(args, useTls);
var staticRoot = ResolveStaticRoot();

var repository = new CatRepository();
var catsHandler = new CatsHandler(repository);
var staticFiles = new StaticFileHandler(staticRoot);

var router = new Router()
    .Get("/", (_, _) => staticFiles.Serve("index.html"))
    .Get("/index.html", (_, _) => staticFiles.Serve("index.html"))
    .Get("/cats", catsHandler.List)
    .Post("/cats", catsHandler.Create)
    .Get("/cats/:id", catsHandler.GetById)
    .Put("/cats/:id", catsHandler.Update)
    .Delete("/cats/:id", catsHandler.Delete);

Func<HttpRequest, Task<HttpResponse>> staticFallback = async request =>
{
    if (request.Method != "GET")
        return HttpResponse.Create(405, "Method Not Allowed");

    return await staticFiles.Serve(request.Path);
};

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

X509Certificate2? tlsCert = null;
if (useTls)
{
    Console.WriteLine("[server] TLS enabled, generating self-signed certificate for 'localhost'...");
    tlsCert = SelfSignedCertificateGenerator.Generate("localhost");
    Console.WriteLine($"[server] Certificate subject: {tlsCert.Subject}");
    Console.WriteLine($"[server] Certificate thumbprint: {tlsCert.Thumbprint}");
}

Console.WriteLine($"[server] Static root: {staticRoot}");
var server = new HttpServer(port, router, staticFallback, tlsCert);
await server.RunAsync(cts.Token);

static int ResolvePort(string[] args, bool useTls)
{
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i].Equals("--port", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
            if (int.TryParse(args[i + 1], out var p) && p > 0)
                return p;
        }
    }

    foreach (var arg in args)
    {
        if (arg.StartsWith("--", StringComparison.Ordinal)) continue;
        if (int.TryParse(arg, out var fromArg) && fromArg > 0)
            return fromArg;
    }

    var fromEnv = Environment.GetEnvironmentVariable("HTTP_SERVER_PORT");
    if (!string.IsNullOrEmpty(fromEnv) && int.TryParse(fromEnv, out var parsed) && parsed > 0)
        return parsed;

    return useTls ? 8443 : 8080;
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
