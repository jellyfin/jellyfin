using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Forward-only read-only stream that yields a buffered prefix before delegating reads to an inner stream.
/// </summary>
/// <remarks>
/// Used to "peek" the first bytes of an upload for MIME sniffing without losing them
/// when the underlying stream (e.g. a <see cref="System.Security.Cryptography.CryptoStream"/>)
/// is non-seekable. The wrapper does not take ownership of the inner stream's lifetime.
/// </remarks>
public sealed class PrefixedStream : Stream
{
    private readonly byte[] _prefix;
    private readonly int _prefixLength;
    private readonly Stream _inner;
    private readonly bool _leaveOpen;
    private int _prefixPosition;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefixedStream"/> class.
    /// </summary>
    /// <param name="prefix">Buffer holding the prefix bytes.</param>
    /// <param name="prefixLength">Number of valid bytes in <paramref name="prefix"/>, starting at offset 0.</param>
    /// <param name="inner">The stream that produces the remainder of the data after the prefix is exhausted.</param>
    /// <param name="leaveOpen"><see langword="true"/> to leave the inner stream open when this wrapper is disposed; <see langword="false"/> to dispose it as well.</param>
    public PrefixedStream(byte[] prefix, int prefixLength, Stream inner, bool leaveOpen = true)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentOutOfRangeException.ThrowIfNegative(prefixLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(prefixLength, prefix.Length);

        _prefix = prefix;
        _prefixLength = prefixLength;
        _inner = inner;
        _leaveOpen = leaveOpen;
    }

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc />
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (count == 0)
        {
            return 0;
        }

        var prefixRemaining = _prefixLength - _prefixPosition;
        if (prefixRemaining > 0)
        {
            var toCopy = Math.Min(prefixRemaining, count);
            Buffer.BlockCopy(_prefix, _prefixPosition, buffer, offset, toCopy);
            _prefixPosition += toCopy;
            return toCopy;
        }

        return _inner.Read(buffer, offset, count);
    }

    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (buffer.Length == 0)
        {
            return 0;
        }

        var prefixRemaining = _prefixLength - _prefixPosition;
        if (prefixRemaining > 0)
        {
            var toCopy = Math.Min(prefixRemaining, buffer.Length);
            _prefix.AsSpan(_prefixPosition, toCopy).CopyTo(buffer.Span);
            _prefixPosition += toCopy;
            return toCopy;
        }

        return await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    /// <inheritdoc />
    public override void Flush() => throw new NotSupportedException();

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_leaveOpen)
        {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (!_leaveOpen)
        {
            await _inner.DisposeAsync().ConfigureAwait(false);
        }

        await base.DisposeAsync().ConfigureAwait(false);
    }
}
