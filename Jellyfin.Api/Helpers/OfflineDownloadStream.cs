using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Disposes the progressive response, then cleans only its unique download session.
/// Both normal completion and client disconnect dispose the ASP.NET response stream.
/// </summary>
public sealed class OfflineDownloadStream : Stream
{
    private readonly Stream _inner;
    private readonly Func<Task> _cleanup;
    private readonly Action? _validateEnd;
    private int _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OfflineDownloadStream"/> class.
    /// </summary>
    /// <param name="inner">The progressive stream.</param>
    /// <param name="cleanup">Cleanup restricted to this request's unique session.</param>
    /// <param name="validateEnd">Optional validation before a successful end of response.</param>
    public OfflineDownloadStream(Stream inner, Func<Task> cleanup, Action? validateEnd = null)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(cleanup);
        _inner = inner;
        _cleanup = cleanup;
        _validateEnd = validateEnd;
    }

    /// <inheritdoc />
    public override bool CanRead => _inner.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => _inner.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => _inner.Length;

    /// <inheritdoc />
    public override long Position { get => _inner.Position; set => _inner.Position = value; }

    /// <inheritdoc />
    public override void Flush() => _inner.Flush();

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
        => CheckEnd(_inner.Read(buffer, offset, count), count);

    /// <inheritdoc />
    public override int Read(Span<byte> buffer) => CheckEnd(_inner.Read(buffer), buffer.Length);

    /// <inheritdoc />
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => CheckEnd(await _inner.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false), count);

    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => CheckEnd(await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false), buffer.Length);

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    private int CheckEnd(int bytes, int requested)
    {
        if (bytes == 0 && requested > 0)
        {
            _validateEnd?.Invoke();
        }

        return bytes;
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        try
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                try
                {
                    await _inner.DisposeAsync().ConfigureAwait(false);
                }
                finally
                {
                    await _cleanup().ConfigureAwait(false);
                }
            }
        }
        finally
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        try
        {
            if (disposing)
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    try
                    {
                        _inner.Dispose();
                    }
                    finally
                    {
                        // ASP.NET uses DisposeAsync; synchronous callers still receive deterministic cleanup.
                        _cleanup().GetAwaiter().GetResult();
                    }
                }
            }
        }
        finally
        {
            base.Dispose(disposing);
        }
    }
}
