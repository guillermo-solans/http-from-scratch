using System;
using System.IO;

namespace TlsLib.Util;

internal sealed class BigEndianWriter
{
    private readonly MemoryStream _ms;

    public BigEndianWriter()
    {
        _ms = new MemoryStream();
    }

    public BigEndianWriter(int initialCapacity)
    {
        _ms = new MemoryStream(initialCapacity);
    }

    public int Length => (int)_ms.Length;

    public void WriteUInt8(byte value)
    {
        _ms.WriteByte(value);
    }

    public void WriteUInt16(ushort value)
    {
        _ms.WriteByte((byte)(value >> 8));
        _ms.WriteByte((byte)value);
    }

    public void WriteUInt24(uint value)
    {
        if (value > 0x00FFFFFF)
            throw new ArgumentOutOfRangeException(nameof(value), "Value exceeds 24-bit range");

        _ms.WriteByte((byte)(value >> 16));
        _ms.WriteByte((byte)(value >> 8));
        _ms.WriteByte((byte)value);
    }

    public void WriteUInt32(uint value)
    {
        _ms.WriteByte((byte)(value >> 24));
        _ms.WriteByte((byte)(value >> 16));
        _ms.WriteByte((byte)(value >> 8));
        _ms.WriteByte((byte)value);
    }

    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        _ms.Write(bytes);
    }

    public void WriteVector8(Action<BigEndianWriter> content)
    {
        long lengthPos = _ms.Position;
        _ms.WriteByte(0);
        long start = _ms.Position;
        content(this);
        long end = _ms.Position;
        long size = end - start;
        if (size > 0xFF)
            throw new InvalidOperationException($"Vector8 length {size} exceeds 1-byte limit");
        _ms.Position = lengthPos;
        _ms.WriteByte((byte)size);
        _ms.Position = end;
    }

    public void WriteVector16(Action<BigEndianWriter> content)
    {
        long lengthPos = _ms.Position;
        _ms.WriteByte(0);
        _ms.WriteByte(0);
        long start = _ms.Position;
        content(this);
        long end = _ms.Position;
        long size = end - start;
        if (size > 0xFFFF)
            throw new InvalidOperationException($"Vector16 length {size} exceeds 2-byte limit");
        _ms.Position = lengthPos;
        _ms.WriteByte((byte)(size >> 8));
        _ms.WriteByte((byte)size);
        _ms.Position = end;
    }

    public void WriteVector24(Action<BigEndianWriter> content)
    {
        long lengthPos = _ms.Position;
        _ms.WriteByte(0);
        _ms.WriteByte(0);
        _ms.WriteByte(0);
        long start = _ms.Position;
        content(this);
        long end = _ms.Position;
        long size = end - start;
        if (size > 0xFFFFFF)
            throw new InvalidOperationException($"Vector24 length {size} exceeds 3-byte limit");
        _ms.Position = lengthPos;
        _ms.WriteByte((byte)(size >> 16));
        _ms.WriteByte((byte)(size >> 8));
        _ms.WriteByte((byte)size);
        _ms.Position = end;
    }

    public byte[] ToArray()
    {
        return _ms.ToArray();
    }
}
