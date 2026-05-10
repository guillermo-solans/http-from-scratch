using System.Globalization;
using HttpLib;

namespace HttpClientApp;

public class CookieJar
{
    private readonly object _lock = new();
    private readonly List<StoredCookie> _cookies = new();

    public void IngestFromResponse(HttpResponse response, string requestPath)
    {
        if (response.SetCookies.Count == 0)
            return;

        var defaultPath = ComputeDefaultPath(requestPath);

        lock (_lock)
        {
            foreach (var raw in response.SetCookies)
            {
                if (TryParse(raw, defaultPath, out var cookie))
                    Upsert(cookie);
            }
        }
    }

    public string? BuildCookieHeader(string requestPath)
    {
        var path = NormalizePath(requestPath);
        var now = DateTimeOffset.UtcNow;
        var matching = new List<StoredCookie>();

        lock (_lock)
        {
            _cookies.RemoveAll(c => c.IsExpired(now));

            foreach (var c in _cookies)
            {
                if (PathMatches(path, c.Path))
                    matching.Add(c);
            }
        }

        if (matching.Count == 0)
            return null;

        return string.Join("; ", matching.Select(c => $"{c.Name}={c.Value}"));
    }

    public IReadOnlyList<string> Snapshot()
    {
        lock (_lock)
        {
            return _cookies
                .Select(c => $"{c.Name}={c.Value}; Path={c.Path}" + (c.Expires.HasValue ? $"; Expires={c.Expires:O}" : ""))
                .ToList();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _cookies.Clear();
        }
    }

    private void Upsert(StoredCookie cookie)
    {
        _cookies.RemoveAll(c => c.Name == cookie.Name && c.Path == cookie.Path);

        if (cookie.IsExpired(DateTimeOffset.UtcNow))
            return;

        _cookies.Add(cookie);
    }

    private static bool TryParse(string raw, string defaultPath, out StoredCookie cookie)
    {
        cookie = default!;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var parts = raw.Split(';');
        var nameValue = parts[0].Trim();
        var eq = nameValue.IndexOf('=');
        if (eq <= 0) return false;

        var name = nameValue[..eq].Trim();
        var value = nameValue[(eq + 1)..].Trim();
        if (name.Length == 0) return false;

        string path = defaultPath;
        DateTimeOffset? expires = null;

        for (int i = 1; i < parts.Length; i++)
        {
            var attr = parts[i].Trim();
            if (attr.Length == 0) continue;

            var attrEq = attr.IndexOf('=');
            var attrName = attrEq > 0 ? attr[..attrEq].Trim() : attr;
            var attrValue = attrEq > 0 ? attr[(attrEq + 1)..].Trim() : "";

            if (string.Equals(attrName, "Path", StringComparison.OrdinalIgnoreCase) && attrValue.Length > 0)
            {
                path = attrValue;
            }
            else if (string.Equals(attrName, "Max-Age", StringComparison.OrdinalIgnoreCase))
            {
                if (long.TryParse(attrValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
                    expires = DateTimeOffset.UtcNow.AddSeconds(seconds);
            }
            else if (string.Equals(attrName, "Expires", StringComparison.OrdinalIgnoreCase) && !expires.HasValue)
            {
                if (DateTimeOffset.TryParseExact(attrValue, "R", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ||
                    DateTimeOffset.TryParse(attrValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out parsed))
                {
                    expires = parsed.ToUniversalTime();
                }
            }
        }

        cookie = new StoredCookie(name, value, path, expires);
        return true;
    }

    private static string ComputeDefaultPath(string requestPath)
    {
        var p = NormalizePath(requestPath);
        var lastSlash = p.LastIndexOf('/');
        if (lastSlash <= 0) return "/";
        return p[..lastSlash];
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return "/";
        var q = path.IndexOf('?');
        var stripped = q >= 0 ? path[..q] : path;
        return string.IsNullOrEmpty(stripped) ? "/" : stripped;
    }

    private static bool PathMatches(string requestPath, string cookiePath)
    {
        if (cookiePath == "/") return true;
        if (requestPath == cookiePath) return true;
        if (requestPath.StartsWith(cookiePath, StringComparison.Ordinal))
        {
            if (cookiePath.EndsWith('/')) return true;
            if (requestPath.Length > cookiePath.Length && requestPath[cookiePath.Length] == '/')
                return true;
        }
        return false;
    }

    private sealed record StoredCookie(string Name, string Value, string Path, DateTimeOffset? Expires)
    {
        public bool IsExpired(DateTimeOffset now) => Expires.HasValue && Expires.Value <= now;
    }
}
