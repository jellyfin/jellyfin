#pragma warning disable CS1591
#pragma warning disable IDISP023

using System;
using System.IO;

// from https://stackoverflow.com/a/21513194/2919731
public class PositionWrapperStream : Stream
{
    private readonly Stream wrapped;

    private long pos = 0;

    public PositionWrapperStream(Stream wrapped)
    {
        this.wrapped = wrapped;
    }

    public override bool CanSeek
    {
        get { return false; }
    }

    public override bool CanWrite
    {
        get { return true; }
    }

    public override bool CanRead => throw new NotImplementedException();

    public override long Length => throw new NotImplementedException();

    public override long Position
    {
        get { return pos; }
        set { throw new NotSupportedException(); }
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        pos += count;
        wrapped.WriteAsync(buffer, offset, count);
    }

    public override void Flush()
    {
        wrapped.FlushAsync();
    }

    protected override void Dispose(bool disposing)
    {
        wrapped.Dispose();
        base.Dispose(disposing);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }
}
