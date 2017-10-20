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

namespace Emby.Server.Implementations.LiveTv.TunerHosts.HdHomerun
{
    public class HdHomerunHttpStream : LiveStream, IDirectStreamProvider
    {
        private readonly IHttpClient _httpClient;
        private readonly IServerApplicationHost _appHost;

        private readonly TaskCompletionSource<bool> _liveStreamTaskCompletionSource = new TaskCompletionSource<bool>();

        public HdHomerunHttpStream(MediaSourceInfo mediaSource, string originalStreamId, IFileSystem fileSystem, IHttpClient httpClient, ILogger logger, IServerApplicationPaths appPaths, IServerApplicationHost appHost, IEnvironmentInfo environment)
            : base(mediaSource, environment, fileSystem, logger, appPaths)
        {
            _httpClient = httpClient;
            _appHost = appHost;
            OriginalStreamId = originalStreamId;
        }

        protected override Task OpenInternal(CancellationToken openCancellationToken)
        {
            LiveStreamCancellationTokenSource.Token.ThrowIfCancellationRequested();

            var mediaSource = OriginalMediaSource;

            var url = mediaSource.Path;

            Logger.Info("Opening HDHR Live stream from {0}", url);

            var taskCompletionSource = new TaskCompletionSource<bool>();

            StartStreaming(url, taskCompletionSource, LiveStreamCancellationTokenSource.Token);

            //OpenedMediaSource.Protocol = MediaProtocol.File;
            //OpenedMediaSource.Path = tempFile;
            //OpenedMediaSource.ReadAtNativeFramerate = true;

            OpenedMediaSource.Path = _appHost.GetLocalApiUrl("127.0.0.1") + "/LiveTv/LiveStreamFiles/" + UniqueId + "/stream.ts";
            OpenedMediaSource.Protocol = MediaProtocol.Http;

            //OpenedMediaSource.Path = _tempFilePath;
            //OpenedMediaSource.Protocol = MediaProtocol.File;
            //OpenedMediaSource.SupportsDirectPlay = false;
            //OpenedMediaSource.SupportsDirectStream = true;
            //OpenedMediaSource.SupportsTranscoding = true;

            return taskCompletionSource.Task;

            //await Task.Delay(5000).ConfigureAwait(false);
        }

        public override async Task Close()
        {
            Logger.Info("Closing HDHR live stream");
            LiveStreamCancellationTokenSource.Cancel();

            await _liveStreamTaskCompletionSource.Task.ConfigureAwait(false);
            await DeleteTempFile(TempFilePath).ConfigureAwait(false);
        }

        private Task StartStreaming(string url, TaskCompletionSource<bool> openTaskCompletionSource, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                try
                {
                    using (var response = await _httpClient.SendAsync(new HttpRequestOptions
                    {
                        Url = url,
                        CancellationToken = cancellationToken,
                        BufferContent = false,

                        // Increase a little bit
                        TimeoutMs = 30000,

                        EnableHttpCompression = false

                    }, "GET").ConfigureAwait(false))
                    {
                        using (var stream = response.Content)
                        {
                            Logger.Info("Opened HDHR stream from {0}", url);

                            Logger.Info("Beginning multicastStream.CopyUntilCancelled");

                            FileSystem.CreateDirectory(FileSystem.GetDirectoryName(TempFilePath));
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

                _liveStreamTaskCompletionSource.TrySetResult(true);
            });
        }

        private void Resolve(TaskCompletionSource<bool> openTaskCompletionSource)
        {
            Task.Run(() =>
            {
                openTaskCompletionSource.TrySetResult(true);
            });
        }
    }
}
