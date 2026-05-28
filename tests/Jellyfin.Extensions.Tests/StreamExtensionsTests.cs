using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using Xunit;

namespace Jellyfin.Extensions.Tests;

public class StreamExtensionsTests
{
    [Fact]
    public async Task IsStreamIdenticalAsync_SeekableDifferentLengths_ReturnsFalse()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var a = new MemoryStream(new byte[] { 1, 2, 3 });
        await using var b = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        var result = await a.IsStreamIdenticalAsync(b, cancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task IsStreamIdenticalAsync_NonSeekableIdenticalStreams_ReturnsTrue()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var a = new NonSeekableReadStream(new byte[] { 1, 2, 3, 4 });
        await using var b = new NonSeekableReadStream(new byte[] { 1, 2, 3, 4 });

        var result = await a.IsStreamIdenticalAsync(b, cancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task IsStreamIdenticalAsync_NonSeekableDifferentStreams_ReturnsFalse()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var a = new NonSeekableReadStream(new byte[] { 1, 2, 3, 4 });
        await using var b = new NonSeekableReadStream(new byte[] { 1, 2, 9, 4 });

        var result = await a.IsStreamIdenticalAsync(b, cancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task IsFileIdenticalAsync_NonSeekableStream_ThrowsArgumentException()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var path = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
        await File.WriteAllBytesAsync(path, new byte[] { 1, 2, 3, 4 }, cancellationToken);

        try
        {
            await using var stream = new NonSeekableReadStream(new byte[] { 1, 2, 3, 4 });

            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await stream.IsFileIdenticalAsync(path, cancellationToken));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task IsFileIdenticalAsync_UsesStartOfStreamAndRestoresPosition_OnMatch()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var path = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
        var bytes = new byte[] { 10, 20, 30, 40, 50 };
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);

        try
        {
            await using var stream = new MemoryStream(bytes);
            stream.Position = 3;

            var result = await stream.IsFileIdenticalAsync(path, cancellationToken);

            Assert.True(result);
            Assert.Equal(3, stream.Position);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task IsFileIdenticalAsync_RestoresPosition_OnMismatch()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var path = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
        await File.WriteAllBytesAsync(path, new byte[] { 10, 20, 30, 40, 99 }, cancellationToken);

        try
        {
            await using var stream = new MemoryStream(new byte[] { 10, 20, 30, 40, 50 });
            stream.Position = 2;

            var result = await stream.IsFileIdenticalAsync(path, cancellationToken);

            Assert.False(result);
            Assert.Equal(2, stream.Position);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class NonSeekableReadStream : Stream
    {
        private readonly Stream _inner;

        public NonSeekableReadStream(byte[] data)
        {
            _inner = new MemoryStream(data, writable: false);
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
            => _inner.Read(buffer, offset, count);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => _inner.ReadAsync(buffer, cancellationToken);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await _inner.DisposeAsync();
            await base.DisposeAsync();
        }
    }
}
