using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Networking;
using MediaBrowser.Controller;
using MediaBrowser.Model.ApiClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Udp
{
    /// <summary>
    /// Provides a Udp Server.
    /// </summary>
    public sealed class UdpServer : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IServerApplicationHost _appHost;
        private readonly IConfiguration _config;

        /// <summary>
        /// UDP socket being used.
        /// </summary>
        private Socket _udpSocket;

        /// <summary>
        /// IPAddress.Any endpoint.
        /// </summary>
        private IPEndPoint _endpoint;

        private readonly byte[] _receiveBuffer = new byte[8192];

        /// <summary>
        /// Not quite sure why this is here.
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpServer" /> class.
        /// </summary>
        /// <param name="appHost">IServerApplicationHost instance.</param>
        /// <param name="logger"> ILogger instance.</param>
        /// <param name="configuration">IConfiguration instance.</param>
        public UdpServer(ILogger logger, IServerApplicationHost appHost)
        {
            _logger = logger;
            _appHost = appHost;
        }

        /// <summary>
        /// Processes any received data.
        /// </summary>
        /// <param name="messageText">Message text received.</param>
        /// <param name="endpoint">Received from.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
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
        /// Starts the specified port.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="cancellationToken">Cancellaton Token.</param>
        public void Start(int port, CancellationToken cancellationToken)
        {
            _endpoint = new IPEndPoint(IPAddress.Any, port);

            if (NetworkManager.Instance.IsIP6Enabled)
            {
                _udpSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                _udpSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            }
            else
            {
                _udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }

            _udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpSocket.Bind(_endpoint);

            _ = Task.Run(async () => await BeginReceiveAsync(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
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
        /// Begins listening to this socket, passing any incoming data to <see cref="RespondToV2Message"/> for processing.
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Task.</returns>
        private async Task BeginReceiveAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var infiniteTask = Task.Delay(-1, cancellationToken);
                try
                {
                    var task = _udpSocket.ReceiveFromAsync(_receiveBuffer, SocketFlags.None, _endpoint);
                    await Task.WhenAny(task, infiniteTask).ConfigureAwait(false);

                    if (!task.IsCompleted)
                    {
                        return;
                    }

                    var result = task.Result;

                    // If this from an excluded address don't both responding to it.
                    if (!NetworkManager.Instance.IsExcluded(((IPEndPoint)result.RemoteEndPoint).Address))
                    {
                        var text = Encoding.UTF8.GetString(_receiveBuffer, 0, result.ReceivedBytes);
                        if (text.Contains("who is JellyfinServer?", StringComparison.OrdinalIgnoreCase))
                        {
                            await RespondToV2Message(text, result.RemoteEndPoint).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Filtering traffic from [{0}] to {1}.", result.RemoteEndPoint, _endpoint);
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
    }
}
