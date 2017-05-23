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
using MediaBrowser.Model.System;

namespace Emby.Server.Implementations.LiveTv.TunerHosts.HdHomerun
{
    public class HdHomerunUdpStream : LiveStream, IDirectStreamProvider
    {
        private readonly ILogger _logger;
        private readonly IServerApplicationHost _appHost;
        private readonly ISocketFactory _socketFactory;

        private readonly CancellationTokenSource _liveStreamCancellationTokenSource = new CancellationTokenSource();
        private readonly TaskCompletionSource<bool> _liveStreamTaskCompletionSource = new TaskCompletionSource<bool>();
        private readonly IHdHomerunChannelCommands _channelCommands;
        private readonly int _numTuners;
        private readonly INetworkManager _networkManager;

        private readonly string _tempFilePath;

        public HdHomerunUdpStream(MediaSourceInfo mediaSource, string originalStreamId, IHdHomerunChannelCommands channelCommands, int numTuners, IFileSystem fileSystem, IHttpClient httpClient, ILogger logger, IServerApplicationPaths appPaths, IServerApplicationHost appHost, ISocketFactory socketFactory, INetworkManager networkManager, IEnvironmentInfo environment)
            : base(mediaSource, environment, fileSystem)
        {
            _logger = logger;
            _appHost = appHost;
            _socketFactory = socketFactory;
            _networkManager = networkManager;
            OriginalStreamId = originalStreamId;
            _channelCommands = channelCommands;
            _numTuners = numTuners;
            _tempFilePath = Path.Combine(appPaths.TranscodingTempPath, UniqueId + ".ts");
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
            //OpenedMediaSource.SupportsDirectPlay = false;
            //OpenedMediaSource.SupportsDirectStream = true;
            //OpenedMediaSource.SupportsTranscoding = true;

            await taskCompletionSource.Task.ConfigureAwait(false);

            //await Task.Delay(5000).ConfigureAwait(false);
        }

        public override Task Close()
        {
            _logger.Info("Closing HDHR UDP live stream");
            _liveStreamCancellationTokenSource.Cancel();

            return _liveStreamTaskCompletionSource.Task;
        }

        private Task StartStreaming(string remoteIp, int localPort, TaskCompletionSource<bool> openTaskCompletionSource, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
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
                                    FileSystem.CreateDirectory(FileSystem.GetDirectoryName(_tempFilePath));
                                    using (var fileStream = FileSystem.GetFileStream(_tempFilePath, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read, FileOpenOptions.Asynchronous))
                                    {
                                        await CopyTo(udpClient, fileStream, openTaskCompletionSource, cancellationToken).ConfigureAwait(false);
                                    }
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

                await DeleteTempFile(_tempFilePath).ConfigureAwait(false);
            });
        }

        private void Resolve(TaskCompletionSource<bool> openTaskCompletionSource)
        {
            Task.Run(() =>
           {
               openTaskCompletionSource.TrySetResult(true);
           });
        }

        public Task CopyToAsync(Stream stream, CancellationToken cancellationToken)
        {
            return CopyFileTo(_tempFilePath, false, stream, cancellationToken);
        }

        private static int RtpHeaderBytes = 12;
        private async Task CopyTo(ISocket udpClient, Stream outputStream, TaskCompletionSource<bool> openTaskCompletionSource, CancellationToken cancellationToken)
        {
            while (true)
            {
                var data = await udpClient.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                var bytesRead = data.ReceivedBytes - RtpHeaderBytes;

                await outputStream.WriteAsync(data.Buffer, RtpHeaderBytes, bytesRead, cancellationToken).ConfigureAwait(false);

                if (openTaskCompletionSource != null)
                {
                    Resolve(openTaskCompletionSource);
                    openTaskCompletionSource = null;
                }
            }
        }
    }

    // This handles the ReadAsync function only of a Stream object
    // This is used to wrap a UDP socket into a stream for MulticastStream which only uses ReadAsync
    public class UdpClientStream : Stream
    {
        private static int RtpHeaderBytes = 12;
        private static int PacketSize = 1316;
        private readonly ISocket _udpClient;
        bool disposed;

        public UdpClientStream(ISocket udpClient) : base()
        {
            _udpClient = udpClient;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (offset + count < 0)
                throw new ArgumentOutOfRangeException("offset + count must not be negative", "offset+count");

            if (offset + count > buffer.Length)
                throw new ArgumentException("offset + count must not be greater than the length of buffer", "offset+count");

            if (disposed)
                throw new ObjectDisposedException(typeof(UdpClientStream).ToString());

            // This will always receive a 1328 packet size (PacketSize + RtpHeaderSize)
            // The RTP header will be stripped so see how many reads we need to make to fill the buffer.
            var numReads = count / PacketSize;

            int totalBytesRead = 0;

            for (int i = 0; i < numReads; ++i)
            {
                var data = await _udpClient.ReceiveAsync(cancellationToken).ConfigureAwait(false);

                var bytesRead = data.ReceivedBytes - RtpHeaderBytes;

                // remove rtp header
                Buffer.BlockCopy(data.Buffer, RtpHeaderBytes, buffer, offset, bytesRead);
                offset += bytesRead;
                totalBytesRead += bytesRead;
            }
            return totalBytesRead;
        }

        protected override void Dispose(bool disposing)
        {
            disposed = true;
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}