using TlsLib.Protocol;
using TlsLib.Records;
using TlsLib.Util;

namespace TlsLib.Handshake;

/// <summary>
/// Buffers Handshake-record fragments so a caller can pull one full
/// handshake message at a time.
/// </summary>
internal sealed class HandshakeReader
{
    private readonly Stream _stream;
    private readonly Queue<byte[]> _pendingFragments = new();
    private byte[] _current = Array.Empty<byte>();
    private int _offset;

    public HandshakeReader(Stream stream)
    {
        _stream = stream;
    }

    public (HandshakeType Type, byte[] Body, byte[] FullBytes) Read()
    {
        EnsureBytes(4);
        byte typeByte = _current[_offset];
        int length = (_current[_offset + 1] << 16) | (_current[_offset + 2] << 8) | _current[_offset + 3];

        EnsureBytes(4 + length);

        var fullBytes = new byte[4 + length];
        Buffer.BlockCopy(_current, _offset, fullBytes, 0, 4 + length);
        var body = new byte[length];
        Buffer.BlockCopy(_current, _offset + 4, body, 0, length);
        _offset += 4 + length;

        return ((HandshakeType)typeByte, body, fullBytes);
    }

    public TlsRecord ReadNonHandshakeRecord()
    {
        if (_offset < _current.Length)
            throw new InvalidOperationException("Pending handshake bytes; cannot read non-handshake record now.");

        return TlsRecordReader.Read(_stream);
    }

    private void EnsureBytes(int needed)
    {
        while (_current.Length - _offset < needed)
        {
            ReadOnlySpan<byte> tail = _current.AsSpan(_offset);
            byte[] next = ReadNextHandshakeFragment();
            if (tail.Length == 0)
            {
                _current = next;
            }
            else
            {
                var combined = new byte[tail.Length + next.Length];
                tail.CopyTo(combined);
                Buffer.BlockCopy(next, 0, combined, tail.Length, next.Length);
                _current = combined;
            }
            _offset = 0;
        }
    }

    private byte[] ReadNextHandshakeFragment()
    {
        var record = TlsRecordReader.Read(_stream);
        if (record.ContentType != ContentType.Handshake)
        {
            if (record.ContentType == ContentType.Alert)
                throw new TlsException(
                    $"Received alert during handshake: level={record.Payload[0]} desc={record.Payload[1]}",
                    AlertDescription.UnexpectedMessage);

            throw new TlsException(
                $"Expected Handshake record, got {record.ContentType}",
                AlertDescription.UnexpectedMessage);
        }
        return record.Payload;
    }
}

internal static class HandshakeWriter
{
    public static byte[] BuildMessage(HandshakeType type, byte[] body)
    {
        var w = new BigEndianWriter(4 + body.Length);
        w.WriteUInt8((byte)type);
        w.WriteUInt24((uint)body.Length);
        w.WriteBytes(body);
        return w.ToArray();
    }

    public static void WriteToStream(Stream stream, HandshakeType type, byte[] body)
    {
        byte[] full = BuildMessage(type, body);
        TlsRecordWriter.Write(stream, ContentType.Handshake, full);
    }
}
