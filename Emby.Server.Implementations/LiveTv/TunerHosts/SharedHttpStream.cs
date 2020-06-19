#pragma warning disable CS1591

using System;
using System.Collections.Generic;
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
        private readonly IHttpClient _httpClient;
        private readonly IServerApplicationHost _appHost;

        public SharedHttpStream(
            MediaSourceInfo mediaSource,
            TunerHostInfo tunerHostInfo,
            string originalStreamId,
            IFileSystem fileSystem,
            IHttpClient httpClient,
            ILogger logger,
            IConfigurationManager configurationManager,
            IServerApplicationHost appHost,
            IStreamHelper streamHelper)
            : base(mediaSource, tunerHostInfo, fileSystem, logger, configurationManager, streamHelper)
        {
            _httpClient = httpClient;
            _appHost = appHost;
            OriginalStreamId = originalStreamId;
            EnableStreamSharing = true;
        }

        public override async Task Open(CancellationToken openCancellationToken)
        {
            LiveStreamCancellationTokenSource.Token.ThrowIfCancellationRequested();

            var mediaSource = OriginalMediaSource;

            var url = mediaSource.Path;

            Directory.CreateDirectory(Path.GetDirectoryName(TempFilePath));

            var typeName = GetType().Name;
            Logger.LogInformation("Opening " + typeName + " Live stream from {0}", url);

            var httpRequestOptions = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = CancellationToken.None,
                BufferContent = false,
                DecompressionMethod = CompressionMethods.None
            };

            foreach (var header in mediaSource.RequiredHttpHeaders)
            {
                httpRequestOptions.RequestHeaders[header.Key] = header.Value;
            }

            var response = await _httpClient.SendAsync(httpRequestOptions, HttpMethod.Get).ConfigureAwait(false);

            var extension = "ts";
            var requiresRemux = false;

            var contentType = response.ContentType ?? string.Empty;
            if (contentType.IndexOf("matroska", StringComparison.OrdinalIgnoreCase) != -1)
            {
                requiresRemux = true;
            }
            else if (contentType.IndexOf("mp4", StringComparison.OrdinalIgnoreCase) != -1 ||
               contentType.IndexOf("dash", StringComparison.OrdinalIgnoreCase) != -1 ||
               contentType.IndexOf("mpegURL", StringComparison.OrdinalIgnoreCase) != -1 ||
               contentType.IndexOf("text/", StringComparison.OrdinalIgnoreCase) != -1)
            {
                requiresRemux = true;
            }

            // Close the stream without any sharing features
            if (requiresRemux)
            {
                using (response)
                {
                    return;
                }
            }

            SetTempFilePath(extension);

            var taskCompletionSource = new TaskCompletionSource<bool>();

            var now = DateTime.UtcNow;

            _ = StartStreaming(response, taskCompletionSource, LiveStreamCancellationTokenSource.Token);

            //OpenedMediaSource.Protocol = MediaProtocol.File;
            //OpenedMediaSource.Path = tempFile;
            //OpenedMediaSource.ReadAtNativeFramerate = true;

            MediaSource.Path = _appHost.GetLoopbackHttpApiUrl() + "/LiveTv/LiveStreamFiles/" + UniqueId + "/stream.ts";
            MediaSource.Protocol = MediaProtocol.Http;

            //OpenedMediaSource.Path = TempFilePath;
            //OpenedMediaSource.Protocol = MediaProtocol.File;

            //OpenedMediaSource.Path = _tempFilePath;
            //OpenedMediaSource.Protocol = MediaProtocol.File;
            //OpenedMediaSource.SupportsDirectPlay = false;
            //OpenedMediaSource.SupportsDirectStream = true;
            //OpenedMediaSource.SupportsTranscoding = true;
            await taskCompletionSource.Task.ConfigureAwait(false);
            if (taskCompletionSource.Task.Exception != null)
            {
                // Error happened while opening the stream so raise the exception again to inform the caller
                throw taskCompletionSource.Task.Exception;
            }

            if (!taskCompletionSource.Task.Result)
            {
                Logger.LogWarning("Zero bytes copied from stream {0} to {1} but no exception raised", GetType().Name, TempFilePath);
                throw new EndOfStreamException(String.Format(CultureInfo.InvariantCulture, "Zero bytes copied from stream {0}", GetType().Name));
            }
        }

        private Task StartStreaming(HttpResponseInfo response, TaskCompletionSource<bool> openTaskCompletionSource, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                try
                {
                    Logger.LogInformation("Beginning {0} stream to {1}", GetType().Name, TempFilePath);
                    using (response)
                    using (var stream = response.Content)
                    using (var fileStream = new FileStream(TempFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        await StreamHelper.CopyToAsync(
                            stream,
                            fileStream,
                            IODefaults.CopyToBufferSize,
                            () => Resolve(openTaskCompletionSource),
                            cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException ex)
                {
                    Logger.LogInformation("Copying of {0} to {1} was canceled", GetType().Name, TempFilePath);
                    openTaskCompletionSource.TrySetException(ex);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error copying live stream {0} to {1}.", GetType().Name, TempFilePath);
                    openTaskCompletionSource.TrySetException(ex);
                }

                openTaskCompletionSource.TrySetResult(false);

                EnableStreamSharing = false;
                await DeleteTempFiles(new List<string> { TempFilePath }).ConfigureAwait(false);
            });
        }

        private void Resolve(TaskCompletionSource<bool> openTaskCompletionSource)
        {
            DateOpened = DateTime.UtcNow;
            openTaskCompletionSource.TrySetResult(true);
        }
    }
}
