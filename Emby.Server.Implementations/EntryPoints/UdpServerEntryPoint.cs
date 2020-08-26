#nullable enable
using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Emby.Dlna.Net;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.ApiClient;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.EntryPoints
{
    /// <summary>
    /// Class UdpServerEntryPoint.
    /// </summary>
    public sealed class UdpServerEntryPoint : IServerEntryPoint
    {
        /// <summary>
        /// The port of the UDP server.
        /// </summary>
        public const int PortNumber = 7359;

        private readonly IServerApplicationHost _appHost;
        private readonly ILogger<UdpServerEntryPoint> _logger;
        private readonly INetworkManager _networkManager;

        /// <summary>
        /// UDP socket being used.
        /// </summary>
        private Socket? _udpSocket;

        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpServerEntryPoint"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="appHost">Application Host instance.</param>
        /// <param name="networkManager">NetwortManager instance.</param>
        public UdpServerEntryPoint(
            ILogger<UdpServerEntryPoint> logger,
            IServerApplicationHost appHost,
            INetworkManager networkManager)
        {
            _logger = logger ?? throw new NullReferenceException(nameof(logger));
            _appHost = appHost ?? throw new NullReferenceException(nameof(appHost));
            _networkManager = networkManager ?? throw new NullReferenceException(nameof(networkManager));
        }

        /// <inheritdoc />
        public Task RunAsync()
        {
            try
            {
                _udpSocket = SocketServer.Instance.CreateUdpBroadcastSocket(PortNumber);
                _ = Task.Run(async () => await BeginReceiveAsync().ConfigureAwait(false));
            }
            catch (SocketException ex)
            {
                _logger.LogWarning(ex, "Unable to start AutoDiscovery listener on UDP port {PortNumber}", PortNumber);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _udpSocket?.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Processes any received data.
        /// </summary>
        /// <param name="messageText">Message text received.</param>
        /// <param name="endpoint">Received from.</param>
        /// <returns>Task.</returns>
        private async Task RespondToV2Message(string messageText, EndPoint endpoint)
        {
            string localUrl = _appHost.GetSmartApiUrl(((IPEndPoint)endpoint).Address);

            if (!string.IsNullOrEmpty(localUrl))
            {
                var response = new ServerDiscoveryInfo
                {
                    Address = localUrl,
                    Id = _appHost.SystemId,
                    Name = _appHost.FriendlyName
                };

                try
                {
                    await _udpSocket.SendToAsync(JsonSerializer.SerializeToUtf8Bytes(response), SocketFlags.None, endpoint).ConfigureAwait(false);
                }
                catch (SocketException ex)
                {
                    _logger.LogError(ex, "Error sending response message");
                }

                var parts = messageText.Split('|');
                if (parts.Length > 1)
                {
                    _appHost.EnableLoopback(parts[1]);
                }
            }
            else
            {
                _logger.LogWarning("Unable to respond to udp request because the local ip address could not be determined.");
            }
        }

        /// <summary>
        /// Begins listening to this socket, passing any incoming data to <see cref="RespondToV2Message"/> for processing.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task BeginReceiveAsync()
        {
            if (_udpSocket == null)
            {
                throw new NullReferenceException("UdpSocket cannot be null.");
            }

            var receiveBuffer = ArrayPool<byte>.Shared.Rent(8192);

            try
            {
                EndPoint endpoint = _udpSocket.LocalEndPoint; // _networkManager.GetMulticastEndPoint(PortNumber);
                while (!_disposed)
                {
                    try
                    {
                        var result = await _udpSocket.ReceiveFromAsync(receiveBuffer, SocketFlags.None, endpoint).ConfigureAwait(false);

                        // If this from an excluded address don't both responding to it.
                        if (!_networkManager.IsExcluded(result.RemoteEndPoint))
                        {
                            var text = Encoding.UTF8.GetString(receiveBuffer, 0, result.ReceivedBytes);
                            if (text.Contains("who is JellyfinServer?", StringComparison.OrdinalIgnoreCase))
                            {
                                await RespondToV2Message(text, result.RemoteEndPoint).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("Filtering traffic from [{0}] to {1}.", result.RemoteEndPoint, endpoint);
                        }
                    }
                    catch (SocketException ex)
                    {
                        _logger.LogError(ex, "Failed to receive data from socket");
                    }
                    catch (OperationCanceledException)
                    {
                        // Don't throw
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(receiveBuffer);
            }
        }
    }
}
