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

                    var durationToken = new CancellationTokenSource(duration);
                    var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, durationToken.Token).Token;

                    await response.Content.CopyToAsync(output, StreamDefaults.DefaultCopyToBufferSize, linkedToken).ConfigureAwait(false);
                }
            }

            _logger.Info("Recording completed to file {0}", targetFile);
        }
    }
}
