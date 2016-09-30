using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Server.Implementations.LiveTv.EmbyTV;

namespace MediaBrowser.Server.Implementations.LiveTv.TunerHosts.HdHomerun
{
    public class HdHomerunLiveStream : LiveStream
    {
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;
        private readonly IServerApplicationPaths _appPaths;
        private readonly IServerApplicationHost _appHost;

        private readonly CancellationTokenSource _liveStreamCancellationTokenSource = new CancellationTokenSource();
        private readonly TaskCompletionSource<bool> _liveStreamTaskCompletionSource = new TaskCompletionSource<bool>();

        public HdHomerunLiveStream(MediaSourceInfo mediaSource, IFileSystem fileSystem, IHttpClient httpClient, ILogger logger, IServerApplicationPaths appPaths, IServerApplicationHost appHost)
            : base(mediaSource)
        {
            _fileSystem = fileSystem;
            _httpClient = httpClient;
            _logger = logger;
            _appPaths = appPaths;
            _appHost = appHost;
        }

        protected override async Task OpenInternal(CancellationToken openCancellationToken)
        {
            _liveStreamCancellationTokenSource.Token.ThrowIfCancellationRequested();

            var mediaSource = OriginalMediaSource;

            var url = mediaSource.Path;
            var tempFile = Path.Combine(_appPaths.TranscodingTempPath, Guid.NewGuid().ToString("N") + ".ts");
            Directory.CreateDirectory(Path.GetDirectoryName(tempFile));

            _logger.Info("Opening HDHR Live stream from {0} to {1}", url, tempFile);

            var output = _fileSystem.GetFileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.Read);

            var taskCompletionSource = new TaskCompletionSource<bool>();

            StartStreamingToTempFile(output, tempFile, url, taskCompletionSource, _liveStreamCancellationTokenSource.Token);

            //OpenedMediaSource.Protocol = MediaProtocol.File;
            //OpenedMediaSource.Path = tempFile;
            //OpenedMediaSource.ReadAtNativeFramerate = true;

            OpenedMediaSource.Path = _appHost.GetLocalApiUrl("localhost") + "/LiveTv/LiveStreamFiles/" + Path.GetFileNameWithoutExtension(tempFile) + "/stream.ts";
            OpenedMediaSource.Protocol = MediaProtocol.Http;

            await taskCompletionSource.Task.ConfigureAwait(false);

            //await Task.Delay(5000).ConfigureAwait(false);
        }

        public override Task Close()
        {
            _logger.Info("Closing HDHR live stream");
            _liveStreamCancellationTokenSource.Cancel();

            return _liveStreamTaskCompletionSource.Task;
        }

        private async Task StartStreamingToTempFile(Stream outputStream, string tempFilePath, string url, TaskCompletionSource<bool> openTaskCompletionSource, CancellationToken cancellationToken)
        {
            await Task.Run(async () =>
            {
                using (outputStream)
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
                                    _logger.Info("Beginning DirectRecorder.CopyUntilCancelled");

                                    Action onStarted = null;
                                    if (isFirstAttempt)
                                    {
                                        onStarted = () => openTaskCompletionSource.TrySetResult(true);
                                    }
                                    await DirectRecorder.CopyUntilCancelled(response.Content, outputStream, onStarted, cancellationToken).ConfigureAwait(false);
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
                }

                _liveStreamTaskCompletionSource.TrySetResult(true);

                DeleteTempFile(tempFilePath);

            }).ConfigureAwait(false);
        }

        private async void DeleteTempFile(string path)
        {
            for (var i = 0; i < 10; i++)
            {
                try
                {
                    File.Delete(path);
                    return;
                }
                catch (FileNotFoundException)
                {
                    return;
                }
                catch (DirectoryNotFoundException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error deleting temp file {0}", ex, path);
                }

                await Task.Delay(1000).ConfigureAwait(false);
            }
        }
    }
}
