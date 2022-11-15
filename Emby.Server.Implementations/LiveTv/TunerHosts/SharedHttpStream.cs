#pragma warning disable CS1591

using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.LiveTv.TunerHosts
{
    public class SharedHttpStream : LiveStream, IDirectStreamProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServerApplicationHost _appHost;

        public SharedHttpStream(
            MediaSourceInfo mediaSource,
            TunerHostInfo tunerHostInfo,
            string originalStreamId,
            IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory,
            ILogger logger,
            IConfigurationManager configurationManager,
            IServerApplicationHost appHost,
            IStreamHelper streamHelper)
            : base(mediaSource, tunerHostInfo, fileSystem, logger, configurationManager, streamHelper)
        {
            _httpClientFactory = httpClientFactory;
            _appHost = appHost;
            OriginalStreamId = originalStreamId;
            EnableStreamSharing = true;
        }

        public override async Task Open(CancellationToken openCancellationToken)
        {
            LiveStreamCancellationTokenSource.Token.ThrowIfCancellationRequested();

            var mediaSource = OriginalMediaSource;

            var url = mediaSource.Path;

            Directory.CreateDirectory(Path.GetDirectoryName(TempFilePath) ?? throw new InvalidOperationException("Path can't be a root directory."));

            var typeName = GetType().Name;
            Logger.LogInformation("Opening {StreamType} Live stream from {Url}", typeName, url);

            // Response stream is disposed manually.
            var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
                .ConfigureAwait(false);

            var contentType = response.Content.Headers.ContentType?.ToString() ?? string.Empty;
            if (contentType.Contains("matroska", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("mp4", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("dash", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("mpegURL", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("text/", StringComparison.OrdinalIgnoreCase))
            {
                // Close the stream without any sharing features
                response.Dispose();
                return;
            }

            SetTempFilePath("ts");

            var taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            _ = StartStreaming(response, taskCompletionSource, LiveStreamCancellationTokenSource.Token);

            // OpenedMediaSource.Protocol = MediaProtocol.File;
            // OpenedMediaSource.Path = tempFile;
            // OpenedMediaSource.ReadAtNativeFramerate = true;

            MediaSource.Path = _appHost.GetApiUrlForLocalAccess() + "/LiveTv/LiveStreamFiles/" + UniqueId + "/stream.ts";
            MediaSource.Protocol = MediaProtocol.Http;

            // OpenedMediaSource.Path = TempFilePath;
            // OpenedMediaSource.Protocol = MediaProtocol.File;

            // OpenedMediaSource.Path = _tempFilePath;
            // OpenedMediaSource.Protocol = MediaProtocol.File;
            // OpenedMediaSource.SupportsDirectPlay = false;
            // OpenedMediaSource.SupportsDirectStream = true;
            // OpenedMediaSource.SupportsTranscoding = true;
            var res = await taskCompletionSource.Task.ConfigureAwait(false);
            if (!res)
            {
                Logger.LogWarning("Zero bytes copied from stream {StreamType} to {FilePath} but no exception raised", GetType().Name, TempFilePath);
                throw new EndOfStreamException(string.Format(CultureInfo.InvariantCulture, "Zero bytes copied from stream {0}", GetType().Name));
            }
        }

        private Task StartStreaming(HttpResponseMessage response, TaskCompletionSource<bool> openTaskCompletionSource, CancellationToken cancellationToken)
        {
            return Task.Run(
                async () =>
                {
                    try
                    {
                        Logger.LogInformation("Beginning {StreamType} stream to {FilePath}", GetType().Name, TempFilePath);
                        using var message = response;
                        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                        await using var fileStream = new FileStream(TempFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, IODefaults.FileStreamBufferSize, FileOptions.Asynchronous);
                        await StreamHelper.CopyToAsync(
                            stream,
                            fileStream,
                            IODefaults.CopyToBufferSize,
                            () => Resolve(openTaskCompletionSource),
                            cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException ex)
                    {
                        Logger.LogInformation("Copying of {StreamType} to {FilePath} was canceled", GetType().Name, TempFilePath);
                        openTaskCompletionSource.TrySetException(ex);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error copying live stream {StreamType} to {FilePath}", GetType().Name, TempFilePath);
                        openTaskCompletionSource.TrySetException(ex);
                    }

                    openTaskCompletionSource.TrySetResult(false);

                    EnableStreamSharing = false;
                    await DeleteTempFiles(TempFilePath).ConfigureAwait(false);
                },
                CancellationToken.None);
        }

        private void Resolve(TaskCompletionSource<bool> openTaskCompletionSource)
        {
            DateOpened = DateTime.UtcNow;
            openTaskCompletionSource.TrySetResult(true);
        }
    }
}
