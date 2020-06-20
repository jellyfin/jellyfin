namespace Rssdp.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Networking;
    using MediaBrowser.Controller.Configuration;
    using MediaBrowser.Model.Net;
    using Microsoft.Extensions.Logging;

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
         * We use one socket to listen for/receive notifications and search requests (_BroadcastListenSocket).
         * We use a second socket, bound to a different local port, to send search requests and listen for
         * responses (_SendSocket). The responses are sent to the local port this socket is bound to,
         * which isn't port 1900 so the MS service doesn't steal them. While the caller can specify a local
         * port to use, we will default to 0 which allows the underlying system to auto-assign a free port.
         */
        /// <summary>
        /// Defines the _BroadcastListenSocketSynchroniser.
        /// </summary>
        private object _BroadcastListenSocketSynchroniser = new object();

        /// <summary>
        /// Defines the _BroadcastListenSocket.
        /// </summary>
        private ISocket _BroadcastListenSocket;

        /// <summary>
        /// Defines the _SendSocketSynchroniser.
        /// </summary>
        private object _SendSocketSynchroniser = new object();

        /// <summary>
        /// Defines the _sendSockets.
        /// </summary>
        private List<ISocket> _sendSockets;

        /// <summary>
        /// Defines the _RequestParser.
        /// </summary>
        private HttpRequestParser _RequestParser;

        /// <summary>
        /// Defines the _ResponseParser.
        /// </summary>
        private HttpResponseParser _ResponseParser;

        /// <summary>
        /// Defines the _logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Defines the _SocketFactory.
        /// </summary>
        private ISocketFactory _SocketFactory;

        /// <summary>
        /// Defines the _networkManager.
        /// </summary>
        private readonly INetworkManager _networkManager;

        /// <summary>
        /// Defines the _config.
        /// </summary>
        private readonly IServerConfigurationManager _config;

        /// <summary>
        /// Defines the _LocalPort.
        /// </summary>
        private int _LocalPort;

        /// <summary>
        /// Defines the _MulticastTtl.
        /// </summary>
        private int _MulticastTtl;

        /// <summary>
        /// Defines the _IsShared.
        /// </summary>
        private bool _IsShared;

        /// <summary>
        /// Defines the _enableMultiSocketBinding.
        /// </summary>
        private readonly bool _enableMultiSocketBinding;

        /// <summary>
        /// Raised when a HTTPU request message is received by a socket (unicast or multicast).
        /// </summary>
        public event EventHandler<RequestReceivedEventArgs> RequestReceived;

        /// <summary>
        /// Raised when an HTTPU response message is received by a socket (unicast or multicast).
        /// </summary>
        public event EventHandler<ResponseReceivedEventArgs> ResponseReceived;

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpCommunicationsServer"/> class.
        /// </summary>
        /// <param name="config">The config<see cref="IServerConfigurationManager"/>.</param>
        /// <param name="socketFactory">The socketFactory<see cref="ISocketFactory"/>.</param>
        /// <param name="networkManager">The networkManager<see cref="INetworkManager"/>.</param>
        /// <param name="logger">The logger<see cref="ILogger"/>.</param>
        /// <param name="enableMultiSocketBinding">The enableMultiSocketBinding<see cref="bool"/>.</param>
        public SsdpCommunicationsServer(IServerConfigurationManager config, ISocketFactory socketFactory,
            INetworkManager networkManager, ILogger logger, bool enableMultiSocketBinding)
            : this(socketFactory, 0, SsdpConstants.SsdpDefaultMulticastTimeToLive, networkManager, logger, enableMultiSocketBinding)
        {
            _config = config;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpCommunicationsServer"/> class.
        /// </summary>
        /// <param name="socketFactory">The socketFactory<see cref="ISocketFactory"/>.</param>
        /// <param name="localPort">The localPort<see cref="int"/>.</param>
        /// <param name="multicastTimeToLive">The multicastTimeToLive<see cref="int"/>.</param>
        /// <param name="networkManager">The networkManager<see cref="INetworkManager"/>.</param>
        /// <param name="logger">The logger<see cref="ILogger"/>.</param>
        /// <param name="enableMultiSocketBinding">The enableMultiSocketBinding<see cref="bool"/>.</param>
        public SsdpCommunicationsServer(ISocketFactory socketFactory, int localPort, int multicastTimeToLive, INetworkManager networkManager, ILogger logger, bool enableMultiSocketBinding)
        {
            if (multicastTimeToLive <= 0) throw new ArgumentOutOfRangeException(nameof(multicastTimeToLive), "multicastTimeToLive must be greater than zero.");

            _BroadcastListenSocketSynchroniser = new object();
            _SendSocketSynchroniser = new object();

            _LocalPort = localPort;
            _SocketFactory = socketFactory ?? throw new ArgumentNullException(nameof(socketFactory));

            _RequestParser = new HttpRequestParser();
            _ResponseParser = new HttpResponseParser();

            _MulticastTtl = multicastTimeToLive;
            _networkManager = networkManager;
            _logger = logger;
            _enableMultiSocketBinding = enableMultiSocketBinding;
        }

        /// <summary>
        /// Causes the server to begin listening for multicast messages, being SSDP search requests and notifications.
        /// </summary>
        public void BeginListeningForBroadcasts()
        {
            ThrowIfDisposed();

            if (_BroadcastListenSocket == null)
            {
                lock (_BroadcastListenSocketSynchroniser)
                {
                    if (_BroadcastListenSocket == null)
                    {
                        try
                        {
                            _BroadcastListenSocket = ListenForBroadcastsAsync();
                        }
                        catch (SocketException ex)
                        {
                            _logger.LogError("Failed to bind to port 1900: {Message}. DLNA will be unavailable", ex.Message);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in BeginListeningForBroadcasts");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Causes the server to stop listening for multicast messages, being SSDP search requests and notifications.
        /// </summary>
        public void StopListeningForBroadcasts()
        {
            lock (_BroadcastListenSocketSynchroniser)
            {
                if (_BroadcastListenSocket != null)
                {
                    _logger.LogInformation("{0} disposing _BroadcastListenSocket", GetType().Name);
                    _BroadcastListenSocket.Dispose();
                    _BroadcastListenSocket = null;
                }
            }
        }

        /// <summary>
        /// Sends a message to a particular address (uni or multicast) and port.
        /// </summary>
        /// <param name="messageData">The messageData<see cref="byte[]"/>.</param>
        /// <param name="destination">The destination<see cref="IPEndPoint"/>.</param>
        /// <param name="fromLocalIpAddress">The fromLocalIpAddress<see cref="IPAddress"/>.</param>
        /// <param name="cancellationToken">The cancellationToken<see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task SendMessage(byte[] messageData, IPEndPoint destination, IPAddress fromLocalIpAddress, CancellationToken cancellationToken)
        {
            if (messageData == null) throw new ArgumentNullException(nameof(messageData));

            if (_networkManager.IsExcluded(fromLocalIpAddress))
            {
                _logger.LogInformation("Filtering traffic from [{0}] to {1}.", fromLocalIpAddress, destination.Address);
                return;
            }

            if (_networkManager.IsExcluded(destination.Address))
            {
                _logger.LogInformation("Filtering traffic from {0} to [{1}].", fromLocalIpAddress, destination.Address);
                return;
            }

            ThrowIfDisposed();

            var sockets = GetSendSockets(fromLocalIpAddress, destination);

            if (sockets.Count == 0)
            {
                return;
            }

            // SSDP spec recommends sending messages multiple times (not more than 3) to account for possible packet loss over UDP.
            for (var i = 0; i < SsdpConstants.UdpResendCount; i++)
            {
                var tasks = sockets.Select(s => SendFromSocket(s, messageData, destination, cancellationToken)).ToArray();
                await Task.WhenAll(tasks).ConfigureAwait(false);
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// The SendFromSocket.
        /// </summary>
        /// <param name="socket">.</param>
        /// <param name="messageData">.</param>
        /// <param name="destination">.</param>
        /// <param name="cancellationToken">.</param>
        /// <returns>.</returns>
        private async Task SendFromSocket(ISocket socket, byte[] messageData, IPEndPoint destination, CancellationToken cancellationToken)
        {
            try
            {
                await socket.SendToAsync(messageData, 0, messageData.Length, destination, cancellationToken).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException ex)
            {
                _logger.LogError("Socket error encountered sending message from {0} to {1}.", socket.LocalIPAddress, destination);

                // Remove the erroring socket.
                lock (_SendSocketSynchroniser)
                {
                    if (_sendSockets != null)
                    {
                        int index = _sendSockets.Count - 1;
                        while (index >= 0)
                        {
                            if (_sendSockets[index].Equals(socket))
                            {
                                _logger.LogDebug("Disposing socket.");
                                _sendSockets.RemoveAt(index);
                                socket.Dispose();
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Locates a suitable socket upon which to send.
        /// </summary>
        /// <param name="fromLocalIpAddress">Address to send from.</param>
        /// <param name="destination">Address to send to.</param>
        /// <returns>List of sockets that match.</returns>
        private List<ISocket> GetSendSockets(IPAddress fromLocalIpAddress, IPEndPoint destination)
        {
            EnsureSendSocketCreated();

            lock (_SendSocketSynchroniser)
            {
                var sockets = _sendSockets.Where(i => i.LocalIPAddress.AddressFamily == fromLocalIpAddress.AddressFamily);

                // Send from the Any socket and the socket with the matching address
                if (fromLocalIpAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    sockets = sockets.Where(i => i.LocalIPAddress.Equals(IPAddress.Any) || fromLocalIpAddress.Equals(i.LocalIPAddress));

                    // If sending to the loopback address, filter the socket list as well
                    if (destination.Address.Equals(IPAddress.Loopback))
                    {
                        sockets = sockets.Where(i => i.LocalIPAddress.Equals(IPAddress.Any) || i.LocalIPAddress.Equals(IPAddress.Loopback));
                    }
                }
                else if (fromLocalIpAddress.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    sockets = sockets.Where(i => i.LocalIPAddress.Equals(IPAddress.IPv6Any) || fromLocalIpAddress.Equals(i.LocalIPAddress));

                    // If sending to the loopback address, filter the socket list as well
                    if (destination.Address.Equals(IPAddress.IPv6Loopback))
                    {
                        sockets = sockets.Where(i => i.LocalIPAddress.Equals(IPAddress.IPv6Any) || i.LocalIPAddress.Equals(IPAddress.IPv6Loopback));
                    }
                }

                return sockets.ToList();
            }
        }

        /// <summary>
        /// The SendMulticastMessage.
        /// </summary>
        /// <param name="message">.</param>
        /// <param name="fromLocalIpAddress">.</param>
        /// <param name="cancellationToken">.</param>
        /// <returns>.</returns>
        public Task SendMulticastMessage(string message, IPAddress fromLocalIpAddress, CancellationToken cancellationToken)
        {
            return SendMulticastMessage(message, SsdpConstants.UdpResendCount, fromLocalIpAddress, cancellationToken);
        }

        /// <summary>
        /// Sends a message to the SSDP multicast address and port.
        /// </summary>
        /// <param name="message">The message<see cref="string"/>.</param>
        /// <param name="sendCount">The sendCount<see cref="int"/>.</param>
        /// <param name="fromLocalIpAddress">The fromLocalIpAddress<see cref="IPAddress"/>.</param>
        /// <param name="cancellationToken">The cancellationToken<see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task SendMulticastMessage(string message, int sendCount, IPAddress fromLocalIpAddress, CancellationToken cancellationToken)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            byte[] messageData = Encoding.UTF8.GetBytes(message);

            ThrowIfDisposed();

            cancellationToken.ThrowIfCancellationRequested();

            EnsureSendSocketCreated();

            // SSDP spec recommends sending messages multiple times (not more than 3) to account for possible packet loss over UDP.
            for (var i = 0; i < sendCount; i++)
            {
                await SendMessageIfSocketNotDisposed(
                        messageData,
                        new IPEndPoint(
                            IPAddress.Parse(fromLocalIpAddress.AddressFamily == AddressFamily.InterNetwork ?
                                SsdpConstants.MulticastLocalAdminAddress
                                : SsdpConstants.MulticastLocalAdminAddressV6),
                            SsdpConstants.MulticastPort),
                        fromLocalIpAddress,
                        cancellationToken).ConfigureAwait(false);

                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Stops listening for search responses on the local, unicast socket.
        /// </summary>
        public void StopListeningForResponses()
        {
            lock (_SendSocketSynchroniser)
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
        /// Gets or sets a value indicating whether IsShared
        /// Gets or sets a boolean value indicating whether or not this instance is shared amongst multiple <see cref="SsdpDeviceLocatorBase"/> and/or <see cref="ISsdpDevicePublisher"/> instances..
        /// </summary>
        public bool IsShared
        {
            get { return _IsShared; }
            set { _IsShared = value; }
        }

        /// <summary>
        /// Stops listening for requests, disposes this instance and all internal resources.
        /// </summary>
        /// <param name="disposing">.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopListeningForBroadcasts();

                StopListeningForResponses();
            }
        }

        /// <summary>
        /// Sends the same message out to all sockets in @sockets.
        /// </summary>
        /// <param name="messageData">Message to transmit.</param>
        /// <param name="destination">Destination endpoint.</param>
        /// <param name="fromLocalIpAddress">Source endpoint.</param>
        /// <param name="cancellationToken">Propegates notification that a cancellation should take place.</param>
        /// <returns>.</returns>
        private Task SendMessageIfSocketNotDisposed(byte[] messageData, IPEndPoint destination, IPAddress fromLocalIpAddress, CancellationToken cancellationToken)
        {
            var sockets = _sendSockets;
            if (sockets != null)
            {
                sockets = sockets.ToList();

                // Exclude any interfaces/IP's the user has defined.
                var tasks = sockets.Where(s =>
                        (fromLocalIpAddress.Equals(IPAddress.Any)
                        || fromLocalIpAddress.Equals(IPAddress.IPv6Any)
                        || fromLocalIpAddress.Equals(s.LocalIPAddress)))
                    .Where(s => destination.Address.AddressFamily == s.LocalIPAddress.AddressFamily)
                    .Select(s => SendFromSocket(s, messageData, destination, cancellationToken));

                return Task.WhenAll(tasks);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates a Multicast and listens for a response.
        /// </summary>
        /// <returns>.</returns>
        private ISocket ListenForBroadcastsAsync()
        {
            ISocket socket = null;

            try
            {
                socket = _SocketFactory.CreateUdpMulticastSocket(
                    _networkManager.IsIP6Enabled ? SsdpConstants.MulticastLocalAdminAddressV6 : SsdpConstants.MulticastLocalAdminAddress,
                    _MulticastTtl,
                    SsdpConstants.MulticastPort);
                _ = ListenToSocketInternal(socket);
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Error in CreateSsdpUdpSocket. IPAddress: {0}", SsdpConstants.MulticastLocalAdminAddress);
            }

            return socket;
        }

        /// <summary>
        /// Creates sockets for all internal interfaces.
        /// </summary>
        /// <returns>List of ISockets.</returns>
        private List<ISocket> CreateSocketAndListenForResponsesAsync()
        {
            var sockets = new List<ISocket>();
          
            if (_enableMultiSocketBinding)
            {
                foreach (IPNetAddress ip in _networkManager.GetInternalInterfaceAddresses())
                {
                    try
                    {
                        _logger.LogInformation("Adding socket {0}.", ip.Address);
                        sockets.Add(_SocketFactory.CreateSsdpUdpSocket(ip.Address, _LocalPort));
                    }
                    catch (SocketException ex)
                    {
                        _logger.LogError(ex, "Error in CreateSsdpUdpSocket. IPAddress: {0}", ip.Address);
                    }
                }
            }
            else
            {
                // Only create a socket for all interfaces if multisocket binding isn't enabled.
                try
                {
                    sockets.Add(_SocketFactory.CreateSsdpUdpSocket(_networkManager.IsIP6Enabled ? IPAddress.IPv6Any : IPAddress.Any, _LocalPort));
                }
                catch (SocketException ex)
                {
                    _logger.LogError(ex, "Error in CreateSsdpUdpSocket. IPAddress: Any");
                }
            }

            foreach (var socket in sockets)
            {
                _ = ListenToSocketInternal(socket);
            }

            return sockets;
        }

        /// <summary>
        /// The ListenToSocketInternal.
        /// </summary>
        /// <param name="socket">The socket<see cref="ISocket"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task ListenToSocketInternal(ISocket socket)
        {
            var cancelled = false;
            var receiveBuffer = new byte[8192];

            while (!cancelled && !IsDisposed)
            {
                try
                {
                    var result = await socket.ReceiveAsync(receiveBuffer, 0, receiveBuffer.Length, CancellationToken.None).ConfigureAwait(false);

                    // If this is from a user defined excluded address ignore it.
                    if (result.ReceivedBytes > 0)
                    {
                        // Strange cannot convert compiler error here if I don't explicitly
                        // assign or cast to Action first. Assignment is easier to read,
                        // so went with that.
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

        /// <summary>
        /// The EnsureSendSocketCreated.
        /// </summary>
        private void EnsureSendSocketCreated()
        {
            if (_sendSockets == null)
            {
                lock (_SendSocketSynchroniser)
                {
                    if (_sendSockets == null)
                    {
                        _sendSockets = CreateSocketAndListenForResponsesAsync();
                    }
                }
            }
        }

        /// <summary>
        /// The ProcessMessage.
        /// </summary>
        /// <param name="data">The data<see cref="string"/>.</param>
        /// <param name="endPoint">The endPoint<see cref="IPEndPoint"/>.</param>
        /// <param name="receivedOnLocalIpAddress">The receivedOnLocalIpAddress<see cref="IPAddress"/>.</param>
        private void ProcessMessage(string data, IPEndPoint endPoint, IPAddress receivedOnLocalIpAddress)
        {
            if (_networkManager.IsExcluded(receivedOnLocalIpAddress))
            {
                _logger.LogInformation("Filtering traffic from {0} to [{1}].", endPoint.Address, receivedOnLocalIpAddress);
                return;
            }

            if (_networkManager.IsExcluded(endPoint.Address))
            {
                _logger.LogInformation("Filtering traffic from [{0}] to {1}.", endPoint.Address, receivedOnLocalIpAddress);
                return;
            }

            // Responses start with the HTTP version, prefixed with HTTP/ while
            // requests start with a method which can vary and might be one we haven't
            // seen/don't know. We'll check if this message is a request or a response
            // by checking for the HTTP/ prefix on the start of the message.
            if (data.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
            {
                HttpResponseMessage responseMessage = null;
                try
                {
                    responseMessage = _ResponseParser.Parse(data);
                }
                catch (ArgumentException)
                {
                    // Ignore invalid packets.
                }

                if (responseMessage != null)
                {
                    OnResponseReceived(responseMessage, endPoint, receivedOnLocalIpAddress);
                }
            }
            else
            {
                HttpRequestMessage requestMessage = null;
                try
                {
                    requestMessage = _RequestParser.Parse(data);
                }
                catch (ArgumentException)
                {
                    // Ignore invalid packets.
                }

                if (requestMessage != null)
                {
                    OnRequestReceived(requestMessage, endPoint, receivedOnLocalIpAddress);
                }
            }
        }

        /// <summary>
        /// The OnRequestReceived.
        /// </summary>
        /// <param name="data">The data<see cref="HttpRequestMessage"/>.</param>
        /// <param name="remoteEndPoint">The remoteEndPoint<see cref="IPEndPoint"/>.</param>
        /// <param name="receivedOnLocalIpAddress">The receivedOnLocalIpAddress<see cref="IPAddress"/>.</param>
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

        /// <summary>
        /// The OnResponseReceived.
        /// </summary>
        /// <param name="data">The data<see cref="HttpResponseMessage"/>.</param>
        /// <param name="endPoint">The endPoint<see cref="IPEndPoint"/>.</param>
        /// <param name="localIpAddress">The localIpAddress<see cref="IPAddress"/>.</param>
        private void OnResponseReceived(HttpResponseMessage data, IPEndPoint endPoint, IPAddress localIpAddress)
        {
            this.ResponseReceived?.Invoke(this, new ResponseReceivedEventArgs(data, endPoint)
            {
                LocalIpAddress = localIpAddress
            });
        }
    }
}
