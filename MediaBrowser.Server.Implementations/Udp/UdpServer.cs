using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Udp
{
    /// <summary>
    /// Provides a Udp Server
    /// </summary>
    public class UdpServer : IDisposable
    {
        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The _network manager
        /// </summary>
        private readonly INetworkManager _networkManager;
        /// <summary>
        /// The _HTTP server
        /// </summary>
        private readonly IHttpServer _httpServer;

        /// <summary>
        /// The _server configuration manager
        /// </summary>
        private readonly IServerConfigurationManager _serverConfigurationManager;

        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpServer" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="networkManager">The network manager.</param>
        /// <param name="serverConfigurationManager">The server configuration manager.</param>
        /// <param name="httpServer">The HTTP server.</param>
        public UdpServer(ILogger logger, INetworkManager networkManager, IServerConfigurationManager serverConfigurationManager, IHttpServer httpServer)
        {
            _logger = logger;
            _networkManager = networkManager;
            _serverConfigurationManager = serverConfigurationManager;
            _httpServer = httpServer;
        }

        /// <summary>
        /// Raises the <see cref="E:MessageReceived" /> event.
        /// </summary>
        /// <param name="e">The <see cref="UdpMessageReceivedEventArgs"/> instance containing the event data.</param>
        private async void OnMessageReceived(UdpMessageReceivedEventArgs e)
        {
            const string context = "Server";

            var expectedMessage = String.Format("who is MediaBrowser{0}?", context);
            var expectedMessageBytes = Encoding.UTF8.GetBytes(expectedMessage);

            if (expectedMessageBytes.SequenceEqual(e.Bytes))
            {
                _logger.Info("Received UDP server request from " + e.RemoteEndPoint);

                var localAddress = GetLocalIpAddress();

                if (!string.IsNullOrEmpty(localAddress))
                {
                    // Send a response back with our ip address and port
                    var response = String.Format("MediaBrowser{0}|{1}:{2}", context, GetLocalIpAddress(), _serverConfigurationManager.Configuration.HttpServerPortNumber);

                    await SendAsync(Encoding.UTF8.GetBytes(response), e.RemoteEndPoint);
                }
                else
                {
                    _logger.Warn("Unable to respond to udp request because the local ip address could not be determined.");
                }
            }
        }

        /// <summary>
        /// Gets the local ip address.
        /// </summary>
        /// <returns>System.String.</returns>
        private string GetLocalIpAddress()
        {
            var localAddresses = _networkManager.GetLocalIpAddresses().ToList();

            // Cross-check the local ip addresses with addresses that have been received on with the http server
            var matchedAddress = _httpServer.LocalEndPoints
                .ToList()
                .Select(i => i.Split(':').FirstOrDefault())
                .Where(i => !string.IsNullOrEmpty(i))
                .FirstOrDefault(i => localAddresses.Contains(i, StringComparer.OrdinalIgnoreCase));

            // Return the first matched address, if found, or the first known local address
            return matchedAddress ?? localAddresses.FirstOrDefault();
        }

        /// <summary>
        /// The _udp client
        /// </summary>
        private UdpClient _udpClient;

        /// <summary>
        /// Starts the specified port.
        /// </summary>
        /// <param name="port">The port.</param>
        public void Start(int port)
        {
            _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));

            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            Task.Run(() => StartListening());
        }

        private async void StartListening()
        {
            while (!_isDisposed)
            {
                try
                {
                    var result = await GetResult().ConfigureAwait(false);

                    OnMessageReceived(result);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in StartListening", ex);
                }
            }
        }

        private Task<UdpReceiveResult> GetResult()
        {
            try
            {
                return _udpClient.ReceiveAsync();
            }
            catch (ObjectDisposedException)
            {
                return Task.FromResult(new UdpReceiveResult(new byte[] { }, new IPEndPoint(IPAddress.Any, 0)));
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error receiving udp message", ex);
                return Task.FromResult(new UdpReceiveResult(new byte[] { }, new IPEndPoint(IPAddress.Any, 0)));
            }
        }

        /// <summary>
        /// Called when [message received].
        /// </summary>
        /// <param name="message">The message.</param>
        private void OnMessageReceived(UdpReceiveResult message)
        {
            if (message.RemoteEndPoint.Port == 0)
            {
                return;
            }
            var bytes = message.Buffer;

            OnMessageReceived(new UdpMessageReceivedEventArgs
            {
                Bytes = bytes,
                RemoteEndPoint = message.RemoteEndPoint.ToString()
            });
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            _isDisposed = true;

            if (_udpClient != null)
            {
                _udpClient.Close();
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                Stop();
            }
        }

        /// <summary>
        /// Sends the async.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="port">The port.</param>
        /// <returns>Task{System.Int32}.</returns>
        /// <exception cref="System.ArgumentNullException">data</exception>
        public Task SendAsync(string data, string ipAddress, int port)
        {
            return SendAsync(Encoding.UTF8.GetBytes(data), ipAddress, port);
        }

        /// <summary>
        /// Sends the async.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="port">The port.</param>
        /// <returns>Task{System.Int32}.</returns>
        /// <exception cref="System.ArgumentNullException">bytes</exception>
        public Task SendAsync(byte[] bytes, string ipAddress, int port)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if (string.IsNullOrEmpty(ipAddress))
            {
                throw new ArgumentNullException("ipAddress");
            }

            return _udpClient.SendAsync(bytes, bytes.Length, ipAddress, port);
        }

        /// <summary>
        /// Sends the async.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// bytes
        /// or
        /// remoteEndPoint
        /// </exception>
        public async Task SendAsync(byte[] bytes, string remoteEndPoint)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if (string.IsNullOrEmpty(remoteEndPoint))
            {
                throw new ArgumentNullException("remoteEndPoint");
            }

            try
            {
                await _udpClient.SendAsync(bytes, bytes.Length, _networkManager.Parse(remoteEndPoint)).ConfigureAwait(false);

                _logger.Info("Udp message sent to {0}", remoteEndPoint);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending message to {0}", ex, remoteEndPoint);
            }
        }
    }

}
