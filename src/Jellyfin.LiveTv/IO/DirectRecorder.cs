#pragma warning disable CS1591

using System;
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
using Polly;

namespace Jellyfin.LiveTv.IO
{
    public sealed class DirectRecorder : IRecorder
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

        public Task Record(IDirectStreamProvider? directStreamProvider, MediaSourceInfo mediaSource, string targetFile, TimeSpan duration, Action onStarted, CancellationToken cancellationToken)
        {
            if (directStreamProvider is not null)
            {
                return RecordFromDirectStreamProvider(directStreamProvider, targetFile, duration, onStarted, cancellationToken);
            }

            return RecordFromMediaSource(mediaSource, targetFile, duration, onStarted, cancellationToken);
        }

        private async Task RecordFromDirectStreamProvider(IDirectStreamProvider directStreamProvider, string targetFile, TimeSpan duration, Action onStarted, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? throw new ArgumentException("Path can't be a root directory.", nameof(targetFile)));

            var output = new FileStream(
                targetFile,
                FileMode.CreateNew,
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
                        1000,
                        linkedCancellationToken).ConfigureAwait(false);
                }
            }

            _logger.LogInformation("Recording completed: {FilePath}", targetFile);
        }

        private async Task RecordFromMediaSource(MediaSourceInfo mediaSource, string targetFile, TimeSpan duration, Action onStarted, CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);

            var retryPolicy = Policy.Handle<Exception>()
                .WaitAndRetryAsync(
                    5,
                    retryAttempt => TimeSpan.FromMilliseconds(
                        retryAttempt == 1 ? 100 : (int)Math.Pow(2, retryAttempt - 1) * 1000),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        if (exception is OperationCanceledException)
                        {
                            _logger.LogInformation("Recording completed to file {TargetFile}", targetFile);
                            return;
                        }

                        _logger.LogInformation(
                            "Stream error: {Message}. Retrying in {TimeSpan}ms...",
                            exception.Message,
                            timeSpan.TotalMilliseconds);
                    });

            // The media source is infinite so we need to handle stopping ourselves
            using var durationToken = new CancellationTokenSource(duration);
            using var linkedCancellationToken =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, durationToken.Token);
            cancellationToken = linkedCancellationToken.Token;

            try
            {
                // Execute only the HTTP request in the retry policy
                using var response = await retryPolicy.ExecuteAsync(
                async ct =>
                {
                    var resp = await httpClient.GetAsync(
                        mediaSource.Path,
                        HttpCompletionOption.ResponseHeadersRead,
                        ct).ConfigureAwait(false);

                    try
                    {
                        resp.EnsureSuccessStatusCode();
                        return resp;
                    }
                    catch
                    {
                        resp.Dispose(); // dispose failed response to avoid socket leaks
                        throw;
                    }
                },
                cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Opened recording stream from tuner provider");

                Directory.CreateDirectory(Path.GetDirectoryName(targetFile)
                    ?? throw new ArgumentException("Path can't be a root directory.", nameof(targetFile)));

                var output = new FileStream(targetFile, FileMode.Append, FileAccess.Write, FileShare.Read, IODefaults.CopyToBufferSize, FileOptions.Asynchronous);

                onStarted();

                _logger.LogInformation("Copying recording stream to file {TargetFile}", targetFile);

                await _streamHelper.CopyUntilCancelled(
                    await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                    output,
                    IODefaults.CopyToBufferSize,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Ignore and let execution continue, because this is expected result of CopyUntilCancelled.
                _logger.LogInformation("Recording completed to file {TargetFile}", targetFile);
            }
            catch (Exception ex)
            {
                _logger.LogError("Stream failed permanently: {Message}", ex.Message);
                throw;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
