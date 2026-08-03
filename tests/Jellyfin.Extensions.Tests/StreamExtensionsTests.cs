using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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

    // Both publiclyVisible values are exercised so the test runs once under the fast path
    // (TryGetBuffer succeeds) and once under the slow path (TryGetBuffer returns false).
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsFileIdenticalAsync_UsesStartOfStreamAndRestoresPosition_OnMatch(bool publiclyVisible)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var path = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
        var bytes = new byte[] { 10, 20, 30, 40, 50 };
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);

        try
        {
            await using var stream = CreateMemoryStream(bytes, publiclyVisible);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsFileIdenticalAsync_RestoresPosition_OnMismatch(bool publiclyVisible)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var path = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
        await File.WriteAllBytesAsync(path, new byte[] { 10, 20, 30, 40, 99 }, cancellationToken);

        try
        {
            await using var stream = CreateMemoryStream(new byte[] { 10, 20, 30, 40, 50 }, publiclyVisible);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsStreamIdenticalAsync_BothMemoryStreams_NonZeroPositions_SeeksToStart(bool publiclyVisible)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var a = CreateMemoryStream(new byte[] { 1, 2, 3, 4, 5 }, publiclyVisible);
        await using var b = CreateMemoryStream(new byte[] { 1, 2, 3, 4, 5 }, publiclyVisible);
        a.Position = 3;
        b.Position = 1;

        var result = await a.IsStreamIdenticalAsync(b, cancellationToken);

        Assert.True(result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsStreamIdenticalAsync_MemoryStreamPairedWithSeekableNonMemoryStream_NonZeroPositions_SeeksToStart(bool publiclyVisible)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var a = CreateMemoryStream(new byte[] { 1, 2, 3, 4 }, publiclyVisible);
        await using var b = new SeekableNonMemoryStream(new byte[] { 1, 2, 3, 4 });
        a.Position = 2;
        b.Position = 3;

        var result = await a.IsStreamIdenticalAsync(b, cancellationToken);

        Assert.True(result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsStreamIdenticalAsync_NonMemoryStreamPairedWithMemoryStream_Swaps_ReturnsTrue(bool publiclyVisible)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var a = new SeekableNonMemoryStream(new byte[] { 1, 2, 3, 4 });
        await using var b = CreateMemoryStream(new byte[] { 1, 2, 3, 4 }, publiclyVisible);

        var result = await a.IsStreamIdenticalAsync(b, cancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task IsStreamIdenticalAsync_BothSeekableNonMemoryStreams_NonZeroPositions_SeeksToStart()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var a = new SeekableNonMemoryStream(new byte[] { 1, 2, 3, 4 });
        await using var b = new SeekableNonMemoryStream(new byte[] { 1, 2, 3, 4 });
        a.Position = 1;
        b.Position = 2;

        var result = await a.IsStreamIdenticalAsync(b, cancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task IsStreamIdenticalAsync_NonSeekableShortReads_Identical_ReturnsTrue()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        await using var a = new ShortReadingNonSeekableStream(data, maxReadSize: 3);
        await using var b = new ShortReadingNonSeekableStream(data, maxReadSize: 5);

        var result = await a.IsStreamIdenticalAsync(b, cancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task IsStreamIdenticalAsync_NonSeekableShortReads_DifferentLengths_ReturnsFalse()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var a = new ShortReadingNonSeekableStream(new byte[] { 1, 2, 3, 4 }, maxReadSize: 3);
        await using var b = new ShortReadingNonSeekableStream(new byte[] { 1, 2, 3, 4, 5 }, maxReadSize: 5);

        var result = await a.IsStreamIdenticalAsync(b, cancellationToken);

        Assert.False(result);
    }

    private static MemoryStream CreateMemoryStream(byte[] data, bool publiclyVisible)
        => publiclyVisible
            ? new MemoryStream(data, 0, data.Length, writable: false, publiclyVisible: true)
            : new MemoryStream(data);

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

    private sealed class SeekableNonMemoryStream : Stream
    {
        private readonly MemoryStream _inner;

        public SeekableNonMemoryStream(byte[] data)
        {
            _inner = new MemoryStream(data, writable: false);
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
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
            => _inner.Seek(offset, origin);

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

    private sealed class ShortReadingNonSeekableStream : Stream
    {
        private readonly Stream _inner;
        private readonly int _maxReadSize;

        public ShortReadingNonSeekableStream(byte[] data, int maxReadSize)
        {
            _inner = new MemoryStream(data, writable: false);
            _maxReadSize = maxReadSize;
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
            => _inner.Read(buffer, offset, Math.Min(count, _maxReadSize));

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => _inner.ReadAsync(buffer[..Math.Min(buffer.Length, _maxReadSize)], cancellationToken);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _inner.ReadAsync(buffer.AsMemory(offset, Math.Min(count, _maxReadSize)), cancellationToken).AsTask();

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
