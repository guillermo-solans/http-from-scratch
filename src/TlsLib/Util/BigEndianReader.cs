using System;

namespace TlsLib.Util;

internal sealed class BigEndianReader
{
    private readonly byte[] _buffer;
    private readonly int _start;
    private readonly int _end;
    private int _offset;

    public BigEndianReader(byte[] buffer)
        : this(buffer, 0, buffer.Length)
    {
    }

    public BigEndianReader(byte[] buffer, int start, int length)
    {
        if (start < 0 || start > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0 || start + length > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(length));

        _buffer = buffer;
        _start = start;
        _end = start + length;
        _offset = start;
    }

    public int Remaining => _end - _offset;

    public int Position => _offset - _start;

    private void EnsureAvailable(int count)
    {
        if (Remaining < count)
            throw new InvalidOperationException($"Not enough bytes: needed {count}, have {Remaining}");
    }

    public byte ReadUInt8()
    {
        EnsureAvailable(1);
        return _buffer[_offset++];
    }

    public ushort ReadUInt16()
    {
        EnsureAvailable(2);
        ushort value = (ushort)((_buffer[_offset] << 8) | _buffer[_offset + 1]);
        _offset += 2;
        return value;
    }

    public uint ReadUInt24()
    {
        EnsureAvailable(3);
        uint value = (uint)((_buffer[_offset] << 16) | (_buffer[_offset + 1] << 8) | _buffer[_offset + 2]);
        _offset += 3;
        return value;
    }

    public uint ReadUInt32()
    {
        EnsureAvailable(4);
        uint value = (uint)((_buffer[_offset] << 24) | (_buffer[_offset + 1] << 16) | (_buffer[_offset + 2] << 8) | _buffer[_offset + 3]);
        _offset += 4;
        return value;
    }

    public ReadOnlySpan<byte> ReadBytes(int length)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));
        EnsureAvailable(length);
        var slice = new ReadOnlySpan<byte>(_buffer, _offset, length);
        _offset += length;
        return slice;
    }

    public ReadOnlySpan<byte> ReadVector8()
    {
        int length = ReadUInt8();
        return ReadBytes(length);
    }

    public ReadOnlySpan<byte> ReadVector16()
    {
        int length = ReadUInt16();
        return ReadBytes(length);
    }

    public ReadOnlySpan<byte> ReadVector24()
    {
        int length = (int)ReadUInt24();
        return ReadBytes(length);
    }
}
