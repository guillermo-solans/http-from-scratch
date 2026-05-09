using TlsLib.Protocol;
using TlsLib.Records;
using TlsLib.State;

namespace TlsLib;

public sealed class TlsStream : Stream
{
    private readonly Stream _inner;
    private readonly bool _leaveInnerOpen;
    internal readonly object _stateLock = new();
    internal TlsConnectionState State { get; }
    private readonly Action<string>? _logger;

    private byte[] _readBuffer = Array.Empty<byte>();
    private int _readOffset;
    private bool _closed;
    private bool _innerClosed;

    internal TlsStream(Stream inner, TlsConnectionState state, Action<string>? logger, bool leaveInnerOpen = false)
    {
        _inner = inner;
        State = state;
        _logger = logger;
        _leaveInnerOpen = leaveInnerOpen;
    }

    public override bool CanRead => !_closed;
    public override bool CanWrite => !_closed;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() => _inner.Flush();

    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (offset < 0 || offset > buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));
        if (count < 0 || offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));
        if (count == 0) return 0;
        if (_closed) return 0;

        if (_readOffset >= _readBuffer.Length)
        {
            if (!FillReadBuffer())
                return 0;
        }

        int available = _readBuffer.Length - _readOffset;
        int toCopy = Math.Min(count, available);
        Buffer.BlockCopy(_readBuffer, _readOffset, buffer, offset, toCopy);
        _readOffset += toCopy;
        return toCopy;
    }

    private bool FillReadBuffer()
    {
        while (true)
        {
            TlsRecord record;
            try
            {
                record = TlsRecordReader.Read(_inner);
            }
            catch (IOException)
            {
                _closed = true;
                return false;
            }

            switch (record.ContentType)
            {
                case ContentType.ApplicationData:
                {
                    byte[] plaintext = State.ReadCipher.Decrypt(
                        State.ReadSequenceNumber, (byte)ContentType.ApplicationData, record.Payload);
                    State.ReadSequenceNumber++;
                    _readBuffer = plaintext;
                    _readOffset = 0;
                    if (plaintext.Length == 0) continue;
                    return true;
                }
                case ContentType.Alert:
                {
                    byte[] alertPayload = State.ReadCipher.Decrypt(
                        State.ReadSequenceNumber, (byte)ContentType.Alert, record.Payload);
                    State.ReadSequenceNumber++;

                    if (alertPayload.Length < 2)
                        throw new TlsException("Malformed alert", AlertDescription.DecodeError);

                    var level = (AlertLevel)alertPayload[0];
                    var desc = (AlertDescription)alertPayload[1];
                    _logger?.Invoke($"[tls] received alert level={level} desc={desc}");

                    if (desc == AlertDescription.CloseNotify || level == AlertLevel.Fatal)
                    {
                        _closed = true;
                        return false;
                    }
                    continue;
                }
                case ContentType.Handshake:
                    _logger?.Invoke("[tls] ignoring handshake record after handshake complete (no renegotiation)");
                    continue;
                case ContentType.ChangeCipherSpec:
                    _logger?.Invoke("[tls] unexpected ChangeCipherSpec after handshake complete");
                    continue;
                default:
                    throw new TlsException(
                        $"Unexpected content type {record.ContentType}",
                        AlertDescription.UnexpectedMessage);
            }
        }
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (offset < 0 || offset > buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));
        if (count < 0 || offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));
        if (_closed) throw new InvalidOperationException("Stream closed");

        int written = 0;
        while (written < count)
        {
            int chunkSize = Math.Min(TlsConstants.MaxRecordPayload, count - written);
            var chunk = new byte[chunkSize];
            Buffer.BlockCopy(buffer, offset + written, chunk, 0, chunkSize);

            byte[] encrypted = State.WriteCipher.Encrypt(
                State.WriteSequenceNumber, (byte)ContentType.ApplicationData, chunk);
            State.WriteSequenceNumber++;

            TlsRecordWriter.Write(_inner, ContentType.ApplicationData, encrypted);
            written += chunkSize;
        }
    }

    public override void SetLength(long value) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    private void SendCloseNotify()
    {
        try
        {
            byte[] alert = new byte[] { (byte)AlertLevel.Warning, (byte)AlertDescription.CloseNotify };
            byte[] encrypted = State.WriteCipher.Encrypt(
                State.WriteSequenceNumber, (byte)ContentType.Alert, alert);
            State.WriteSequenceNumber++;
            TlsRecordWriter.Write(_inner, ContentType.Alert, encrypted);
            _logger?.Invoke("[tls] sent close_notify");
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"[tls] failed to send close_notify: {ex.Message}");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_closed)
        {
            _closed = true;
            SendCloseNotify();
            State.Dispose();
            if (!_leaveInnerOpen && !_innerClosed)
            {
                _innerClosed = true;
                _inner.Dispose();
            }
        }
        base.Dispose(disposing);
    }
}
