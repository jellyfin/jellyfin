using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.LiveTv.TunerHosts.HdHomerun
{
    public class HdHomerunUdpStream : LiveStream, IDirectStreamProvider
    {
        private const int RtpHeaderBytes = 12;

        private readonly IServerApplicationHost _appHost;
        private readonly MediaBrowser.Model.Net.ISocketFactory _socketFactory;

        private readonly IHdHomerunChannelCommands _channelCommands;
        private readonly int _numTuners;
        private readonly INetworkManager _networkManager;

        public HdHomerunUdpStream(
            MediaSourceInfo mediaSource,
            TunerHostInfo tunerHostInfo,
            string originalStreamId,
            IHdHomerunChannelCommands channelCommands,
            int numTuners,
            IFileSystem fileSystem,
            ILogger logger,
            IServerApplicationPaths appPaths,
            IServerApplicationHost appHost,
            MediaBrowser.Model.Net.ISocketFactory socketFactory,
            INetworkManager networkManager,
            IStreamHelper streamHelper)
            : base(mediaSource, tunerHostInfo, fileSystem, logger, appPaths, streamHelper)
        {
            _appHost = appHost;
            _socketFactory = socketFactory;
            _networkManager = networkManager;
            OriginalStreamId = originalStreamId;
            _channelCommands = channelCommands;
            _numTuners = numTuners;
            EnableStreamSharing = true;
        }

        public override async Task Open(CancellationToken openCancellationToken)
        {
            LiveStreamCancellationTokenSource.Token.ThrowIfCancellationRequested();

            var mediaSource = OriginalMediaSource;

            var uri = new Uri(mediaSource.Path);
            var localPort = _networkManager.GetRandomUnusedUdpPort();

            Directory.CreateDirectory(Path.GetDirectoryName(TempFilePath));

            Logger.LogInformation("Opening HDHR UDP Live stream from {host}", uri.Host);

            var remoteAddress = IPAddress.Parse(uri.Host);
            IPAddress localAddress = null;
            using (var tcpClient = new TcpClient())
            {
                try
                {
                    await tcpClient.ConnectAsync(remoteAddress, HdHomerunManager.HdHomeRunPort).ConfigureAwait(false);
                    localAddress = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address;
                    tcpClient.Close();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Unable to determine local ip address for Legacy HDHomerun stream.");
                    return;
                }
            }

            var udpClient = _socketFactory.CreateUdpSocket(localPort);
            var hdHomerunManager = new HdHomerunManager();

            try
            {
                // send url to start streaming
                await hdHomerunManager.StartStreaming(remoteAddress, localAddress, localPort, _channelCommands, _numTuners, openCancellationToken).ConfigureAwait(false);
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

            await StartStreaming(
                udpClient,
                hdHomerunManager,
                remoteAddress,
                taskCompletionSource,
                LiveStreamCancellationTokenSource.Token).ConfigureAwait(false);

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

        private async Task CopyTo(MediaBrowser.Model.Net.ISocket udpClient, string file, TaskCompletionSource<bool> openTaskCompletionSource, CancellationToken cancellationToken)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(StreamDefaults.DefaultCopyToBufferSize);
            try
            {
                using (var source = _socketFactory.CreateNetworkStream(udpClient, false))
                using (var fileStream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    var currentCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token).Token;
                    int read;
                    var resolved = false;
                    while ((read = await source.ReadAsync(buffer, 0, buffer.Length, currentCancellationToken).ConfigureAwait(false)) != 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        currentCancellationToken = cancellationToken;

                        read -= RtpHeaderBytes;

                        if (read > 0)
                        {
                            await fileStream.WriteAsync(buffer, RtpHeaderBytes, read).ConfigureAwait(false);
                        }

                        if (!resolved)
                        {
                            resolved = true;
                            DateOpened = DateTime.UtcNow;
                            openTaskCompletionSource.TrySetResult(true);
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
