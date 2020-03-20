using System;
using System.IO;

public sealed class HttpPostedFile : IDisposable
{
    private string _name;
    private string _contentType;
    private Stream _stream;
    private bool _disposed = false;

    internal HttpPostedFile(string name, string content_type, Stream base_stream, long offset, long length)
    {
        _name = name;
        _contentType = content_type;
        _stream = new ReadSubStream(base_stream, offset, length);
    }

    public string ContentType => _contentType;

    public int ContentLength => (int)_stream.Length;

    public string FileName => _name;

    public Stream InputStream => _stream;

    /// <summary>
    /// Releases the unmanaged resources and disposes of the managed resources used.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _stream.Dispose();
        _stream = null;

        _name = null;
        _contentType = null;

        _disposed = true;
    }

    private class ReadSubStream : Stream
    {
        private Stream _stream;
        private long _offset;
        private long _end;
        private long _position;

        public ReadSubStream(Stream s, long offset, long length)
        {
            _stream = s;
            _offset = offset;
            _end = offset + length;
            _position = offset;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _end - _offset;

        public override long Position
        {
            get => _position - _offset;
            set
            {
                if (value > Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _position = Seek(value, SeekOrigin.Begin);
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int dest_offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (dest_offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dest_offset), "< 0");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "< 0");
            }

            int len = buffer.Length;
            if (dest_offset > len)
            {
                throw new ArgumentException("destination offset is beyond array size", nameof(dest_offset));
            }

            // reordered to avoid possible integer overflow
            if (dest_offset > len - count)
            {
                throw new ArgumentException("Reading would overrun buffer", nameof(count));
            }

            if (count > _end - _position)
            {
                count = (int)(_end - _position);
            }

            if (count <= 0)
            {
                return 0;
            }

            _stream.Position = _position;
            int result = _stream.Read(buffer, dest_offset, count);
            if (result > 0)
            {
                _position += result;
            }
            else
            {
                _position = _end;
            }

            return result;
        }

        public override int ReadByte()
        {
            if (_position >= _end)
            {
                return -1;
            }

            _stream.Position = _position;
            int result = _stream.ReadByte();
            if (result < 0)
            {
                _position = _end;
            }
            else
            {
                _position++;
            }

            return result;
        }

        public override long Seek(long d, SeekOrigin origin)
        {
            long real;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    real = _offset + d;
                    break;
                case SeekOrigin.End:
                    real = _end + d;
                    break;
                case SeekOrigin.Current:
                    real = _position + d;
                    break;
                default:
                    throw new ArgumentException("Unknown SeekOrigin value", nameof(origin));
            }

            long virt = real - _offset;
            if (virt < 0 || virt > Length)
            {
                throw new ArgumentException("Invalid position", nameof(d));
            }

            _position = _stream.Seek(real, SeekOrigin.Begin);
            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
