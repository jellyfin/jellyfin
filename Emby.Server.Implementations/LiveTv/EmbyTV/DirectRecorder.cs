#pragma warning disable CS1591

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.LiveTv.EmbyTV
{
    public class DirectRecorder : IRecorder
    {
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IStreamHelper _streamHelper;

        public DirectRecorder(ILogger logger, IHttpClient httpClient, IStreamHelper streamHelper)
        {
            _logger = logger;
            _httpClient = httpClient;
            _streamHelper = streamHelper;
        }

        public string GetOutputPath(MediaSourceInfo mediaSource, string targetFile)
        {
            return targetFile;
        }

        public Task Record(IDirectStreamProvider directStreamProvider, MediaSourceInfo mediaSource, string targetFile, TimeSpan duration, Action onStarted, CancellationToken cancellationToken)
        {
            if (directStreamProvider != null)
            {
                return RecordFromDirectStreamProvider(directStreamProvider, targetFile, duration, onStarted, cancellationToken);
            }

            return RecordFromMediaSource(mediaSource, targetFile, duration, onStarted, cancellationToken);
        }

        private async Task RecordFromDirectStreamProvider(IDirectStreamProvider directStreamProvider, string targetFile, TimeSpan duration, Action onStarted, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile));

            using (var output = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                onStarted();

                _logger.LogInformation("Copying recording stream to file {0}", targetFile);

                // The media source is infinite so we need to handle stopping ourselves
                var durationToken = new CancellationTokenSource(duration);
                cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, durationToken.Token).Token;

                await directStreamProvider.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Recording completed to file {0}", targetFile);
        }

        private async Task RecordFromMediaSource(MediaSourceInfo mediaSource, string targetFile, TimeSpan duration, Action onStarted, CancellationToken cancellationToken)
        {
            var httpRequestOptions = new HttpRequestOptions
            {
                Url = mediaSource.Path,
                BufferContent = false,

                // Some remote urls will expect a user agent to be supplied
                UserAgent = "Emby/3.0",

                // Shouldn't matter but may cause issues
                DecompressionMethod = CompressionMethods.None
            };

            using (var response = await _httpClient.SendAsync(httpRequestOptions, HttpMethod.Get).ConfigureAwait(false))
            {
                _logger.LogInformation("Opened recording stream from tuner provider");

                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));

                using (var output = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    onStarted();

                    _logger.LogInformation("Copying recording stream to file {0}", targetFile);

                    // The media source if infinite so we need to handle stopping ourselves
                    var durationToken = new CancellationTokenSource(duration);
                    cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, durationToken.Token).Token;

                    await _streamHelper.CopyUntilCancelled(response.Content, output, 81920, cancellationToken).ConfigureAwait(false);
                }
            }

            _logger.LogInformation("Recording completed to file {0}", targetFile);
        }
    }
}
