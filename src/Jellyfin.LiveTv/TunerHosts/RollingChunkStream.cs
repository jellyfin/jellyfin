#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv.TunerHosts;

/// <summary>
/// A sequential read stream that transparently advances through a list of rolling chunk
/// files produced by a live stream capture. Chunk files are added to the shared chunk-paths list
/// as they are sealed and a new chunk is opened; the oldest entries are removed when they fall
/// outside the configured rewind window.
/// </summary>
internal sealed class RollingChunkStream : Stream
{
    private readonly List<string> _chunkPaths;
    private readonly object _lock;
    private readonly ILogger _logger;
    private string _currentPath;
    private FileStream _currentStream;
    private bool _disposed;

    public RollingChunkStream(List<string> chunkPaths, object chunkLock, ILogger logger, bool seekToLiveEdge)
    {
        _chunkPaths = chunkPaths;
        _lock = chunkLock;
        _logger = logger;

        string initialPath;
        lock (_lock)
        {
            initialPath = chunkPaths[^1];
        }

        _currentPath = initialPath;
        _currentStream = OpenChunkStream(initialPath);

        if (seekToLiveEdge)
        {
            TrySeekToLiveEdge(_currentStream);
        }
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
        => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        int read = _currentStream.Read(buffer);
        if (read > 0)
        {
            return read;
        }

        if (TryAdvanceChunk())
        {
            return _currentStream.Read(buffer);
        }

        return 0;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int read = await _currentStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        if (read > 0)
        {
            return read;
        }

        if (TryAdvanceChunk())
        {
            return await _currentStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        return 0;
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _currentStream.Dispose();
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    /// <summary>
    /// Checks whether there is a sealed chunk after the current one and, if so, advances to it.
    /// Returns true when the stream was advanced and data may now be available.
    /// </summary>
    private bool TryAdvanceChunk()
    {
        string? nextPath = null;

        lock (_lock)
        {
            int idx = _chunkPaths.IndexOf(_currentPath);

            if (idx < 0)
            {
                // Current chunk was deleted (client fell behind the rewind window).
                // Skip to the oldest still-available chunk.
                if (_chunkPaths.Count > 0)
                {
                    nextPath = _chunkPaths[0];
                    _logger.LogWarning(
                        "Live stream reader fell behind the rewind window; skipping to oldest available chunk {Path}",
                        nextPath);
                }
            }
            else if (idx < _chunkPaths.Count - 1)
            {
                // Current chunk is sealed; advance to the next one.
                nextPath = _chunkPaths[idx + 1];
            }

            // else: idx == Count - 1 → at the live edge; return 0 so ProgressiveFileStream retries.
        }

        if (nextPath is null)
        {
            return false;
        }

        _currentStream.Dispose();
        _currentPath = nextPath;
        _currentStream = OpenChunkStream(nextPath);
        return true;
    }

    private static FileStream OpenChunkStream(string path)
        => new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            IODefaults.FileStreamBufferSize,
            FileOptions.SequentialScan | FileOptions.Asynchronous);

    private static void TrySeekToLiveEdge(FileStream stream)
    {
        try
        {
            stream.Seek(-20000, SeekOrigin.End);
        }
        catch (IOException)
        {
            // File is shorter than 20 KB — start from the beginning.
        }
    }
}
