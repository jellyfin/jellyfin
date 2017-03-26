using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Net;

namespace Emby.Server.Implementations.LiveTv.TunerHosts.HdHomerun
{
    public class HdHomerunUdpStream : LiveStream, IDirectStreamProvider
    {
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;
        private readonly IServerApplicationPaths _appPaths;
        private readonly IServerApplicationHost _appHost;
        private readonly ISocketFactory _socketFactory;

        private readonly CancellationTokenSource _liveStreamCancellationTokenSource = new CancellationTokenSource();
        private readonly TaskCompletionSource<bool> _liveStreamTaskCompletionSource = new TaskCompletionSource<bool>();
        private readonly MulticastStream _multicastStream;
        private readonly IHdHomerunChannelCommands _channelCommands;
        private readonly int _numTuners;
        private readonly INetworkManager _networkManager;

        public HdHomerunUdpStream(MediaSourceInfo mediaSource, string originalStreamId, IHdHomerunChannelCommands channelCommands, int numTuners, IFileSystem fileSystem, IHttpClient httpClient, ILogger logger, IServerApplicationPaths appPaths, IServerApplicationHost appHost, ISocketFactory socketFactory, INetworkManager networkManager)
            : base(mediaSource)
        {
            _fileSystem = fileSystem;
            _httpClient = httpClient;
            _logger = logger;
            _appPaths = appPaths;
            _appHost = appHost;
            _socketFactory = socketFactory;
            _networkManager = networkManager;
            OriginalStreamId = originalStreamId;
            _multicastStream = new MulticastStream(_logger);
            _channelCommands = channelCommands;
            _numTuners = numTuners;
        }

        protected override async Task OpenInternal(CancellationToken openCancellationToken)
        {
            _liveStreamCancellationTokenSource.Token.ThrowIfCancellationRequested();

            var mediaSource = OriginalMediaSource;

            var uri = new Uri(mediaSource.Path);
            var localPort = _networkManager.GetRandomUnusedUdpPort();

            _logger.Info("Opening HDHR UDP Live stream from {0}", uri.Host);

            var taskCompletionSource = new TaskCompletionSource<bool>();

            StartStreaming(uri.Host, localPort, taskCompletionSource, _liveStreamCancellationTokenSource.Token);

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
            _logger.Info("Closing HDHR UDP live stream");
            _liveStreamCancellationTokenSource.Cancel();

            return _liveStreamTaskCompletionSource.Task;
        }

        private async Task StartStreaming(string remoteIp, int localPort, TaskCompletionSource<bool> openTaskCompletionSource, CancellationToken cancellationToken)
        {
            await Task.Run(async () =>
            {
                var isFirstAttempt = true;
                using (var udpClient = _socketFactory.CreateUdpSocket(localPort))
                {
                    using (var hdHomerunManager = new HdHomerunManager(_socketFactory))
                    {
                        var remoteAddress = _networkManager.ParseIpAddress(remoteIp);
                        IpAddressInfo localAddress = null;
                        using (var tcpSocket = _socketFactory.CreateSocket(remoteAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp, false))
                        {
                            try
                            {
                                tcpSocket.Connect(new IpEndPointInfo(remoteAddress, HdHomerunManager.HdHomeRunPort));
                                localAddress = tcpSocket.LocalEndPoint.IpAddress;
                                tcpSocket.Close();
                            }
                            catch (Exception)
                            {
                                _logger.Error("Unable to determine local ip address for Legacy HDHomerun stream.");
                                return;
                            }
                        }

                        while (!cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                // send url to start streaming
                                await hdHomerunManager.StartStreaming(remoteAddress, localAddress, localPort, _channelCommands, _numTuners, cancellationToken).ConfigureAwait(false);

                                _logger.Info("Opened HDHR UDP stream from {0}", remoteAddress);

                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    Action onStarted = null;
                                    if (isFirstAttempt)
                                    {
                                        onStarted = () => openTaskCompletionSource.TrySetResult(true);
                                    }

                                    await _multicastStream.CopyUntilCancelled(udpClient, onStarted, cancellationToken).ConfigureAwait(false);
                                }
                            }
                            catch (OperationCanceledException ex)
                            {
                                _logger.Info("HDHR UDP stream cancelled or timed out from {0}", remoteAddress);
                                openTaskCompletionSource.TrySetException(ex);
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

                        await hdHomerunManager.StopStreaming().ConfigureAwait(false);
                        _liveStreamTaskCompletionSource.TrySetResult(true);
                    }
                }

            }).ConfigureAwait(false);
        }

        public Task CopyToAsync(Stream stream, CancellationToken cancellationToken)
        {
            return _multicastStream.CopyToAsync(stream);
        }
    }
}