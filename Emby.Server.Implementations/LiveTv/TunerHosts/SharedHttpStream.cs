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
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.System;
using System.Globalization;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.LiveTv;

namespace Emby.Server.Implementations.LiveTv.TunerHosts
{
    public class SharedHttpStream : LiveStream, IDirectStreamProvider
    {
        private readonly IHttpClient _httpClient;
        private readonly IServerApplicationHost _appHost;

        public SharedHttpStream(MediaSourceInfo mediaSource, TunerHostInfo tunerHostInfo, string originalStreamId, IFileSystem fileSystem, IHttpClient httpClient, ILogger logger, IServerApplicationPaths appPaths, IServerApplicationHost appHost, IEnvironmentInfo environment)
            : base(mediaSource, tunerHostInfo, environment, fileSystem, logger, appPaths)
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
            Logger.Info("Opening " + typeName + " Live stream from {0}", url);

            var response = await _httpClient.SendAsync(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = CancellationToken.None,
                BufferContent = false,

                // Increase a little bit
                TimeoutMs = 30000,

                EnableHttpCompression = false,

                LogResponse = true,
                LogResponseHeaders = true

            }, "GET").ConfigureAwait(false);

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

            OpenedMediaSource.Path = _appHost.GetLocalApiUrl("127.0.0.1") + "/LiveTv/LiveStreamFiles/" + UniqueId + "/stream.ts";
            OpenedMediaSource.Protocol = MediaProtocol.Http;

            //OpenedMediaSource.Path = TempFilePath;
            //OpenedMediaSource.Protocol = MediaProtocol.File;

            //OpenedMediaSource.Path = _tempFilePath;
            //OpenedMediaSource.Protocol = MediaProtocol.File;
            //OpenedMediaSource.SupportsDirectPlay = false;
            //OpenedMediaSource.SupportsDirectStream = true;
            //OpenedMediaSource.SupportsTranscoding = true;
            await taskCompletionSource.Task.ConfigureAwait(false);

            if (OpenedMediaSource.SupportsProbing)
            {
                var elapsed = (DateTime.UtcNow - now).TotalMilliseconds;

                var delay = Convert.ToInt32(3000 - elapsed);

                if (delay > 0)
                {
                    Logger.Info("Delaying shared stream by {0}ms to allow the buffer to build.", delay);

                    await Task.Delay(delay).ConfigureAwait(false);
                }
            }
        }

        protected override void CloseInternal()
        {
            LiveStreamCancellationTokenSource.Cancel();
        }

        private Task StartStreaming(HttpResponseInfo response, TaskCompletionSource<bool> openTaskCompletionSource, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                try
                {
                    using (response)
                    {
                        using (var stream = response.Content)
                        {
                            Logger.Info("Beginning {0} stream to {1}", GetType().Name, TempFilePath);

                            using (var fileStream = FileSystem.GetFileStream(TempFilePath, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read, FileOpenOptions.None))
                            {
                                StreamHelper.CopyTo(stream, fileStream, 81920, () => Resolve(openTaskCompletionSource), cancellationToken);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error copying live stream.", ex);
                }
                EnableStreamSharing = false;
                await DeleteTempFile(TempFilePath).ConfigureAwait(false);
            });
        }

        private void Resolve(TaskCompletionSource<bool> openTaskCompletionSource)
        {
            openTaskCompletionSource.TrySetResult(true);
        }
    }
}
