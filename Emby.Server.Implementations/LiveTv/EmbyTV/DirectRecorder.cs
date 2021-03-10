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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IStreamHelper _streamHelper;

        public DirectRecorder(ILogger logger, IHttpClientFactory httpClientFactory, IStreamHelper streamHelper)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
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

            // use FileShare.None as this bypasses dotnet bug dotnet/runtime#42790 .
            using (var output = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                onStarted();

                _logger.LogInformation("Copying recording stream to file {0}", targetFile);

                // The media source is infinite so we need to handle stopping ourselves
                using var durationToken = new CancellationTokenSource(duration);
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, durationToken.Token);

                await directStreamProvider.CopyToAsync(output, cancellationTokenSource.Token).ConfigureAwait(false);
            }

            _logger.LogInformation("Recording completed to file {0}", targetFile);
        }

        private async Task RecordFromMediaSource(MediaSourceInfo mediaSource, string targetFile, TimeSpan duration, Action onStarted, CancellationToken cancellationToken)
        {
            using var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                .GetAsync(mediaSource.Path, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Opened recording stream from tuner provider");

            Directory.CreateDirectory(Path.GetDirectoryName(targetFile));

            // use FileShare.None as this bypasses dotnet bug dotnet/runtime#42790 .
            await using var output = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None);

            onStarted();

            _logger.LogInformation("Copying recording stream to file {0}", targetFile);

            // The media source if infinite so we need to handle stopping ourselves
            using var durationToken = new CancellationTokenSource(duration);
            using var linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, durationToken.Token);
            cancellationToken = linkedCancellationToken.Token;

            await _streamHelper.CopyUntilCancelled(
                await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                output,
                IODefaults.CopyToBufferSize,
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Recording completed to file {0}", targetFile);
        }
    }
}
