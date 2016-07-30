using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Api.Playback.Progressive
{
    public class ProgressiveFileCopier
    {
        private readonly IFileSystem _fileSystem;
        private readonly TranscodingJob _job;
        private readonly ILogger _logger;

        // 256k
        private const int BufferSize = 81920;

        private long _bytesWritten = 0;

        public ProgressiveFileCopier(IFileSystem fileSystem, TranscodingJob job, ILogger logger)
        {
            _fileSystem = fileSystem;
            _job = job;
            _logger = logger;
        }

        public async Task StreamFile(string path, Stream outputStream, CancellationToken cancellationToken)
        {
            var eofCount = 0;

            using (var fs = _fileSystem.GetFileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, true))
            {
                while (eofCount < 15)
                {
                    var bytesRead = await CopyToAsyncInternal(fs, outputStream, BufferSize, cancellationToken).ConfigureAwait(false);

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

        private async Task<int> CopyToAsyncInternal(Stream source, Stream destination, Int32 bufferSize, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            int totalBytesRead = 0;

            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);

                _bytesWritten += bytesRead;
                totalBytesRead += bytesRead;

                if (_job != null)
                {
                    _job.BytesDownloaded = Math.Max(_job.BytesDownloaded ?? _bytesWritten, _bytesWritten);
                }
            }

            return totalBytesRead;
        }
    }
}
