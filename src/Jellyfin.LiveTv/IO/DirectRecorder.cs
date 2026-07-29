#pragma warning disable CS1591

using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv.IO
{
    public sealed class DirectRecorder : IRecorder
    {
        // Number of consecutive empty reads (~50ms apart) tolerated on the shared-stream path before
        // treating the source as ended so the caller can reconnect. Large enough to ride out brief
        // buffer-empty hiccups on a live stream.
        private const int EmptyReadLimit = 1000;

        // Direct HTTP reads return 0 only on a real connection close, so recover quickly (~2s) to let
        // the caller reconnect and resume appending instead of stalling on a dead socket.
        private const int HttpEmptyReadLimit = 40;

        // IPTV providers commonly redirect an HTTPS front URL to an HTTP CDN edge. HttpClient will not
        // auto-follow an HTTPS->HTTP downgrade, so we follow a bounded number of redirects ourselves.
        private const int MaxManualRedirects = 5;

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

        public Task Record(IDirectStreamProvider? directStreamProvider, MediaSourceInfo mediaSource, string targetFile, TimeSpan duration, Action onStarted, bool append, CancellationToken cancellationToken)
        {
            if (directStreamProvider is not null)
            {
                return RecordFromDirectStreamProvider(directStreamProvider, targetFile, duration, onStarted, append, cancellationToken);
            }

            return RecordFromMediaSource(mediaSource, targetFile, duration, onStarted, append, cancellationToken);
        }

        private async Task RecordFromDirectStreamProvider(IDirectStreamProvider directStreamProvider, string targetFile, TimeSpan duration, Action onStarted, bool append, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? throw new ArgumentException("Path can't be a root directory.", nameof(targetFile)));

            var output = new FileStream(
                targetFile,
                append ? FileMode.Append : FileMode.CreateNew,
                FileAccess.Write,
                FileShare.Read,
                IODefaults.FileStreamBufferSize,
                FileOptions.Asynchronous);

            await using (output.ConfigureAwait(false))
            {
                onStarted();

                _logger.LogInformation("Copying recording to file {FilePath}", targetFile);

                // The media source is infinite so we need to handle stopping ourselves
                using var durationToken = new CancellationTokenSource(duration);
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, durationToken.Token);
                var linkedCancellationToken = cancellationTokenSource.Token;
                var fileStream = new ProgressiveFileStream(directStreamProvider.GetStream());
                await using (fileStream.ConfigureAwait(false))
                {
                    await _streamHelper.CopyToAsync(
                        fileStream,
                        output,
                        IODefaults.CopyToBufferSize,
                        EmptyReadLimit,
                        linkedCancellationToken).ConfigureAwait(false);
                }
            }

            _logger.LogInformation("Recording completed: {FilePath}", targetFile);
        }

        private async Task RecordFromMediaSource(MediaSourceInfo mediaSource, string targetFile, TimeSpan duration, Action onStarted, bool append, CancellationToken cancellationToken)
        {
            using var response = await OpenStreamResponseAsync(mediaSource, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Opened recording stream from tuner provider");

            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? throw new ArgumentException("Path can't be a root directory.", nameof(targetFile)));

            var output = new FileStream(targetFile, append ? FileMode.Append : FileMode.CreateNew, FileAccess.Write, FileShare.Read, IODefaults.CopyToBufferSize, FileOptions.Asynchronous);
            await using (output.ConfigureAwait(false))
            {
                onStarted();

                _logger.LogInformation("Copying recording stream to file {0}", targetFile);

                // The media source is infinite so we need to handle stopping ourselves
                using var durationToken = new CancellationTokenSource(duration);
                using var linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, durationToken.Token);
                cancellationToken = linkedCancellationToken.Token;

                // Copy until the source stops delivering data (a dropped/ended connection) or the
                // scheduled duration elapses. Returning on end-of-stream (rather than spinning until
                // the duration is up) lets the caller reconnect and resume appending, so a flaky IPTV
                // source still yields a complete recording instead of a truncated one.
                await _streamHelper.CopyToAsync(
                    await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                    output,
                    IODefaults.CopyToBufferSize,
                    HttpEmptyReadLimit,
                    cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Recording completed to file {0}", targetFile);
            }
        }

        /// <summary>
        /// Opens the recording HTTP stream, applying the source's required headers and manually
        /// following redirects that <see cref="HttpClient"/> refuses to follow automatically
        /// (notably HTTPS-&gt;HTTP downgrades used by many IPTV CDNs). Without this the recorder would
        /// capture the empty redirect body and write a 0-byte recording.
        /// </summary>
        private async Task<HttpResponseMessage> OpenStreamResponseAsync(MediaSourceInfo mediaSource, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient(NamedClient.Default);
            var url = mediaSource.Path;

            for (var hop = 0; hop <= MaxManualRedirects; hop++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                // Apply provider-specific headers (User-Agent/Referer/etc.) so sources that require
                // them deliver data to the recorder just like they do to the player and transcoder.
                if (mediaSource.RequiredHttpHeaders is not null)
                {
                    foreach (var header in mediaSource.RequiredHttpHeaders)
                    {
                        if (!string.IsNullOrWhiteSpace(header.Key) && !string.IsNullOrWhiteSpace(header.Value))
                        {
                            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }
                }

                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

                var status = (int)response.StatusCode;
                if (status is >= 300 and < 400 && response.Headers.Location is not null)
                {
                    var location = response.Headers.Location;
                    var next = location.IsAbsoluteUri ? location : new Uri(new Uri(url), location);
                    _logger.LogInformation("Following recording stream redirect to {Scheme}://{Host}", next.Scheme, next.Host);
                    url = next.ToString();
                    response.Dispose();
                    continue;
                }

                return response;
            }

            throw new HttpRequestException(
                string.Format(CultureInfo.InvariantCulture, "Too many redirects opening recording stream from {0}", mediaSource.Path));
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
