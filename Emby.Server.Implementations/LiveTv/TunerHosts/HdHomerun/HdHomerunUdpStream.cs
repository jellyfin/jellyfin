using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.IO;
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
using System.Globalization;
using MediaBrowser.Controller.IO;

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
        private bool _enableFileBuffer = false;
        private readonly MulticastStream _multicastStream;

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
            _multicastStream = new MulticastStream(_logger);
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
                                    if (_enableFileBuffer)
                                    {
                                        FileSystem.CreateDirectory(FileSystem.GetDirectoryName(_tempFilePath));
                                        using (var fileStream = FileSystem.GetFileStream(_tempFilePath, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read, FileOpenOptions.None))
                                        {
                                            CopyTo(udpClient, fileStream, openTaskCompletionSource, cancellationToken);
                                        }
                                    }
                                    else
                                    {
                                        await _multicastStream.CopyUntilCancelled(new UdpClientStream(udpClient), () => Resolve(openTaskCompletionSource), cancellationToken).ConfigureAwait(false);
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

        public async Task CopyToAsync(Stream outputStream, CancellationToken cancellationToken)
        {
            if (!_enableFileBuffer)
            {
                await _multicastStream.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
                return;
            }

            var path = _tempFilePath;

            long startPosition = -20000;
            if (startPosition < 0)
            {
                var length = FileSystem.GetFileInfo(path).Length;
                startPosition = Math.Max(length - startPosition, 0);
            }

            _logger.Info("Live stream starting position is {0} bytes", startPosition.ToString(CultureInfo.InvariantCulture));

            var allowAsync = Environment.OperatingSystem != MediaBrowser.Model.System.OperatingSystem.Windows;
            // use non-async filestream along with read due to https://github.com/dotnet/corefx/issues/6039

            using (var inputStream = GetInputStream(path, startPosition, allowAsync))
            {
                if (startPosition > 0)
                {
                    inputStream.Position = startPosition;
                }

                while (!cancellationToken.IsCancellationRequested)
                {
                    long bytesRead;

                    if (allowAsync)
                    {
                        bytesRead = await AsyncStreamCopier.CopyStream(inputStream, outputStream, 81920, 2, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        StreamHelper.CopyTo(inputStream, outputStream, 81920, cancellationToken);
                        bytesRead = 1;
                    }

                    //var position = fs.Position;
                    //_logger.Debug("Streamed {0} bytes to position {1} from file {2}", bytesRead, position, path);

                    if (bytesRead == 0)
                    {
                        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        private static int RtpHeaderBytes = 12;
        private void CopyTo(ISocket udpClient, Stream target, TaskCompletionSource<bool> openTaskCompletionSource, CancellationToken cancellationToken)
        {
            var source = _socketFactory.CreateNetworkStream(udpClient, false);
            var bufferSize = 81920;

            byte[] buffer = new byte[bufferSize];
            int read;
            var resolved = false;

            while ((read = source.Read(buffer, 0, buffer.Length)) != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                read -= RtpHeaderBytes;

                if (read > 0)
                {
                    target.Write(buffer, RtpHeaderBytes, read);
                }

                if (!resolved)
                {
                    resolved = true;
                    Resolve(openTaskCompletionSource);
                }
            }

            //var copier = new AsyncStreamCopier(source, target, 0, cancellationToken, false, bufferSize, bufferCount);
            //copier.IndividualReadOffset = RtpHeaderBytes;

            //var taskCompletion = new TaskCompletionSource<long>();

            //copier.TaskCompletionSource = taskCompletion;

            //var result = copier.BeginCopy(StreamCopyCallback, copier);

            //if (openTaskCompletionSource != null)
            //{
            //    Resolve(openTaskCompletionSource);
            //    openTaskCompletionSource = null;
            //}

            //if (result.CompletedSynchronously)
            //{
            //    StreamCopyCallback(result);
            //}

            //cancellationToken.Register(() => taskCompletion.TrySetCanceled());

            //return taskCompletion.Task;
        }

        private void StreamCopyCallback(IAsyncResult result)
        {
            var copier = (AsyncStreamCopier)result.AsyncState;
            var taskCompletion = copier.TaskCompletionSource;

            try
            {
                copier.EndCopy(result);
                taskCompletion.TrySetResult(0);
            }
            catch (Exception ex)
            {
                taskCompletion.TrySetException(ex);
            }
        }

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
                int numReads = count / PacketSize;
                int totalBytesRead = 0;
                byte[] receiveBuffer = new byte[81920];

                for (int i = 0; i < numReads; ++i)
                {
                    var data = await _udpClient.ReceiveAsync(receiveBuffer, 0, receiveBuffer.Length, cancellationToken).ConfigureAwait(false);

                    var bytesRead = data.ReceivedBytes - RtpHeaderBytes;

                    // remove rtp header
                    Buffer.BlockCopy(data.Buffer, RtpHeaderBytes, buffer, offset, bytesRead);
                    offset += bytesRead;
                    totalBytesRead += bytesRead;
                }
                return totalBytesRead;
            }

            public override int Read(byte[] buffer, int offset, int count)
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
                int numReads = count / PacketSize;
                int totalBytesRead = 0;
                byte[] receiveBuffer = new byte[81920];

                for (int i = 0; i < numReads; ++i)
                {
                    var receivedBytes = _udpClient.Receive(receiveBuffer, 0, receiveBuffer.Length);

                    var bytesRead = receivedBytes - RtpHeaderBytes;

                    // remove rtp header
                    Buffer.BlockCopy(receiveBuffer, RtpHeaderBytes, buffer, offset, bytesRead);
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
                    throw new NotImplementedException();
                }
            }

            public override bool CanSeek
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool CanWrite
            {
                get
                {
                    throw new NotImplementedException();
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
}