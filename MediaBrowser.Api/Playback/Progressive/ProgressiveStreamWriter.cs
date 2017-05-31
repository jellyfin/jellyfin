using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Net;
using System.Collections.Generic;

using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.System;

namespace MediaBrowser.Api.Playback.Progressive
{
    public class ProgressiveFileCopier : IAsyncStreamWriter, IHasHeaders
    {
        private readonly IFileSystem _fileSystem;
        private readonly TranscodingJob _job;
        private readonly ILogger _logger;
        private readonly string _path;
        private readonly CancellationToken _cancellationToken;
        private readonly Dictionary<string, string> _outputHeaders;

        const int StreamCopyToBufferSize = 81920;

        private long _bytesWritten = 0;
        public long StartPosition { get; set; }
        public bool AllowEndOfFile = true;

        private readonly IDirectStreamProvider _directStreamProvider;
        private readonly IEnvironmentInfo _environment;

        public ProgressiveFileCopier(IFileSystem fileSystem, string path, Dictionary<string, string> outputHeaders, TranscodingJob job, ILogger logger, IEnvironmentInfo environment, CancellationToken cancellationToken)
        {
            _fileSystem = fileSystem;
            _path = path;
            _outputHeaders = outputHeaders;
            _job = job;
            _logger = logger;
            _cancellationToken = cancellationToken;
            _environment = environment;
        }

        public ProgressiveFileCopier(IDirectStreamProvider directStreamProvider, Dictionary<string, string> outputHeaders, TranscodingJob job, ILogger logger, IEnvironmentInfo environment, CancellationToken cancellationToken)
        {
            _directStreamProvider = directStreamProvider;
            _outputHeaders = outputHeaders;
            _job = job;
            _logger = logger;
            _cancellationToken = cancellationToken;
            _environment = environment;
        }

        public IDictionary<string, string> Headers
        {
            get
            {
                return _outputHeaders;
            }
        }

        private Stream GetInputStream(bool allowAsyncFileRead)
        {
            var fileOpenOptions = StartPosition > 0
                ? FileOpenOptions.RandomAccess
                : FileOpenOptions.SequentialScan;

            if (allowAsyncFileRead)
            {
                fileOpenOptions |= FileOpenOptions.Asynchronous;
            }

            return _fileSystem.GetFileStream(_path, FileOpenMode.Open, FileAccessMode.Read, FileShareMode.ReadWrite, fileOpenOptions);
        }

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

                var eofCount = 0;

                // use non-async filestream along with read due to https://github.com/dotnet/corefx/issues/6039
                var allowAsyncFileRead = _environment.OperatingSystem != OperatingSystem.Windows;

                using (var inputStream = GetInputStream(allowAsyncFileRead))
                {
                    if (StartPosition > 0)
                    {
                        inputStream.Position = StartPosition;
                    }

                    while (eofCount < 20 || !AllowEndOfFile)
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

                        //var position = fs.Position;
                        //_logger.Debug("Streamed {0} bytes to position {1} from file {2}", bytesRead, position, path);

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
            }
            finally
            {
                if (_job != null)
                {
                    ApiEntryPoint.Instance.OnTranscodeEndRequest(_job);
                }
            }
        }

        private async Task<int> CopyToInternalAsyncWithSyncRead(Stream source, Stream destination, CancellationToken cancellationToken)
        {
            var array = new byte[StreamCopyToBufferSize];
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
            var array = new byte[StreamCopyToBufferSize];
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
