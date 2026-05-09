using System.Text;
using HttpLib;

namespace HttpServerApp.Server;

public static class RequestReader
{
    private const int MaxHeaderSize = 8192;
    private static readonly byte[] HeaderTerminator = { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };

    public static async Task<HttpRequest?> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        var headerBuffer = new byte[MaxHeaderSize];
        var read = 0;
        var headerEnd = -1;
        var temp = new byte[1024];

        while (read < MaxHeaderSize)
        {
            var available = MaxHeaderSize - read;
            var chunk = available < temp.Length ? available : temp.Length;
            var n = await stream.ReadAsync(temp.AsMemory(0, chunk), cancellationToken).ConfigureAwait(false);

            if (n == 0)
            {
                if (read == 0) return null;
                throw new IOException("Unexpected end of stream while reading headers");
            }

            Buffer.BlockCopy(temp, 0, headerBuffer, read, n);
            read += n;

            headerEnd = IndexOf(headerBuffer, read, HeaderTerminator);
            if (headerEnd >= 0) break;
        }

        if (headerEnd < 0)
            throw new InvalidDataException("Header section exceeded maximum size or missing terminator");

        var headerText = Encoding.ASCII.GetString(headerBuffer, 0, headerEnd);
        var request = ParseHead(headerText);

        var bodyStart = headerEnd + HeaderTerminator.Length;
        var alreadyBuffered = read - bodyStart;

        var contentLength = 0;
        if (request.Headers.TryGetValue("Content-Length", out var rawLength))
        {
            if (!int.TryParse(rawLength.Trim(), out contentLength) || contentLength < 0)
                throw new InvalidDataException("Invalid Content-Length");
        }

        if (contentLength > 0)
        {
            var body = new byte[contentLength];
            var copyFromBuffer = Math.Min(alreadyBuffered, contentLength);
            if (copyFromBuffer > 0)
                Buffer.BlockCopy(headerBuffer, bodyStart, body, 0, copyFromBuffer);

            var bodyRead = copyFromBuffer;
            while (bodyRead < contentLength)
            {
                var n = await stream.ReadAsync(body.AsMemory(bodyRead, contentLength - bodyRead), cancellationToken).ConfigureAwait(false);
                if (n == 0)
                    throw new IOException("Unexpected end of stream while reading body");
                bodyRead += n;
            }

            request.Body = Encoding.UTF8.GetString(body);
        }

        return request;
    }

    private static HttpRequest ParseHead(string headerText)
    {
        var request = new HttpRequest();
        var lines = headerText.Split("\r\n");
        if (lines.Length == 0)
            throw new InvalidDataException("Empty request");

        var parts = lines[0].Split(' ', 3);
        if (parts.Length < 3)
            throw new InvalidDataException("Malformed request line");

        request.Method = parts[0].ToUpperInvariant();
        request.Path = parts[1];
        request.Version = parts[2];

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrEmpty(line)) continue;

            var colon = line.IndexOf(':');
            if (colon <= 0) continue;

            var name = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();
            request.Headers[name] = value;
        }

        return request;
    }

    private static int IndexOf(byte[] buffer, int length, byte[] needle)
    {
        for (var i = 0; i <= length - needle.Length; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (buffer[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }
            if (match) return i;
        }
        return -1;
    }
}
