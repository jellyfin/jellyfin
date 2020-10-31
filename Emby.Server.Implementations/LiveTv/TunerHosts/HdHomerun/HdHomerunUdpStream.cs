#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Networking.Configuration;
using Jellyfin.Networking.Manager;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Udp;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
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

        private readonly string _portRange;
        private readonly IServerApplicationHost _appHost;
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
            IConfigurationManager configurationManager,
            IServerApplicationHost appHost,
            INetworkManager networkManager,
            IStreamHelper streamHelper)
            : base(mediaSource, tunerHostInfo, fileSystem, logger, configurationManager, streamHelper)
        {
            _portRange = configurationManager.GetNetworkConfiguration().UDPPortRange;
            _appHost = appHost;
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
            var localPort = UdpHelper.GetPort(_portRange);
            Logger.LogDebug("Using udp port {0}", localPort);

            Directory.CreateDirectory(Path.GetDirectoryName(TempFilePath));

            Logger.LogInformation("Opening HDHR UDP Live stream from {host}", uri.Host);

            var remote = IPHost.Parse(uri.Host, AddressFamily.InterNetwork);

            Logger.LogDebug("Parsed host from {0} as {1}", uri.Host, remote);

            IPAddress localAddress = null;
            using (var tcpClient = new TcpClient())
            {
                try
                {
                    await tcpClient.ConnectAsync(remote.Address, HdHomerunManager.HdHomeRunPort).ConfigureAwait(false);
                    localAddress = ((IPEndPoint)tcpClient.Client.LocalEndPoint).Address;
                    tcpClient.Close();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Unable to determine local ip address for Legacy HDHomerun stream.");
                    return;
                }
            }

            var udpClient = new UdpClient(localPort, _networkManager.IsIP6Enabled ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork);
            var hdHomerunManager = new HdHomerunManager();

            try
            {
                Logger.LogDebug("Starting streaming to {0}/{1} -> {1}", localAddress, localPort, remote.Address);
                // send url to start streaming
                await hdHomerunManager.StartStreaming(
                    remote.Address,
                    localAddress,
                    localPort,
                    _channelCommands,
                    _numTuners,
                    openCancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
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
                remote.Address,
                taskCompletionSource,
                LiveStreamCancellationTokenSource.Token).ConfigureAwait(false);

            // OpenedMediaSource.Protocol = MediaProtocol.File;
            // OpenedMediaSource.Path = tempFile;
            // OpenedMediaSource.ReadAtNativeFramerate = true;

            MediaSource.Path = _appHost.GetLoopbackHttpApiUrl() + "/LiveTv/LiveStreamFiles/" + UniqueId + "/stream.ts";
            MediaSource.Protocol = MediaProtocol.Http;
            // OpenedMediaSource.SupportsDirectPlay = false;
            // OpenedMediaSource.SupportsDirectStream = true;
            // OpenedMediaSource.SupportsTranscoding = true;

            Logger.LogDebug("Mediasource path {0}", MediaSource.Path);

            // await Task.Delay(5000).ConfigureAwait(false);
            await taskCompletionSource.Task.ConfigureAwait(false);
        }

        private Task StartStreaming(UdpClient udpClient, HdHomerunManager hdHomerunManager, IPAddress remoteAddress, TaskCompletionSource<bool> openTaskCompletionSource, CancellationToken cancellationToken)
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

        private async Task CopyTo(UdpClient udpClient, string file, TaskCompletionSource<bool> openTaskCompletionSource, CancellationToken cancellationToken)
        {
            var resolved = false;

            using (var fileStream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using (var timeOutSource = new CancellationTokenSource())
                    using (var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken,
                        timeOutSource.Token))
                    {
                        var resTask = udpClient.ReceiveAsync();
                        if (await Task.WhenAny(resTask, Task.Delay(30000, linkedSource.Token)).ConfigureAwait(false) != resTask)
                        {
                            resTask.Dispose();
                            break;
                        }

                        // We don't want all these delay tasks to keep running
                        timeOutSource.Cancel();
                        var res = await resTask.ConfigureAwait(false);
                        var buffer = res.Buffer;

                        var read = buffer.Length - RtpHeaderBytes;

                        if (read > 0)
                        {
                            fileStream.Write(buffer, RtpHeaderBytes, read);
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
        }
    }
}
