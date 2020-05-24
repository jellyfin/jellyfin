using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;
        private readonly IServerApplicationHost _appHost;
        private readonly IConfiguration _config;

        /// <summary>
        /// Address Override Configuration Key.
        /// </summary>
        public const string AddressOverrideConfigKey = "PublishedServerUrl";

        private Socket _udpSocket;
        private IPEndPoint _endpoint;
        private readonly byte[] _receiveBuffer = new byte[8192];

        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpServer" /> class.
        /// </summary>
        public UdpServer(ILogger logger, IServerApplicationHost appHost, IConfiguration configuration)
        {
            _logger = logger;
            _appHost = appHost;
            _config = configuration;
        }

        private async Task RespondToV2Message(string messageText, EndPoint endpoint, CancellationToken cancellationToken)
        {
            string localUrl = !string.IsNullOrEmpty(_config[AddressOverrideConfigKey])
                ? _config[AddressOverrideConfigKey]
                : await _appHost.GetLocalApiUrl(cancellationToken).ConfigureAwait(false);

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
        /// <param name="cancellationToken"></param>
        public void Start(int port, CancellationToken cancellationToken)
        {
            _endpoint = new IPEndPoint(IPAddress.Any, port);

            _udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpSocket.Bind(_endpoint);

            _ = Task.Run(async () => await BeginReceiveAsync(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        }

        private async Task BeginReceiveAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpSocket.ReceiveFromAsync(_receiveBuffer, SocketFlags.None, _endpoint).ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();

                    var text = Encoding.UTF8.GetString(_receiveBuffer, 0, result.ReceivedBytes);
                    if (text.Contains("who is JellyfinServer?", StringComparison.OrdinalIgnoreCase))
                    {
                        await RespondToV2Message(text, result.RemoteEndPoint, cancellationToken).ConfigureAwait(false);
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

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _udpSocket?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
