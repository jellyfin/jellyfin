using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Streaming;

/// <summary>
/// A progressive file stream for transferring transcoded files as they are written to.
/// </summary>
public class ProgressiveFileStream : Stream
{
    private readonly Stream _stream;
    private readonly TranscodingJob? _job;
    private readonly ITranscodeManager? _transcodeManager;
    private readonly int _timeoutMs;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressiveFileStream"/> class.
    /// </summary>
    /// <param name="filePath">The path to the transcoded file.</param>
    /// <param name="job">The transcoding job information.</param>
    /// <param name="transcodeManager">The transcode manager.</param>
    /// <param name="timeoutMs">The timeout duration in milliseconds.</param>
    public ProgressiveFileStream(string filePath, TranscodingJob? job, ITranscodeManager transcodeManager, int timeoutMs = 30000)
    {
        _job = job;
        _transcodeManager = transcodeManager;
        _timeoutMs = timeoutMs;

        _stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, IODefaults.FileStreamBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressiveFileStream"/> class.
    /// </summary>
    /// <param name="stream">The stream to progressively copy.</param>
    /// <param name="timeoutMs">The timeout duration in milliseconds.</param>
    public ProgressiveFileStream(Stream stream, int timeoutMs = 30000)
    {
        _job = null;
        _transcodeManager = null;
        _timeoutMs = timeoutMs;
        _stream = stream;
    }

    /// <inheritdoc />
    public override bool CanRead => _stream.CanRead;

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
    public override void Flush()
    {
        // Not supported
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
        => Read(buffer.AsSpan(offset, count));

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        int totalBytesRead = 0;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            totalBytesRead += _stream.Read(buffer);
            if (StopReading(totalBytesRead, stopwatch.ElapsedMilliseconds))
            {
                break;
            }

            Thread.Sleep(50);
        }

        UpdateBytesWritten(totalBytesRead);

        return totalBytesRead;
    }

    /// <inheritdoc />
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => await ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int totalBytesRead = 0;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            totalBytesRead += await _stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (StopReading(totalBytesRead, stopwatch.ElapsedMilliseconds))
            {
                break;
            }

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        UpdateBytesWritten(totalBytesRead);

        return totalBytesRead;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (disposing)
            {
                _stream.Dispose();

                if (_job is not null)
                {
                    _transcodeManager?.OnTranscodeEndRequest(_job);
                }
            }
        }
        finally
        {
            _disposed = true;
            base.Dispose(disposing);
        }
    }

    private void UpdateBytesWritten(int totalBytesRead)
    {
        if (_job is not null)
        {
            _job.BytesDownloaded += totalBytesRead;
        }
    }

    private bool StopReading(int bytesRead, long elapsed)
    {
        // It should stop reading when anything has been successfully read or if the job has exited
        // If the job is null, however, it's a live stream and will require user action to close,
        // but don't keep it open indefinitely if it isn't reading anything
        return bytesRead > 0 || (_job?.HasExited ?? elapsed >= _timeoutMs);
    }
}
