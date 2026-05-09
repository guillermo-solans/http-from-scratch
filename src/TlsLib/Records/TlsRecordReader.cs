using System;
using System.IO;
using TlsLib.Protocol;

namespace TlsLib.Records;

internal static class TlsRecordReader
{
    public static TlsRecord Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var header = ReadExactly(stream, TlsConstants.RecordHeaderLength);

        byte contentTypeByte = header[0];
        ushort version = (ushort)((header[1] << 8) | header[2]);
        int payloadLength = (header[3] << 8) | header[4];

        if (!IsValidContentType(contentTypeByte))
            throw new TlsException(
                $"Invalid TLS record content type: 0x{contentTypeByte:X2}",
                AlertDescription.UnexpectedMessage);

        if (payloadLength < 0 || payloadLength > TlsConstants.MaxEncryptedRecordPayload)
            throw new TlsException(
                $"TLS record payload length {payloadLength} exceeds maximum allowed ({TlsConstants.MaxEncryptedRecordPayload})",
                AlertDescription.RecordOverflow);

        byte[] payload = payloadLength == 0
            ? Array.Empty<byte>()
            : ReadExactly(stream, payloadLength);

        return new TlsRecord
        {
            ContentType = (ContentType)contentTypeByte,
            Version = version,
            Payload = payload
        };
    }

    private static bool IsValidContentType(byte value)
    {
        return value is (byte)ContentType.ChangeCipherSpec
            or (byte)ContentType.Alert
            or (byte)ContentType.Handshake
            or (byte)ContentType.ApplicationData;
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
}
