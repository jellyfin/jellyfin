using System;
using System.IO;
using System.Runtime.InteropServices;
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
        private readonly FileStream _fileStream;
        private readonly TranscodingJobDto? _job;
        private readonly TranscodingJobHelper _transcodingJobHelper;
        private readonly bool _allowAsyncFileRead;
        private int _bytesWritten;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressiveFileStream"/> class.
        /// </summary>
        /// <param name="filePath">The path to the transcoded file.</param>
        /// <param name="job">The transcoding job information.</param>
        /// <param name="transcodingJobHelper">The transcoding job helper.</param>
        public ProgressiveFileStream(string filePath, TranscodingJobDto? job, TranscodingJobHelper transcodingJobHelper)
        {
            _job = job;
            _transcodingJobHelper = transcodingJobHelper;
            _bytesWritten = 0;

            var fileOptions = FileOptions.SequentialScan;
            _allowAsyncFileRead = false;

            // use non-async filestream along with read due to https://github.com/dotnet/corefx/issues/6039
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fileOptions |= FileOptions.Asynchronous;
                _allowAsyncFileRead = true;
            }

            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, IODefaults.FileStreamBufferSize, fileOptions);
        }

        /// <inheritdoc />
        public override bool CanRead => _fileStream.CanRead;

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
            _fileStream.Flush();
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            return _fileStream.Read(buffer, offset, count);
        }

        /// <inheritdoc />
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var eofCount = 0;
            const int EmptyReadLimit = 20;

            int totalBytesRead = 0;
            int remainingBytesToRead = count;

            while (eofCount < EmptyReadLimit && remainingBytesToRead > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int bytesRead;
                if (_allowAsyncFileRead)
                {
                    bytesRead = await _fileStream.ReadAsync(buffer, offset, remainingBytesToRead, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    bytesRead = _fileStream.Read(buffer, offset, remainingBytesToRead);
                }

                remainingBytesToRead -= bytesRead;
                if (bytesRead > 0)
                {
                    _bytesWritten += bytesRead;
                    totalBytesRead += bytesRead;

                    if (_job != null)
                    {
                        _job.BytesDownloaded = Math.Max(_job.BytesDownloaded ?? _bytesWritten, _bytesWritten);
                    }
                }

                if (bytesRead == 0)
                {
                    if (_job == null || _job.HasExited)
                    {
                        eofCount++;
                    }

                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    eofCount = 0;
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

            if (disposing)
            {
                _fileStream.Dispose();

                if (_job != null)
                {
                    _transcodingJobHelper.OnTranscodeEndRequest(_job);
                }
            }

            _disposed = true;
        }
    }
}
