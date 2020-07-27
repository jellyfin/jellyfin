using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Models.PlaybackDtos;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// Progressive file copier.
    /// </summary>
    public class ProgressiveFileCopier
    {
        private readonly TranscodingJobDto? _job;
        private readonly string? _path;
        private readonly CancellationToken _cancellationToken;
        private readonly IDirectStreamProvider? _directStreamProvider;
        private readonly TranscodingJobHelper _transcodingJobHelper;
        private long _bytesWritten;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressiveFileCopier"/> class.
        /// </summary>
        /// <param name="path">The path to copy from.</param>
        /// <param name="job">The transcoding job.</param>
        /// <param name="transcodingJobHelper">Instance of the <see cref="TranscodingJobHelper"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public ProgressiveFileCopier(string path, TranscodingJobDto? job, TranscodingJobHelper transcodingJobHelper, CancellationToken cancellationToken)
        {
            _path = path;
            _job = job;
            _cancellationToken = cancellationToken;
            _transcodingJobHelper = transcodingJobHelper;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressiveFileCopier"/> class.
        /// </summary>
        /// <param name="directStreamProvider">Instance of the <see cref="IDirectStreamProvider"/> interface.</param>
        /// <param name="job">The transcoding job.</param>
        /// <param name="transcodingJobHelper">Instance of the <see cref="TranscodingJobHelper"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public ProgressiveFileCopier(IDirectStreamProvider directStreamProvider, TranscodingJobDto? job, TranscodingJobHelper transcodingJobHelper, CancellationToken cancellationToken)
        {
            _directStreamProvider = directStreamProvider;
            _job = job;
            _cancellationToken = cancellationToken;
            _transcodingJobHelper = transcodingJobHelper;
        }

        /// <summary>
        /// Gets or sets a value indicating whether allow read end of file.
        /// </summary>
        public bool AllowEndOfFile { get; set; } = true;

        /// <summary>
        /// Gets or sets copy start position.
        /// </summary>
        public long StartPosition { get; set; }

        /// <summary>
        /// Write source stream to output.
        /// </summary>
        /// <param name="outputStream">Output stream.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        public async Task WriteToAsync(Stream outputStream, CancellationToken cancellationToken)
        {
            cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationToken).Token;

            try
            {
                if (_directStreamProvider != null)
                {
                    await _directStreamProvider.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
                    return;
                }

                var fileOptions = FileOptions.SequentialScan;
                var allowAsyncFileRead = false;

                // use non-async filestream along with read due to https://github.com/dotnet/corefx/issues/6039
                if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                {
                    fileOptions |= FileOptions.Asynchronous;
                    allowAsyncFileRead = true;
                }

                await using var inputStream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, IODefaults.FileStreamBufferSize, fileOptions);

                var eofCount = 0;
                const int emptyReadLimit = 20;
                if (StartPosition > 0)
                {
                    inputStream.Position = StartPosition;
                }

                while (eofCount < emptyReadLimit || !AllowEndOfFile)
                {
                    int bytesRead;
                    if (allowAsyncFileRead)
                    {
                        bytesRead = await CopyToInternalAsync(inputStream, outputStream, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        bytesRead = await CopyToInternalAsyncWithSyncRead(inputStream, outputStream, cancellationToken).ConfigureAwait(false);
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
            }
            finally
            {
                if (_job != null)
                {
                    _transcodingJobHelper.OnTranscodeEndRequest(_job);
                }
            }
        }

        private async Task<int> CopyToInternalAsyncWithSyncRead(Stream source, Stream destination, CancellationToken cancellationToken)
        {
            var array = new byte[IODefaults.CopyToBufferSize];
            int bytesRead;
            int totalBytesRead = 0;

            while ((bytesRead = source.Read(array, 0, array.Length)) != 0)
            {
                var bytesToWrite = bytesRead;

                if (bytesToWrite > 0)
                {
                    await destination.WriteAsync(array, 0, Convert.ToInt32(bytesToWrite), cancellationToken).ConfigureAwait(false);

                    _bytesWritten += bytesRead;
                    totalBytesRead += bytesRead;

                    if (_job != null)
                    {
                        _job.BytesDownloaded = Math.Max(_job.BytesDownloaded ?? _bytesWritten, _bytesWritten);
                    }
                }
            }

            return totalBytesRead;
        }

        private async Task<int> CopyToInternalAsync(Stream source, Stream destination, CancellationToken cancellationToken)
        {
            var array = new byte[IODefaults.CopyToBufferSize];
            int bytesRead;
            int totalBytesRead = 0;

            while ((bytesRead = await source.ReadAsync(array, 0, array.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                var bytesToWrite = bytesRead;

                if (bytesToWrite > 0)
                {
                    await destination.WriteAsync(array, 0, Convert.ToInt32(bytesToWrite), cancellationToken).ConfigureAwait(false);

                    _bytesWritten += bytesRead;
                    totalBytesRead += bytesRead;

                    if (_job != null)
                    {
                        _job.BytesDownloaded = Math.Max(_job.BytesDownloaded ?? _bytesWritten, _bytesWritten);
                    }
                }
            }

            return totalBytesRead;
        }
    }
}
