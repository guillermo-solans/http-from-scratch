namespace HttpLib;

public delegate Task<HttpResponse> RequestDelegate(HttpRequest request);

public class MiddlewareChain
{
    private readonly List<Func<HttpRequest, RequestDelegate, Task<HttpResponse>>> _middlewares = new();

    public MiddlewareChain Use(Func<HttpRequest, RequestDelegate, Task<HttpResponse>> middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        _middlewares.Add(middleware);
        return this;
    }

    public RequestDelegate Build(RequestDelegate finalHandler)
    {
        ArgumentNullException.ThrowIfNull(finalHandler);

        RequestDelegate next = finalHandler;
        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var current = _middlewares[i];
            var captured = next;
            next = request => current(request, captured);
        }

        return next;
    }
}
