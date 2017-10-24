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

        public HdHomerunHttpStream(MediaSourceInfo mediaSource, string originalStreamId, IFileSystem fileSystem, IHttpClient httpClient, ILogger logger, IServerApplicationPaths appPaths, IServerApplicationHost appHost, IEnvironmentInfo environment)
            : base(mediaSource, environment, fileSystem, logger, appPaths)
        {
            _httpClient = httpClient;
            _appHost = appHost;
            OriginalStreamId = originalStreamId;
        }

        public override async Task Open(CancellationToken openCancellationToken)
        {
            LiveStreamCancellationTokenSource.Token.ThrowIfCancellationRequested();

            var mediaSource = OriginalMediaSource;

            var url = mediaSource.Path;

            FileSystem.CreateDirectory(FileSystem.GetDirectoryName(TempFilePath));

            Logger.Info("Opening HDHR Live stream from {0}", url);

            var response = await _httpClient.SendAsync(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = CancellationToken.None,
                BufferContent = false,

                // Increase a little bit
                TimeoutMs = 30000,

                EnableHttpCompression = false

            }, "GET").ConfigureAwait(false);

            Logger.Info("Opened HDHR stream from {0}", url);

            StartStreaming(response, LiveStreamCancellationTokenSource.Token);

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
        }

        public override void Close()
        {
            Logger.Info("Closing HDHR live stream");
            LiveStreamCancellationTokenSource.Cancel();
        }

        private Task StartStreaming(HttpResponseInfo response, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                try
                {
                    using (response)
                    {
                        using (var stream = response.Content)
                        {
                            Logger.Info("Beginning HdHomerunHttpStream stream to file");

                            FileSystem.CreateDirectory(FileSystem.GetDirectoryName(TempFilePath));
                            using (var fileStream = FileSystem.GetFileStream(TempFilePath, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read, FileOpenOptions.None))
                            {
                                StreamHelper.CopyTo(stream, fileStream, 81920, null, cancellationToken);
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

                await DeleteTempFile(TempFilePath).ConfigureAwait(false);
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
