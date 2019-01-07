using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.System;
using MediaBrowser.Model.LiveTv;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;

namespace Emby.Server.Implementations.LiveTv.TunerHosts.HdHomerun
{
    public class HdHomerunUdpStream : LiveStream, IDirectStreamProvider
    {
        private readonly IServerApplicationHost _appHost;
        private readonly MediaBrowser.Model.Net.ISocketFactory _socketFactory;

        private readonly IHdHomerunChannelCommands _channelCommands;
        private readonly int _numTuners;
        private readonly INetworkManager _networkManager;

        public HdHomerunUdpStream(MediaSourceInfo mediaSource, TunerHostInfo tunerHostInfo, string originalStreamId, IHdHomerunChannelCommands channelCommands, int numTuners, IFileSystem fileSystem, IHttpClient httpClient, ILogger logger, IServerApplicationPaths appPaths, IServerApplicationHost appHost, MediaBrowser.Model.Net.ISocketFactory socketFactory, INetworkManager networkManager)
            : base(mediaSource, tunerHostInfo, fileSystem, logger, appPaths)
        {
            _appHost = appHost;
            _socketFactory = socketFactory;
            _networkManager = networkManager;
            OriginalStreamId = originalStreamId;
            _channelCommands = channelCommands;
            _numTuners = numTuners;
            EnableStreamSharing = true;
        }

        private Socket CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            var socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);

            return socket;
        }

        public override async Task Open(CancellationToken openCancellationToken)
        {
            LiveStreamCancellationTokenSource.Token.ThrowIfCancellationRequested();

            var mediaSource = OriginalMediaSource;

            var uri = new Uri(mediaSource.Path);
            var localPort = _networkManager.GetRandomUnusedUdpPort();

            FileSystem.CreateDirectory(FileSystem.GetDirectoryName(TempFilePath));

            Logger.LogInformation("Opening HDHR UDP Live stream from {host}", uri.Host);

            var remoteAddress = IPAddress.Parse(uri.Host);
            var embyRemoteAddress = _networkManager.ParseIpAddress(uri.Host);
            IPAddress localAddress = null;
            using (var tcpSocket = CreateSocket(remoteAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    tcpSocket.Connect(new IPEndPoint(remoteAddress, HdHomerunManager.HdHomeRunPort));
                    localAddress = ((IPEndPoint)tcpSocket.LocalEndPoint).Address;
                    tcpSocket.Close();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Unable to determine local ip address for Legacy HDHomerun stream.");
                    return;
                }
            }

            var udpClient = _socketFactory.CreateUdpSocket(localPort);
            var hdHomerunManager = new HdHomerunManager(_socketFactory, Logger);

            try
            {
                // send url to start streaming
                await hdHomerunManager.StartStreaming(embyRemoteAddress, localAddress, localPort, _channelCommands, _numTuners, openCancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                using (udpClient)
                using (hdHomerunManager)
                {
                    if (!(ex is OperationCanceledException))
                    {
                        Logger.LogError(ex, "Error opening live stream:");
                    }
                    throw;
                }
            }

            var taskCompletionSource = new TaskCompletionSource<bool>();

            await StartStreaming(udpClient, hdHomerunManager, remoteAddress, taskCompletionSource, LiveStreamCancellationTokenSource.Token);

            //OpenedMediaSource.Protocol = MediaProtocol.File;
            //OpenedMediaSource.Path = tempFile;
            //OpenedMediaSource.ReadAtNativeFramerate = true;

            MediaSource.Path = _appHost.GetLocalApiUrl("127.0.0.1") + "/LiveTv/LiveStreamFiles/" + UniqueId + "/stream.ts";
            MediaSource.Protocol = MediaProtocol.Http;
            //OpenedMediaSource.SupportsDirectPlay = false;
            //OpenedMediaSource.SupportsDirectStream = true;
            //OpenedMediaSource.SupportsTranscoding = true;

            //await Task.Delay(5000).ConfigureAwait(false);
            await taskCompletionSource.Task.ConfigureAwait(false);
        }

        private Task StartStreaming(MediaBrowser.Model.Net.ISocket udpClient, HdHomerunManager hdHomerunManager, IPAddress remoteAddress, TaskCompletionSource<bool> openTaskCompletionSource, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                using (udpClient)
                using (hdHomerunManager)
                {
                    try
                    {
                        await CopyTo(udpClient, TempFilePath, openTaskCompletionSource, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException ex)
                    {
                        Logger.LogInformation("HDHR UDP stream cancelled or timed out from {0}", remoteAddress);
                        openTaskCompletionSource.TrySetException(ex);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error opening live stream:");
                        openTaskCompletionSource.TrySetException(ex);
                    }

                    EnableStreamSharing = false;
                }

                await DeleteTempFiles(new List<string> { TempFilePath }).ConfigureAwait(false);
            });
        }

        private void Resolve(TaskCompletionSource<bool> openTaskCompletionSource)
        {
            Task.Run(() =>
            {
                openTaskCompletionSource.TrySetResult(true);
            });
        }

        private static int RtpHeaderBytes = 12;
        private async Task CopyTo(MediaBrowser.Model.Net.ISocket udpClient, string file, TaskCompletionSource<bool> openTaskCompletionSource, CancellationToken cancellationToken)
        {
            var bufferSize = 81920;

            byte[] buffer = new byte[bufferSize];
            int read;
            var resolved = false;

            using (var source = _socketFactory.CreateNetworkStream(udpClient, false))
            using (var fileStream = FileSystem.GetFileStream(file, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read, FileOpenOptions.None))
            {
                var currentCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token).Token;

                while ((read = await source.ReadAsync(buffer, 0, buffer.Length, currentCancellationToken).ConfigureAwait(false)) != 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    currentCancellationToken = cancellationToken;

                    read -= RtpHeaderBytes;

                    if (read > 0)
                    {
                        fileStream.Write(buffer, RtpHeaderBytes, read);
                    }

                    if (!resolved)
                    {
                        resolved = true;
                        DateOpened = DateTime.UtcNow;
                        Resolve(openTaskCompletionSource);
                    }
                }
            }
        }

        public class UdpClientStream : Stream
        {
            private static int RtpHeaderBytes = 12;
            private static int PacketSize = 1316;
            private readonly MediaBrowser.Model.Net.ISocket _udpClient;
            bool disposed;

            public UdpClientStream(MediaBrowser.Model.Net.ISocket udpClient) : base()
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
