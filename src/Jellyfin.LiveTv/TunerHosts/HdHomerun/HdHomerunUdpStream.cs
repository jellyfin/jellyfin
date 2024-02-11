#nullable disable

#pragma warning disable CA1711
#pragma warning disable CS1591

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv.TunerHosts.HdHomerun
{
    public class HdHomerunUdpStream : LiveStream, IDirectStreamProvider
    {
        private const int RtpHeaderBytes = 12;

        private readonly IServerApplicationHost _appHost;
        private readonly IHdHomerunChannelCommands _channelCommands;
        private readonly int _numTuners;

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
            IStreamHelper streamHelper)
            : base(mediaSource, tunerHostInfo, fileSystem, logger, configurationManager, streamHelper)
        {
            _appHost = appHost;
            OriginalStreamId = originalStreamId;
            _channelCommands = channelCommands;
            _numTuners = numTuners;
            EnableStreamSharing = true;
        }

        /// <summary>
        /// Returns an unused UDP port number in the range specified.
        /// Temporarily placed here until future network PR merged.
        /// </summary>
        /// <param name="range">Upper and Lower boundary of ports to select.</param>
        /// <returns>System.Int32.</returns>
        private static int GetUdpPortFromRange((int Min, int Max) range)
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();

            // Get active udp listeners.
            var udpListenerPorts = properties.GetActiveUdpListeners()
                        .Where(n => n.Port >= range.Min && n.Port <= range.Max)
                        .Select(n => n.Port);

            return Enumerable
                .Range(range.Min, range.Max)
                .FirstOrDefault(i => !udpListenerPorts.Contains(i));
        }

        public override async Task Open(CancellationToken openCancellationToken)
        {
            LiveStreamCancellationTokenSource.Token.ThrowIfCancellationRequested();

            var mediaSource = OriginalMediaSource;

            var uri = new Uri(mediaSource.Path);
            // Temporary code to reduce PR size. This will be updated by a future network pr.
            var localPort = GetUdpPortFromRange((49152, 65535));

            Directory.CreateDirectory(Path.GetDirectoryName(TempFilePath));

            Logger.LogInformation("Opening HDHR UDP Live stream from {Host}", uri.Host);

            var remoteAddress = IPAddress.Parse(uri.Host);
            IPAddress localAddress;
            using (var tcpClient = new TcpClient())
            {
                try
                {
                    await tcpClient.ConnectAsync(remoteAddress, HdHomerunManager.HdHomeRunPort, openCancellationToken).ConfigureAwait(false);
                    localAddress = ((IPEndPoint)tcpClient.Client.LocalEndPoint).Address;
                    tcpClient.Close();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Unable to determine local ip address for Legacy HDHomerun stream.");
                    return;
                }
            }

            if (localAddress.IsIPv4MappedToIPv6)
            {
                localAddress = localAddress.MapToIPv4();
            }

            var udpClient = new UdpClient(localPort, AddressFamily.InterNetwork);
            var hdHomerunManager = new HdHomerunManager();

            try
            {
                // send url to start streaming
                await hdHomerunManager.StartStreaming(
                    remoteAddress,
                    localAddress,
                    localPort,
                    _channelCommands,
                    _numTuners,
                    openCancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                using (udpClient)
                using (hdHomerunManager)
                {
                    if (ex is not OperationCanceledException)
                    {
                        Logger.LogError(ex, "Error opening live stream:");
                    }

                    throw;
                }
            }

            var taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            _ = StartStreaming(
                udpClient,
                hdHomerunManager,
                remoteAddress,
                taskCompletionSource,
                LiveStreamCancellationTokenSource.Token);

            // OpenedMediaSource.Protocol = MediaProtocol.File;
            // OpenedMediaSource.Path = tempFile;
            // OpenedMediaSource.ReadAtNativeFramerate = true;

            MediaSource.Path = _appHost.GetApiUrlForLocalAccess() + "/LiveTv/LiveStreamFiles/" + UniqueId + "/stream.ts";
            MediaSource.Protocol = MediaProtocol.Http;
            // OpenedMediaSource.SupportsDirectPlay = false;
            // OpenedMediaSource.SupportsDirectStream = true;
            // OpenedMediaSource.SupportsTranscoding = true;

            // await Task.Delay(5000).ConfigureAwait(false);
            await taskCompletionSource.Task.ConfigureAwait(false);
        }

        private async Task StartStreaming(UdpClient udpClient, HdHomerunManager hdHomerunManager, IPAddress remoteAddress, TaskCompletionSource<bool> openTaskCompletionSource, CancellationToken cancellationToken)
        {
            using (udpClient)
            using (hdHomerunManager)
            {
                try
                {
                    await CopyTo(udpClient, TempFilePath, openTaskCompletionSource, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException)
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

            await DeleteTempFiles(TempFilePath).ConfigureAwait(false);
        }

        private async Task CopyTo(UdpClient udpClient, string file, TaskCompletionSource<bool> openTaskCompletionSource, CancellationToken cancellationToken)
        {
            var resolved = false;

            var fileStream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read);
            await using (fileStream.ConfigureAwait(false))
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var res = await udpClient.ReceiveAsync(cancellationToken)
                        .AsTask()
                        .WaitAsync(TimeSpan.FromMilliseconds(30000), CancellationToken.None)
                        .ConfigureAwait(false);
                    var buffer = res.Buffer;

                    var read = buffer.Length - RtpHeaderBytes;

                    if (read > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(RtpHeaderBytes, read), cancellationToken).ConfigureAwait(false);
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
