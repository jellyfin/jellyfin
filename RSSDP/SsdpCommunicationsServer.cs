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
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Net;
using Microsoft.Extensions.Logging;

namespace Rssdp.Infrastructure
{
    /// <summary>
    /// Provides the platform independent logic for publishing device existence and responding to search requests.
    /// </summary>
    public sealed class SsdpCommunicationsServer : DisposableManagedObjectBase, ISsdpCommunicationsServer
    {

        #region Fields

        /* We could technically use one socket listening on port 1900 for everything.
         * This should get both multicast (notifications) and unicast (search response) messages, however
         * this often doesn't work under Windows because the MS SSDP service is running. If that service
         * is running then it will steal the unicast messages and we will never see search responses.
         * Since stopping the service would be a bad idea (might not be allowed security wise and might
         * break other apps running on the system) the only other work around is to use two sockets.
         *
         * We use one socket to listen for/receive notifications and search requests (_BroadcastListenSocket).
         * We use a second socket, bound to a different local port, to send search requests and listen for
         * responses (_SendSocket). The responses  are sent to the local port this socket is bound to,
         * which isn't port 1900 so the MS service doesn't steal them. While the caller can specify a  local
         * port to use, we will default to 0 which allows the underlying system to auto-assign a free port.
         */

        private object _BroadcastListenSocketSynchroniser = new object();
        private ISocket _BroadcastListenSocket;

        private object _SendSocketSynchroniser = new object();
        private List<ISocket> _sendSockets;

        private HttpRequestParser _RequestParser;
        private HttpResponseParser _ResponseParser;
        private readonly ILogger _logger;
        private ISocketFactory _SocketFactory;
        private readonly INetworkManager _networkManager;
        private readonly IServerConfigurationManager _config;

        private int _LocalPort;
        private int _MulticastTtl;

        private bool _IsShared;
        private readonly bool _enableMultiSocketBinding;

        #endregion

        #region Events

        /// <summary>
        /// Raised when a HTTPU request message is received by a socket (unicast or multicast).
        /// </summary>
        public event EventHandler<RequestReceivedEventArgs> RequestReceived;

        /// <summary>
        /// Raised when an HTTPU response message is received by a socket (unicast or multicast).
        /// </summary>
        public event EventHandler<ResponseReceivedEventArgs> ResponseReceived;

        #endregion

        #region Constructors

        /// <summary>
        /// Minimum constructor.
        /// </summary>
        /// <exception cref="ArgumentNullException">The <paramref name="socketFactory"/> argument is null.</exception>
        public SsdpCommunicationsServer(IServerConfigurationManager config, ISocketFactory socketFactory,
            INetworkManager networkManager, ILogger logger, bool enableMultiSocketBinding)
            : this(socketFactory, 0, SsdpConstants.SsdpDefaultMulticastTimeToLive, networkManager, logger, enableMultiSocketBinding)
        {
            _config = config;
        }

        /// <summary>
        /// Full constructor.
        /// </summary>
        /// <exception cref="ArgumentNullException">The <paramref name="socketFactory"/> argument is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="multicastTimeToLive"/> argument is less than or equal to zero.</exception>
        public SsdpCommunicationsServer(ISocketFactory socketFactory, int localPort, int multicastTimeToLive, INetworkManager networkManager, ILogger logger, bool enableMultiSocketBinding)
        {
            if (socketFactory == null) throw new ArgumentNullException(nameof(socketFactory));
            if (multicastTimeToLive <= 0) throw new ArgumentOutOfRangeException(nameof(multicastTimeToLive), "multicastTimeToLive must be greater than zero.");

            _BroadcastListenSocketSynchroniser = new object();
            _SendSocketSynchroniser = new object();

            _LocalPort = localPort;
            _SocketFactory = socketFactory;

            _RequestParser = new HttpRequestParser();
            _ResponseParser = new HttpResponseParser();

            _MulticastTtl = multicastTimeToLive;
            _networkManager = networkManager;
            _logger = logger;
            _enableMultiSocketBinding = enableMultiSocketBinding;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Causes the server to begin listening for multicast messages, being SSDP search requests and notifications.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the <see cref="DisposableManagedObjectBase.IsDisposed"/> property is true (because <seealso cref="DisposableManagedObjectBase.Dispose()" /> has been called previously).</exception>
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
        /// <exception cref="ObjectDisposedException">Thrown if the <see cref="DisposableManagedObjectBase.IsDisposed"/> property is true (because <seealso cref="DisposableManagedObjectBase.Dispose()" /> has been called previously).</exception>
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
        public async Task SendMessage(byte[] messageData, IPEndPoint destination, IPAddress fromLocalIpAddress, CancellationToken cancellationToken)
        {
            if (messageData == null) throw new ArgumentNullException(nameof(messageData));

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending socket message from {0} to {1}", socket.LocalIPAddress.ToString(), destination.ToString());
            }
        }

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

        public Task SendMulticastMessage(string message, IPAddress fromLocalIpAddress, CancellationToken cancellationToken)
        {
            return SendMulticastMessage(message, SsdpConstants.UdpResendCount, fromLocalIpAddress, cancellationToken);
        }

        /// <summary>
        /// Sends a message to the SSDP multicast address and port.
        /// </summary>
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
                        IPAddress.Parse(SsdpConstants.MulticastLocalAdminAddress),
                        SsdpConstants.MulticastPort),
                    fromLocalIpAddress,
                    cancellationToken).ConfigureAwait(false);

                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Stops listening for search responses on the local, unicast socket.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the <see cref="DisposableManagedObjectBase.IsDisposed"/> property is true (because <seealso cref="DisposableManagedObjectBase.Dispose()" /> has been called previously).</exception>
        public void StopListeningForResponses()
        {
            lock (_SendSocketSynchroniser)
            {
                if (_sendSockets != null)
                {
                    var sockets = _sendSockets.ToList();
                    _sendSockets = null;

                    _logger.LogInformation("{0} Disposing {1} sendSockets", GetType().Name, sockets.Count);

                    foreach (var socket in sockets)
                    {
                        _logger.LogInformation("{0} disposing sendSocket from {1}", GetType().Name, socket.LocalIPAddress);
                        socket.Dispose();
                    }
                }
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets a boolean value indicating whether or not this instance is shared amongst multiple <see cref="SsdpDeviceLocatorBase"/> and/or <see cref="ISsdpDevicePublisher"/> instances.
        /// </summary>
        /// <remarks>
        /// <para>If true, disposing an instance of a <see cref="SsdpDeviceLocatorBase"/>or a <see cref="ISsdpDevicePublisher"/> will not dispose this comms server instance. The calling code is responsible for managing the lifetime of the server.</para>
        /// </remarks>
        public bool IsShared
        {
            get { return _IsShared; }
            set { _IsShared = value; }
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Stops listening for requests, disposes this instance and all internal resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopListeningForBroadcasts();

                StopListeningForResponses();
            }
        }

        #endregion

        #region Private Methods

        private Task SendMessageIfSocketNotDisposed(byte[] messageData, IPEndPoint destination, IPAddress fromLocalIpAddress, CancellationToken cancellationToken)
        {
            var sockets = _sendSockets;
            if (sockets != null)
            {
                sockets = sockets.ToList();

                var tasks = sockets.Where(s => (fromLocalIpAddress == null || fromLocalIpAddress.Equals(s.LocalIPAddress)))
                    .Select(s => SendFromSocket(s, messageData, destination, cancellationToken));
                return Task.WhenAll(tasks);
            }

            return Task.CompletedTask;
        }

        private ISocket ListenForBroadcastsAsync()
        {
            var socket = _SocketFactory.CreateUdpMulticastSocket(SsdpConstants.MulticastLocalAdminAddress, _MulticastTtl, SsdpConstants.MulticastPort);

            _ = ListenToSocketInternal(socket);

            return socket;
        }

        private List<ISocket> CreateSocketAndListenForResponsesAsync()
        {
            var sockets = new List<ISocket>();

            sockets.Add(_SocketFactory.CreateSsdpUdpSocket(IPAddress.Any, _LocalPort));

            if (_enableMultiSocketBinding)
            {
                foreach (var address in _networkManager.GetLocalIpAddresses(_config.Configuration.IgnoreVirtualInterfaces))
                {
                    if (address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        // Not support IPv6 right now
                        continue;
                    }

                    try
                    {
                        sockets.Add(_SocketFactory.CreateSsdpUdpSocket(address, _LocalPort));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in CreateSsdpUdpSocket. IPAddress: {0}", address);
                    }
                }
            }

            foreach (var socket in sockets)
            {
                _ = ListenToSocketInternal(socket);
            }

            return sockets;
        }

        private async Task ListenToSocketInternal(ISocket socket)
        {
            var cancelled = false;
            var receiveBuffer = new byte[8192];

            while (!cancelled && !IsDisposed)
            {
                try
                {
                    var result = await socket.ReceiveAsync(receiveBuffer, 0, receiveBuffer.Length, CancellationToken.None).ConfigureAwait(false);

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

        private void ProcessMessage(string data, IPEndPoint endPoint, IPAddress receivedOnLocalIpAddress)
        {
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

        private void OnRequestReceived(HttpRequestMessage data, IPEndPoint remoteEndPoint, IPAddress receivedOnLocalIpAddress)
        {
            //SSDP specification says only * is currently used but other uri's might
            //be implemented in the future and should be ignored unless understood.
            //Section 4.2 - http://tools.ietf.org/html/draft-cai-ssdp-v1-03#page-11
            if (data.RequestUri.ToString() != "*")
            {
                return;
            }

            var handlers = this.RequestReceived;
            if (handlers != null)
                handlers(this, new RequestReceivedEventArgs(data, remoteEndPoint, receivedOnLocalIpAddress));
        }

        private void OnResponseReceived(HttpResponseMessage data, IPEndPoint endPoint, IPAddress localIpAddress)
        {
            var handlers = this.ResponseReceived;
            if (handlers != null)
                handlers(this, new ResponseReceivedEventArgs(data, endPoint)
                {
                    LocalIpAddress = localIpAddress
                });
        }

        #endregion

    }
}
