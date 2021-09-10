using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Models.PlaybackDtos;
using MediaBrowser.Model.IO;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// A progressive file stream for transferring transcoded files as they are written to.
    /// </summary>
    public class ProgressiveFileStream : Stream
    {
        private readonly Stream _stream;
        private readonly TranscodingJobDto? _job;
        private readonly TranscodingJobHelper? _transcodingJobHelper;
        private readonly int _timeoutMs;
        private readonly bool _allowAsyncFileRead;
        private int _bytesWritten;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressiveFileStream"/> class.
        /// </summary>
        /// <param name="filePath">The path to the transcoded file.</param>
        /// <param name="job">The transcoding job information.</param>
        /// <param name="transcodingJobHelper">The transcoding job helper.</param>
        /// <param name="timeoutMs">The timeout duration in milliseconds.</param>
        public ProgressiveFileStream(string filePath, TranscodingJobDto? job, TranscodingJobHelper transcodingJobHelper, int timeoutMs = 30000)
        {
            _job = job;
            _transcodingJobHelper = transcodingJobHelper;
            _timeoutMs = timeoutMs;

            var fileOptions = FileOptions.SequentialScan;
            _allowAsyncFileRead = false;

            // use non-async filestream along with read due to https://github.com/dotnet/corefx/issues/6039
            if (AsyncFile.UseAsyncIO)
            {
                fileOptions |= FileOptions.Asynchronous;
                _allowAsyncFileRead = true;
            }

            _stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, IODefaults.FileStreamBufferSize, fileOptions);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressiveFileStream"/> class.
        /// </summary>
        /// <param name="stream">The stream to progressively copy.</param>
        /// <param name="job">The transcoding job information.</param>
        /// <param name="transcodingJobHelper">The transcoding job helper.</param>
        /// <param name="timeoutMs">The timeout duration in milliseconds.</param>
        public ProgressiveFileStream(Stream stream, TranscodingJobDto? job, TranscodingJobHelper? transcodingJobHelper, int timeoutMs = 30000)
        {
            _job = job;
            _transcodingJobHelper = transcodingJobHelper;
            _timeoutMs = timeoutMs;
            _allowAsyncFileRead = AsyncFile.UseAsyncIO;
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
            _stream.Flush();
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        /// <inheritdoc />
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int totalBytesRead = 0;
            int remainingBytesToRead = count;
            var stopwatch = Stopwatch.StartNew();

            int newOffset = offset;
            while (remainingBytesToRead > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int bytesRead;
                if (_allowAsyncFileRead)
                {
                    bytesRead = await _stream.ReadAsync(buffer, newOffset, remainingBytesToRead, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    bytesRead = _stream.Read(buffer, newOffset, remainingBytesToRead);
                }

                remainingBytesToRead -= bytesRead;
                newOffset += bytesRead;

                if (bytesRead > 0)
                {
                    _bytesWritten += bytesRead;
                    totalBytesRead += bytesRead;

                    if (_job != null)
                    {
                        _job.BytesDownloaded = Math.Max(_job.BytesDownloaded ?? _bytesWritten, _bytesWritten);
                    }
                }
                else
                {
                    // If the job is null it's a live stream and will require user action to close, but don't keep it open indefinitely
                    if (_job?.HasExited ?? stopwatch.ElapsedMilliseconds > _timeoutMs)
                    {
                        break;
                    }

                    await Task.Delay(50, cancellationToken).ConfigureAwait(false);
                }
            }

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

                    if (_job != null)
                    {
                        _transcodingJobHelper?.OnTranscodeEndRequest(_job);
                    }
                }
            }
            finally
            {
                _disposed = true;
                base.Dispose(disposing);
            }
        }
    }
}
