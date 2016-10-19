using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Server.Implementations.LiveTv.EmbyTV;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Common.Extensions;

namespace MediaBrowser.Server.Implementations.LiveTv.TunerHosts.HdHomerun
{
    public class HdHomerunLiveStream : LiveStream, IDirectStreamProvider
    {
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;
        private readonly IServerApplicationPaths _appPaths;
        private readonly IServerApplicationHost _appHost;

        private readonly CancellationTokenSource _liveStreamCancellationTokenSource = new CancellationTokenSource();
        private readonly TaskCompletionSource<bool> _liveStreamTaskCompletionSource = new TaskCompletionSource<bool>();
        private readonly MulticastStream _multicastStream;


        public HdHomerunLiveStream(MediaSourceInfo mediaSource, string originalStreamId, IFileSystem fileSystem, IHttpClient httpClient, ILogger logger, IServerApplicationPaths appPaths, IServerApplicationHost appHost)
            : base(mediaSource)
        {
            _fileSystem = fileSystem;
            _httpClient = httpClient;
            _logger = logger;
            _appPaths = appPaths;
            _appHost = appHost;
            OriginalStreamId = originalStreamId;
            _multicastStream = new MulticastStream(_logger);
        }

        protected override async Task OpenInternal(CancellationToken openCancellationToken)
        {
            _liveStreamCancellationTokenSource.Token.ThrowIfCancellationRequested();

            var mediaSource = OriginalMediaSource;

            var url = mediaSource.Path;

            _logger.Info("Opening HDHR Live stream from {0}", url);

            var taskCompletionSource = new TaskCompletionSource<bool>();

            StartStreaming(url, taskCompletionSource, _liveStreamCancellationTokenSource.Token);

            //OpenedMediaSource.Protocol = MediaProtocol.File;
            //OpenedMediaSource.Path = tempFile;
            //OpenedMediaSource.ReadAtNativeFramerate = true;

            OpenedMediaSource.Path = _appHost.GetLocalApiUrl("127.0.0.1") + "/LiveTv/LiveStreamFiles/" + UniqueId + "/stream.ts";
            OpenedMediaSource.Protocol = MediaProtocol.Http;
            OpenedMediaSource.SupportsDirectPlay = false;
            OpenedMediaSource.SupportsDirectStream = true;
            OpenedMediaSource.SupportsTranscoding = true;

            await taskCompletionSource.Task.ConfigureAwait(false);

            //await Task.Delay(5000).ConfigureAwait(false);
        }

        public override Task Close()
        {
            _logger.Info("Closing HDHR live stream");
            _liveStreamCancellationTokenSource.Cancel();

            return _liveStreamTaskCompletionSource.Task;
        }

        private async Task StartStreaming(string url, TaskCompletionSource<bool> openTaskCompletionSource, CancellationToken cancellationToken)
        {
            await Task.Run(async () =>
            {
                var isFirstAttempt = true;

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        using (var response = await _httpClient.SendAsync(new HttpRequestOptions
                        {
                            Url = url,
                            CancellationToken = cancellationToken,
                            BufferContent = false

                        }, "GET").ConfigureAwait(false))
                        {
                            _logger.Info("Opened HDHR stream from {0}", url);

                            if (!cancellationToken.IsCancellationRequested)
                            {
                                _logger.Info("Beginning multicastStream.CopyUntilCancelled");

                                Action onStarted = null;
                                if (isFirstAttempt)
                                {
                                    onStarted = () => openTaskCompletionSource.TrySetResult(true);
                                }

                                await _multicastStream.CopyUntilCancelled(response.Content, onStarted, cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (isFirstAttempt)
                        {
                            _logger.ErrorException("Error opening live stream:", ex);
                            openTaskCompletionSource.TrySetException(ex);
                            break;
                        }

                        _logger.ErrorException("Error copying live stream, will reopen", ex);
                    }

                    isFirstAttempt = false;
                }

                _liveStreamTaskCompletionSource.TrySetResult(true);

            }).ConfigureAwait(false);
        }

        public Task CopyToAsync(Stream stream, CancellationToken cancellationToken)
        {
            return _multicastStream.CopyToAsync(stream);
        }
    }
}
