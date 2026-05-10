namespace HttpServerApp.Logging;

public class FileLogger : IDisposable
{
    private readonly object _lock = new();
    private readonly StreamWriter _writer;
    private bool _disposed;

    public FileLogger(string path)
    {
        var fullPath = System.IO.Path.GetFullPath(path);
        var directory = System.IO.Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var stream = new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.Read);
        _writer = new StreamWriter(stream)
        {
            AutoFlush = true
        };

        FilePath = fullPath;
    }

    public string FilePath { get; }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        if (_disposed) return;

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var line = $"[{timestamp}] {level} {message}";

        lock (_lock)
        {
            if (_disposed) return;
            _writer.WriteLine(line);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            _writer.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
