using System.IO;
using System.Text;

namespace HttpLib;

public static class HttpResponseReader
{
    public static HttpResponse ReadFromStream(Stream stream, bool isHeadRequest = false)
    {
        var headerBytes = ReadUntilDoubleCrLf(stream);
        var headerText = Encoding.ASCII.GetString(headerBytes);

        var response = ParseHeaderSection(headerText);

        if (isHeadRequest)
            return response;

        if (response.Headers.TryGetValue("Content-Length", out var lengthStr) &&
            int.TryParse(lengthStr, out var contentLength) && contentLength > 0)
        {
            var bodyBytes = ReadExactly(stream, contentLength);
            response.Body = Encoding.UTF8.GetString(bodyBytes);
        }
        else if (response.Headers.TryGetValue("Transfer-Encoding", out var te) &&
                 te.Contains("chunked", StringComparison.OrdinalIgnoreCase))
        {
            response.Body = ReadChunkedBody(stream);
        }
        else if (!response.Headers.ContainsKey("Content-Length"))
        {
            response.Body = ReadUntilEnd(stream);
        }

        return response;
    }

    private static byte[] ReadUntilDoubleCrLf(Stream stream)
    {
        using var ms = new MemoryStream();
        int matched = 0;
        var pattern = new byte[] { 0x0D, 0x0A, 0x0D, 0x0A };

        while (matched < 4)
        {
            int b = stream.ReadByte();
            if (b == -1)
            {
                if (ms.Length == 0)
                    throw new IOException("Connection closed before any response was received");
                break;
            }

            ms.WriteByte((byte)b);

            if ((byte)b == pattern[matched])
                matched++;
            else if ((byte)b == pattern[0])
                matched = 1;
            else
                matched = 0;
        }

        return ms.ToArray();
    }

    private static HttpResponse ParseHeaderSection(string raw)
    {
        var response = new HttpResponse();
        var trimmed = raw.TrimEnd('\r', '\n');
        var lines = trimmed.Split("\r\n");

        if (lines.Length == 0 || string.IsNullOrEmpty(lines[0]))
            throw new FormatException("Empty response head");

        var statusLine = lines[0].Split(' ', 3);
        if (statusLine.Length < 2)
            throw new FormatException($"Malformed status line: '{lines[0]}'");

        response.Version = statusLine[0];
        if (!int.TryParse(statusLine[1], out var code))
            throw new FormatException($"Invalid status code: '{statusLine[1]}'");
        response.StatusCode = code;
        response.ReasonPhrase = statusLine.Length > 2 ? statusLine[2] : "";

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrEmpty(lines[i])) continue;

            var colonIdx = lines[i].IndexOf(':');
            if (colonIdx <= 0) continue;

            var name = lines[i][..colonIdx].Trim();
            var value = lines[i][(colonIdx + 1)..].Trim();
            response.Headers[name] = value;
        }

        return response;
    }

    private static byte[] ReadExactly(Stream stream, int count)
    {
        var buffer = new byte[count];
        int total = 0;
        while (total < count)
        {
            int read = stream.Read(buffer, total, count - total);
            if (read == 0)
                throw new IOException($"Connection closed: expected {count} bytes, got {total}");
            total += read;
        }
        return buffer;
    }

    private static string ReadUntilEnd(Stream stream)
    {
        using var ms = new MemoryStream();
        var buffer = new byte[4096];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            ms.Write(buffer, 0, read);
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static string ReadChunkedBody(Stream stream)
    {
        using var ms = new MemoryStream();
        while (true)
        {
            var sizeLine = ReadLine(stream);
            if (string.IsNullOrEmpty(sizeLine)) continue;

            var semi = sizeLine.IndexOf(';');
            var sizeHex = semi >= 0 ? sizeLine[..semi] : sizeLine;

            if (!int.TryParse(sizeHex.Trim(), System.Globalization.NumberStyles.HexNumber, null, out var size))
                throw new FormatException($"Invalid chunk size: '{sizeLine}'");

            if (size == 0)
            {
                ReadLine(stream);
                break;
            }

            var chunk = ReadExactly(stream, size);
            ms.Write(chunk, 0, chunk.Length);
            ReadLine(stream);
        }
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static string ReadLine(Stream stream)
    {
        using var ms = new MemoryStream();
        int prev = -1;
        while (true)
        {
            int b = stream.ReadByte();
            if (b == -1) break;
            if (prev == 0x0D && b == 0x0A)
            {
                var arr = ms.ToArray();
                return Encoding.ASCII.GetString(arr, 0, arr.Length - 1);
            }
            ms.WriteByte((byte)b);
            prev = b;
        }
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}
