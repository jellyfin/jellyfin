#pragma warning disable CA1303 // Do not pass literals as localized parameters
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Networking;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Net;
using Microsoft.Extensions.Logging;
using Mono.Nat;

namespace Rssdp.Infrastructure
{
    /// <summary>
    /// Provides the platform independent logic for publishing device existence and responding to search requests.
    /// </summary>
    public sealed class SsdpCommunicationsServer : DisposableManagedObjectBase, ISsdpCommunicationsServer
    {
        /* We could technically use one socket listening on port 1900 for everything.
         * This should get both multicast (notifications) and unicast (search response) messages, however
         * this often doesn't work under Windows because the MS SSDP service is running. If that service
         * is running then it will steal the unicast messages and we will never see search responses.
         * Since stopping the service would be a bad idea (might not be allowed security wise and might
         * break other apps running on the system) the only other work around is to use two sockets.
         *
         * We use one socket to listen for/receive notifications and search requests (_multicastListener).
         * We use a second socket, bound to a different local port, to send search requests and listen for
         * responses (_SendSocket). The responses are sent to the local port this socket is bound to,
         * which isn't port 1900 so the MS service doesn't steal them. While the caller can specify a local
         * port to use, we will default to 0 which allows the underlying system to auto-assign a free port.
         */


        /// <summary>
        /// Multicast IP Address used for SSDP multicast messages.
        /// </summary>
        private readonly IPAddress _multicastLocalAdminAddress = IPAddress.Parse("239.255.255.250");

        /// <summary>
        /// Multicast IP6 Address used for SSDP multicast messages. 
        /// </summary>
        private readonly IPAddress _multicastLocalAdminAddressV6 = IPAddress.Parse("ff01::1");

        private readonly object _multicastListenerSynchroniser;
        private readonly object _sendSocketSynchroniser;
        private readonly HttpRequestParser _requestParser;
        private readonly HttpResponseParser _responseParser;
        private readonly ILogger _logger;
        private readonly ISocketFactory _socketFactory;
        private readonly INetworkManager _networkManager;
        private readonly IServerConfigurationManager _config;

        private readonly int _localPort;
        private readonly int _multicastTtl;
        
        private ISocket? _multicastListener;
        
        private List<ISocket>? _sendSockets;

        private bool _uPnPEnabled;

        // <summary>
        /// Initializes a new instance of the <see cref="SsdpCommunicationsServer"/> class.
        /// </summary>
        /// <param name="socketFactory">The socketFactory<see cref="ISocketFactory"/>.</param>
        /// <param name="localPort">The localPort<see cref="int"/>.</param>
        /// <param name="multicastTimeToLive">The multicastTimeToLive<see cref="int"/>.</param>
        /// <param name="networkManager">The networkManager<see cref="INetworkManager"/>.</param>
        /// <param name="logger">The logger<see cref="ILogger"/>.</param>
        /// <param name="enableMultiSocketBinding">The enableMultiSocketBinding<see cref="bool"/>.</param>
        /// /// <exception cref="ArgumentNullException">The <paramref name="socketFactory"/> argument is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="multicastTimeToLive"/> argument is less than or equal to zero.</exception>
        public SsdpCommunicationsServer(
            ISocketFactory socketFactory,
            INetworkManager networkManager,
            IServerConfigurationManager config,
            ILogger logger)
        {
            _localPort = 0;
            _multicastTtl = SsdpConstants.SsdpDefaultMulticastTimeToLive;
            _socketFactory = socketFactory ?? throw new ArgumentNullException(nameof(socketFactory));
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _multicastListenerSynchroniser = new object();
            _sendSocketSynchroniser = new object();

            _requestParser = new HttpRequestParser();
            _responseParser = new HttpResponseParser();

            _config.ConfigurationUpdated += ConfigurationUpdated;
            _networkManager.NetworkChanged += NetworkChanged;

            _uPnPEnabled = _config.Configuration.EnableUPnP && _config.Configuration.EnableRemoteAccess;
        }

        /// <summary>
        /// Raised when a HTTPU request message is received by a socket (unicast or multicast).
        /// </summary>
        public event EventHandler<RequestReceivedEventArgs>? RequestReceived;

        /// <summary>
        /// Raised when an HTTPU response message is received by a socket (unicast or multicast).
        /// </summary>
        public event EventHandler<ResponseReceivedEventArgs>? ResponseReceived;

        /// <summary>
        /// Causes the server to begin listening for multicast messages, being SSDP search requests and notifications.
        /// </summary>
        public void BeginListeningForMulticasts()
        {
            ThrowIfDisposed();

            if (_multicastListener == null)
            {
                lock (_multicastListenerSynchroniser)
                {
                    if (_multicastListener == null)
                    {
                        try
                        {
                            _multicastListener = _socketFactory.CreateUdpMulticastSocket(_networkManager.IsIP6Enabled, _multicastTtl);
                            _ = ListenToSocket(_multicastListener);
                        }
                        catch (SocketException ex)
                        {
                            _logger.LogError("Failed to bind to port 1900: {Message}. DLNA will be unavailable", ex.Message);
                        }
                    }
                }
            }

            return;
        }

        /// <summary>
        /// Causes the server to stop listening for multicast messages, being SSDP search requests and notifications.
        /// </summary>
        public void StopListeningForMulticasts()
        {
            lock (_multicastListenerSynchroniser)
            {
                if (_multicastListener != null)
                {
                    _logger.LogInformation("{0} disposing Multicast listener.");
                    _multicastListener.Dispose();
                    _multicastListener = null;
                }
            }
        }

        /// <summary>
        /// Triggered on a configuration change event.
        /// </summary>
        /// <param name="sender">Configuration object.</param>
        /// <param name="args">Event arguments.</param>
        public void ConfigurationUpdated(object sender, EventArgs args)
        {
            // Check to see if uPNP has changed status.
            bool newValue = (_config.Configuration.EnableUPnP && _config.Configuration.EnableRemoteAccess);

            if (newValue != _uPnPEnabled)
            {
                // If it has create the missing sockets.
                CreateSocketsAndListen();
                _uPnPEnabled = newValue;
            }
        }

        /// <summary>
        /// Triggered on a network change event.
        /// </summary>
        /// <param name="sender">NetworkManager object.</param>
        /// <param name="args">Event arguments.</param>
        public void NetworkChanged(object sender, EventArgs args)
        {
            var ba = _networkManager.GetInternalBindAddresses();

            // Rather than destroying all sockets and re-creating them,
            // only destroy invalid ones and create new ones.            
            lock (_sendSocketSynchroniser)
            {
                if (_sendSockets != null)
                {
                    int index = _sendSockets.Count - 1;
                    while (index >= 0)
                    {
                        if (!ba.Exists(_sendSockets[index].LocalIPAddress))
                        {
                            _logger.LogDebug("Disposing socket.");
                            var socket = _sendSockets[index];
                            _sendSockets.RemoveAt(index);
                            socket.Dispose();
                        }
                    }
                }
            }

            // Create any that are missing.
            CreateSocketsAndListen();
        }

        /// <summary>
        /// Sends a message to a particular address (unicast or multicast) via all available sockets.
        /// </summary>
        /// <param name="messageData">The message to send.</param>
        /// <param name="destination">The destination address.</param>
        /// <param name="localIp">The local IP address to use.</param>
        /// <returns>Task.</returns>
        public async Task SendMessage(byte[] messageData, IPEndPoint destination, IPAddress localIp)
        {
            if (messageData == null)
            {
                throw new ArgumentNullException(nameof(messageData));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (localIp == null)
            {
                throw new ArgumentNullException(nameof(localIp));
            }

            if (!_networkManager.IsInLocalNetwork(destination.Address))
            {
                _logger.LogDebug("SSDP filtered from sending to non-LAN address {0}.", destination.Address);
                return;
            }

            if (!localIp.Equals(IPAddress.Any) && !localIp.Equals(IPAddress.IPv6Any))
            {
                if (!_networkManager.IsInLocalNetwork(localIp))
                {
                    _logger.LogDebug("SSDP filtered due to attempt to send from a non LAN interface {0}.", localIp);
                    return;
                }
            }

            ThrowIfDisposed();
            
            var sockets = GetSendSockets(localIp, destination);

            if (sockets.Count == 0)
            {
                _logger.LogError("Unable to locate or create socket for {0}:{1}", localIp, destination);
                return;
            }

            // SSDP spec recommends sending messages multiple times (not more than 3) to account for possible packet loss over UDP.
            for (var i = 0; i < SsdpConstants.UdpResendCount; i++)
            {
                var tasks = sockets.Select(s => SendFromSocket(s, messageData, destination)).ToArray();
                await Task.WhenAll(tasks).ContinueWith(delegate { Task.Delay(100); }, TaskScheduler.Default).ConfigureAwait(false);                
            }
        }

        private async Task SendFromSocket(ISocket socket, byte[] messageData, IPEndPoint destination)
        {
            try
            {
                await socket.SendToAsync(messageData, 0, messageData.Length, destination, CancellationToken.None).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException)
            {
                _logger.LogError("Socket error encountered sending message from {0} to {1}.", socket.LocalIPAddress, destination);
            }
        }

        /// <summary>
        /// Locates a suitable set of sockets upon which to send.
        /// </summary>
        /// <param name="localIp">Address to send from.</param>
        /// <param name="destination">Address to send to.</param>
        /// <returns>List of sockets that match.</returns>
        private List<ISocket> GetSendSockets(IPAddress localIp, IPEndPoint destination)
        {
            EnsureSendSocketCreated();

            lock (_sendSocketSynchroniser)
            {
                var sockets = _sendSockets.Where(i => i.LocalIPAddress.AddressFamily == localIp.AddressFamily);

                if (sockets.Any())
                {
                    // Send from the Any socket and the socket with the matching address
                    if (localIp.AddressFamily == AddressFamily.InterNetwork)
                    {
                        sockets = sockets.Where(i => i.LocalIPAddress.Equals(IPAddress.Any) || localIp.Equals(i.LocalIPAddress));

                        // If sending to the loopback address, filter the socket list as well
                        if (destination.Address.Equals(IPAddress.Loopback))
                        {
                            sockets = sockets.Where(i => i.LocalIPAddress.Equals(IPAddress.Any) || i.LocalIPAddress.Equals(IPAddress.Loopback));
                        }
                    }
                    else if (localIp.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        sockets = sockets.Where(i => i.LocalIPAddress.Equals(IPAddress.IPv6Any) || localIp.Equals(i.LocalIPAddress));

                        // If sending to the loopback address, filter the socket list as well
                        if (destination.Address.Equals(IPAddress.IPv6Loopback))
                        {
                            sockets = sockets.Where(i => i.LocalIPAddress.Equals(IPAddress.IPv6Any) || i.LocalIPAddress.Equals(IPAddress.IPv6Loopback));
                        }
                    }
                }

                return sockets.ToList();
            }
        }

        public Task SendMulticastMessage(string message, IPAddress from)
        {
            return SendMulticastMessage(message, SsdpConstants.UdpResendCount, from);
        }

        /// <summary>
        /// Sends a message to the SSDP multicast address and port.
        /// </summary>x
        public async Task SendMulticastMessage(string message, int sendCount, IPAddress from)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            ThrowIfDisposed();

            byte[] messageData = Encoding.UTF8.GetBytes(message);

            EnsureSendSocketCreated();

            // SSDP spec recommends sending messages multiple times (not more than 3) to account for possible packet loss over UDP.
            for (var i = 0; i < sendCount; i++)
            {
                await SendMessageViaAllSockets(
                        messageData,
                        new IPEndPoint(from.AddressFamily == AddressFamily.InterNetwork ? _multicastLocalAdminAddress : _multicastLocalAdminAddressV6, 1900),
                        from).ConfigureAwait(false);

                _ = Task.Delay(100);
            }
        }

        /// <summary>
        /// Stops listening for search responses on the local, unicast socket.
        /// </summary>
        public void StopListeningForResponses()
        {
            lock (_sendSocketSynchroniser)
            {
                if (_sendSockets != null)
                {
                    var sockets = _sendSockets.ToList();
                    _sendSockets = null;

                    _logger.LogInformation("{0} disposing {1} sendSockets", GetType().Name, sockets.Count);

                    foreach (var socket in sockets)
                    {
                        _logger.LogInformation("{0} disposing sendSocket from {1}", GetType().Name, socket.LocalIPAddress);
                        socket.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a boolean value indicating whether or not this instance is shared amongst multiple <see cref="SsdpDeviceLocatorBase"/> and/or <see cref="ISsdpDevicePublisher"/> instances.
        /// </summary>
        public bool IsShared { get; set; }
        
        /// <summary>
        /// Stops listening for requests, disposes this instance and all internal resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _config.ConfigurationUpdated -= ConfigurationUpdated;
                _networkManager.NetworkChanged -= ConfigurationUpdated;

                StopListeningForMulticasts();

                StopListeningForResponses();
            }
        }

        /// <summary>
        /// Sends the same message out to all sockets in @sockets.
        /// </summary>
        /// <param name="messageData">Message to transmit.</param>
        /// <param name="destination">Destination endpoint.</param>
        /// <param name="localIp">Source endpoint.</param>
        /// <returns>.</returns>
        private Task SendMessageViaAllSockets(byte[] messageData, IPEndPoint destination, IPAddress localIp)
        {
            var sockets = _sendSockets;
            if (sockets != null)
            {
                sockets = sockets.ToList();

                // Exclude any interfaces/IP's the user has defined.
                var tasks = sockets.Where(s => (localIp.Equals(IPAddress.Any) || localIp.Equals(IPAddress.IPv6Any) || localIp.Equals(s.LocalIPAddress)))
                    .Where(s => destination.Address.AddressFamily == s.LocalIPAddress.AddressFamily)
                    .Select(s => SendFromSocket(s, messageData, destination));

                return Task.WhenAll(tasks);
            }

            return Task.CompletedTask;
        }

        private void EnsureSendSocketCreated()
        {
            if (_sendSockets == null)
            {
                lock (_sendSocketSynchroniser)
                {
                    if (_sendSockets == null)
                    {
                        _sendSockets = CreateSocketsAndListen();
                    }
                }
            }
        }

        /// <summary>
        /// Creates sockets for all internal interfaces.
        /// </summary>
        /// <returns>List of ISockets.</returns
        private List<ISocket> CreateSocketsAndListen()
        {
            ISocket? socket;

            var sockets = new List<ISocket>();
            if (_networkManager.EnableMultiSocketBinding)
            {
                foreach (IPObject ip in _networkManager.GetInternalBindAddresses())
                {
                    socket = sockets.Where(s => s.LocalIPAddress.Equals(ip.Address)).FirstOrDefault();
                    // Don't recreate a socket that exists.
                    if (socket != null)
                    {
                        // An interface with a -ve tag has a gateway address, so will be listened to by Mono.NAT.
                        // Mono.NAT will send us this traffic, so make this a send only socket.
                        socket.SendOnly = (_uPnPEnabled && ip.Tag < 0);
                        continue;
                    }

                    try
                    {
                        socket = _socketFactory.CreateSsdpUdpSocket(ip.Address, _localPort);

                        // An interface with a -ve tag has a gateway address, so will be listened to by Mono.NAT.
                        // Mono.NAT will send us this traffic, so make this a send only socket.
                        socket.SendOnly = (_uPnPEnabled && ip.Tag < 0);
                        sockets.Add(socket);
                        _logger.LogInformation("Creating socket for {0}. SendOnly {1}.", ip.Address, socket.SendOnly);
                    }
                    catch (SocketException ex)
                    {
                        _logger.LogError(ex, "Error in CreateSsdpUdpSocket. IPAddress: {0}", ip.Address);
                    }
                }
            }

            // Add an IPAny Socket if it doesn't already exist.
            socket = sockets.Where(s => s.LocalIPAddress.Equals(IPAddress.Any)).FirstOrDefault();

            try
            {

                // Don't recreate a socket that exists.
                if (socket == null)
                {
                    socket = _socketFactory.CreateSsdpUdpSocket(_networkManager.IsIP6Enabled ? IPAddress.IPv6Any : IPAddress.Any, _localPort);
                    sockets.Add(socket);
                }
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Error in CreateSsdpUdpSocket. IPAddress: Any");
            }

            // Only listen to non send-only sockets.
            foreach (var sock in sockets)
            {
                if (!sock.SendOnly)
                {
                    _= ListenToSocket(sock);
                }
            }

            return sockets;
        }

        /// <summary>
        /// Listening Loop
        /// </summary>
        /// <param name="socket">Socket to listen to.</param>
        /// <returns>Task</returns>
        private async Task ListenToSocket(ISocket socket)
        {
            try
            {
                var cancelled = false;
                var receiveBuffer = new byte[8192];

                while (!cancelled && !IsDisposed && !socket.StopListening)
                {
                    try
                    {
                        var result = await socket.ReceiveAsync(receiveBuffer, 0, receiveBuffer.Length, CancellationToken.None).ConfigureAwait(false);

                        if (result.ReceivedBytes > 0 && !socket.StopListening)
                        {
                            if (!_networkManager.IsInLocalNetwork(result.RemoteEndPoint.Address))
                            {
                                _logger.LogDebug("SSDP filtered from non-LAN address {0}.", result.RemoteEndPoint.Address);
                                return;
                            }

                            ProcessMessage(System.Text.UTF8Encoding.UTF8.GetString(result.Buffer, 0, result.ReceivedBytes), result.RemoteEndPoint, result.LocalIPAddress);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        cancelled = true;
                    }
                    catch (TaskCanceledException)
                    {
                        cancelled = true;
                    }
                }
            }
            finally
            {
                socket.StopListening = false;
            }
        }


        /// <inheritdoc/>
        public void ProcessMessage(string data, IPEndPoint endPoint, IPAddress localIp)
        {
            if (endPoint == null)
            {
                throw new ArgumentNullException(nameof(endPoint));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (localIp == null)
            {
                throw new ArgumentNullException(nameof(localIp));
            }

            if (!_networkManager.IsInLocalNetwork(endPoint.Address))
            {
                _logger.LogDebug("SSDP filtered from sending to non-LAN address {0}.", endPoint.Address);
                return;
            }

            if (!localIp.Equals(IPAddress.Any) && !localIp.Equals(IPAddress.IPv6Any))
            {
                if (!_networkManager.IsInLocalNetwork(localIp))
                {
                    _logger.LogDebug("SSDP filtered due to arrive on a non LAN interface {0}.", localIp);
                    return;
                }
            }

            // Responses start with the HTTP version, prefixed with HTTP/ while
            // requests start with a method which can vary and might be one we haven't
            // seen/don't know. We'll check if this message is a request or a response
            // by checking for the HTTP/ prefix on the start of the message.
            if (data.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
            {
                HttpResponseMessage? responseMessage = null;
                try
                {
                    responseMessage = _responseParser.Parse(data);
                }
                catch (ArgumentException)
                {
                    // Ignore invalid packets.
                }

                if (responseMessage != null)
                {
                    OnResponseReceived(responseMessage, endPoint, localIp);
                }
            }
            else
            {
                HttpRequestMessage? requestMessage = null;
                try
                {
                    requestMessage = _requestParser.Parse(data);
                }
                catch (ArgumentException)
                {
                    // Ignore invalid packets.
                }

                if (requestMessage != null)
                {
                    OnRequestReceived(requestMessage, endPoint, localIp);
                }
            }
        }

        private void OnRequestReceived(HttpRequestMessage data, IPEndPoint remoteEndPoint, IPAddress receivedOnLocalIpAddress)
        {
            // SSDP specification says only * is currently used but other uri's might
            // be implemented in the future and should be ignored unless understood.
            // Section 4.2 - http://tools.ietf.org/html/draft-cai-ssdp-v1-03#page-11
            if (data.RequestUri.ToString() != "*")
            {
                return;
            }

            this.RequestReceived?.Invoke(this, new RequestReceivedEventArgs(data, remoteEndPoint, receivedOnLocalIpAddress));
        }

        private void OnResponseReceived(HttpResponseMessage data, IPEndPoint endPoint, IPAddress localIpAddress)
        {
            this.ResponseReceived?.Invoke(this, new ResponseReceivedEventArgs(data, endPoint)
            {
                LocalIpAddress = localIpAddress
            });
        }
    }
}
