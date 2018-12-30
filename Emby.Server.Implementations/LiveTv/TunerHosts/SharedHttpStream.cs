using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.System;
using System.Globalization;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.LiveTv;
using System.Collections.Generic;

namespace Emby.Server.Implementations.LiveTv.TunerHosts
{
    public class SharedHttpStream : LiveStream, IDirectStreamProvider
    {
        private readonly IHttpClient _httpClient;
        private readonly IServerApplicationHost _appHost;

        public SharedHttpStream(MediaSourceInfo mediaSource, TunerHostInfo tunerHostInfo, string originalStreamId, IFileSystem fileSystem, IHttpClient httpClient, ILogger logger, IServerApplicationPaths appPaths, IServerApplicationHost appHost)
            : base(mediaSource, tunerHostInfo, fileSystem, logger, appPaths)
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

            FileSystem.CreateDirectory(FileSystem.GetDirectoryName(TempFilePath));

            var typeName = GetType().Name;
            Logger.LogInformation("Opening " + typeName + " Live stream from {0}", url);

            var httpRequestOptions = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = CancellationToken.None,
                BufferContent = false,

                // Increase a little bit
                TimeoutMs = 30000,

                EnableHttpCompression = false,

                LogResponse = true,
                LogResponseHeaders = true
            };

            foreach (var header in mediaSource.RequiredHttpHeaders)
            {
                httpRequestOptions.RequestHeaders[header.Key] = header.Value;
            }

            var response = await _httpClient.SendAsync(httpRequestOptions, "GET").ConfigureAwait(false);

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

            StartStreaming(response, taskCompletionSource, LiveStreamCancellationTokenSource.Token);

            //OpenedMediaSource.Protocol = MediaProtocol.File;
            //OpenedMediaSource.Path = tempFile;
            //OpenedMediaSource.ReadAtNativeFramerate = true;

            MediaSource.Path = _appHost.GetLocalApiUrl("127.0.0.1") + "/LiveTv/LiveStreamFiles/" + UniqueId + "/stream.ts";
            MediaSource.Protocol = MediaProtocol.Http;

            //OpenedMediaSource.Path = TempFilePath;
            //OpenedMediaSource.Protocol = MediaProtocol.File;

            //OpenedMediaSource.Path = _tempFilePath;
            //OpenedMediaSource.Protocol = MediaProtocol.File;
            //OpenedMediaSource.SupportsDirectPlay = false;
            //OpenedMediaSource.SupportsDirectStream = true;
            //OpenedMediaSource.SupportsTranscoding = true;
            await taskCompletionSource.Task.ConfigureAwait(false);
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
                    using (var fileStream = FileSystem.GetFileStream(TempFilePath, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read, FileOpenOptions.None))
                    {
                        await ApplicationHost.StreamHelper.CopyToAsync(stream, fileStream, 81920, () => Resolve(openTaskCompletionSource), cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error copying live stream.");
                }
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
