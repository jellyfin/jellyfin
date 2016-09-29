using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Server.Implementations.LiveTv.EmbyTV
{
    public class DirectRecorder : IRecorder
    {
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;

        public DirectRecorder(ILogger logger, IHttpClient httpClient, IFileSystem fileSystem)
        {
            _logger = logger;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
        }

        public string GetOutputPath(MediaSourceInfo mediaSource, string targetFile)
        {
            return targetFile;
        }

        public async Task Record(MediaSourceInfo mediaSource, string targetFile, TimeSpan duration, Action onStarted, CancellationToken cancellationToken)
        {
            var httpRequestOptions = new HttpRequestOptions()
            {
                Url = mediaSource.Path
            };

            httpRequestOptions.BufferContent = false;

            using (var response = await _httpClient.SendAsync(httpRequestOptions, "GET").ConfigureAwait(false))
            {
                _logger.Info("Opened recording stream from tuner provider");

                using (var output = _fileSystem.GetFileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    onStarted();

                    _logger.Info("Copying recording stream to file {0}", targetFile);

                    // The media source if infinite so we need to handle stopping ourselves
                    var durationToken = new CancellationTokenSource(duration);
                    cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, durationToken.Token).Token;

                    await CopyUntilCancelled(response.Content, output, cancellationToken).ConfigureAwait(false);
                }
            }

            _logger.Info("Recording completed to file {0}", targetFile);
        }

        private const int BufferSize = 81920;
        public static Task CopyUntilCancelled(Stream source, Stream target, CancellationToken cancellationToken)
        {
            return CopyUntilCancelled(source, target, null, cancellationToken);
        }
        public static async Task CopyUntilCancelled(Stream source, Stream target, Action onStarted, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await CopyToAsyncInternal(source, target, BufferSize, onStarted, cancellationToken).ConfigureAwait(false);

                onStarted = null;

                //var position = fs.Position;
                //_logger.Debug("Streamed {0} bytes to position {1} from file {2}", bytesRead, position, path);

                if (bytesRead == 0)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }
        }

        private static async Task<int> CopyToAsyncInternal(Stream source, Stream destination, Int32 bufferSize, Action onStarted, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            int totalBytesRead = 0;

            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);

                totalBytesRead += bytesRead;

                if (onStarted != null)
                {
                    onStarted();
                }
                onStarted = null;
            }

            return totalBytesRead;
        }
    }
}
