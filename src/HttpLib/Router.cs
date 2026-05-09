using System.Text.RegularExpressions;

namespace HttpLib;

public delegate Task<HttpResponse> RouteHandler(HttpRequest request, IReadOnlyDictionary<string, string> pathParameters);

public class Router
{
    private readonly List<Route> _routes = new();

    public Router Map(string method, string pathPattern, RouteHandler handler)
    {
        _routes.Add(new Route(method.ToUpperInvariant(), pathPattern, handler));
        return this;
    }

    public Router Get(string pathPattern, RouteHandler handler) => Map("GET", pathPattern, handler);
    public Router Post(string pathPattern, RouteHandler handler) => Map("POST", pathPattern, handler);
    public Router Put(string pathPattern, RouteHandler handler) => Map("PUT", pathPattern, handler);
    public Router Delete(string pathPattern, RouteHandler handler) => Map("DELETE", pathPattern, handler);

    public RouteMatch? Resolve(string method, string path)
    {
        var requestPath = StripQueryString(path);
        var upperMethod = method.ToUpperInvariant();

        Route? pathOnlyMatch = null;
        Dictionary<string, string>? pathOnlyParameters = null;

        foreach (var route in _routes)
        {
            if (!route.TryMatch(requestPath, out var parameters))
                continue;

            if (route.Method == upperMethod)
                return new RouteMatch(route.Handler, parameters!, MatchKind.Exact);

            pathOnlyMatch = route;
            pathOnlyParameters = parameters;
        }

        if (pathOnlyMatch is not null)
            return new RouteMatch(pathOnlyMatch.Handler, pathOnlyParameters!, MatchKind.MethodNotAllowed);

        return null;
    }

    private static string StripQueryString(string path)
    {
        var queryIndex = path.IndexOf('?');
        return queryIndex >= 0 ? path[..queryIndex] : path;
    }

    private sealed class Route
    {
        private static readonly Regex ParameterRegex = new(@":([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);

        public string Method { get; }
        public RouteHandler Handler { get; }

        private readonly Regex _pattern;
        private readonly List<string> _parameterNames = new();

        public Route(string method, string pathPattern, RouteHandler handler)
        {
            Method = method;
            Handler = handler;

            var regexPattern = "^" + ParameterRegex.Replace(Regex.Escape(pathPattern).Replace("\\:", ":"), match =>
            {
                _parameterNames.Add(match.Groups[1].Value);
                return "([^/]+)";
            }) + "$";

            _pattern = new Regex(regexPattern, RegexOptions.Compiled);
        }

        public bool TryMatch(string path, out Dictionary<string, string>? parameters)
        {
            var match = _pattern.Match(path);
            if (!match.Success)
            {
                parameters = null;
                return false;
            }

            parameters = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < _parameterNames.Count; i++)
                parameters[_parameterNames[i]] = Uri.UnescapeDataString(match.Groups[i + 1].Value);

            return true;
        }
    }
}

public enum MatchKind
{
    Exact,
    MethodNotAllowed
}

public record RouteMatch(RouteHandler Handler, IReadOnlyDictionary<string, string> Parameters, MatchKind Kind);
