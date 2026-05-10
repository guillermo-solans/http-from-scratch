using System.Diagnostics;
using HttpLib;
using HttpServerApp.Logging;

namespace HttpServerApp.Middleware;

public class LoggingMiddleware
{
    private readonly FileLogger _logger;

    public LoggingMiddleware(FileLogger logger)
    {
        _logger = logger;
    }

    public async Task<HttpResponse> InvokeAsync(HttpRequest request, RequestDelegate next)
    {
        var sw = Stopwatch.StartNew();
        HttpResponse response;

        try
        {
            response = await next(request).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.Error($"{request.Method} {request.Path} 500 {sw.ElapsedMilliseconds}ms ({ex.GetType().Name}: {ex.Message})");
            throw;
        }

        sw.Stop();

        var line = $"{request.Method} {request.Path} {response.StatusCode} {sw.ElapsedMilliseconds}ms";

        switch (response.StatusCode)
        {
            case >= 500:
                _logger.Error(line);
                break;
            case >= 400:
                _logger.Warn(line);
                break;
            default:
                _logger.Info(line);
                break;
        }

        return response;
    }
}
