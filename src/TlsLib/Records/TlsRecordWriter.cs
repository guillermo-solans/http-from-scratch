using System;
using System.IO;
using TlsLib.Protocol;

namespace TlsLib.Records;

internal static class TlsRecordWriter
{
    public static void Write(Stream stream, ContentType contentType, byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(payload);

        if (payload.Length == 0)
        {
            WriteSingleRecord(stream, contentType, payload, 0, 0);
            return;
        }

        int offset = 0;
        while (offset < payload.Length)
        {
            int chunkSize = Math.Min(TlsConstants.MaxRecordPayload, payload.Length - offset);
            WriteSingleRecord(stream, contentType, payload, offset, chunkSize);
            offset += chunkSize;
        }
    }

    private static void WriteSingleRecord(Stream stream, ContentType contentType, byte[] payload, int offset, int length)
    {
        if (length > TlsConstants.MaxEncryptedRecordPayload)
            throw new TlsException(
                $"TLS record fragment length {length} exceeds maximum",
                AlertDescription.RecordOverflow);

        Span<byte> header = stackalloc byte[TlsConstants.RecordHeaderLength];
        header[0] = (byte)contentType;
        header[1] = (byte)(TlsConstants.TlsVersion >> 8);
        header[2] = (byte)(TlsConstants.TlsVersion & 0xFF);
        header[3] = (byte)(length >> 8);
        header[4] = (byte)length;

        stream.Write(header);
        if (length > 0)
            stream.Write(payload, offset, length);
        stream.Flush();
    }
}
