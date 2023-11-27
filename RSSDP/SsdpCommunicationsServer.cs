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
using MediaBrowser.Model.Net;
using Microsoft.Extensions.Logging;

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
         * We use one group of sockets to listen for/receive notifications and search requests (_MulticastListenSockets).
         * We use a second group, bound to a different local port, to send search requests and listen for
         * responses (_SendSockets). The responses  are sent to the local ports these sockets are bound to,
         * which aren't port 1900 so the MS service doesn't steal them. While the caller can specify a local
         * port to use, we will default to 0 which allows the underlying system to auto-assign a free port.
         */

        private object _BroadcastListenSocketSynchroniser = new();
        private List<Socket> _MulticastListenSockets;

        private object _SendSocketSynchroniser = new();
        private List<Socket> _sendSockets;

        private HttpRequestParser _RequestParser;
        private HttpResponseParser _ResponseParser;
        private readonly ILogger _logger;
        private ISocketFactory _SocketFactory;
        private readonly INetworkManager _networkManager;

        private int _LocalPort;
        private int _MulticastTtl;

        private bool _IsShared;

        /// <summary>
        /// Raised when a HTTPU request message is received by a socket (unicast or multicast).
        /// </summary>
        public event EventHandler<RequestReceivedEventArgs> RequestReceived;

        /// <summary>
        /// Raised when an HTTPU response message is received by a socket (unicast or multicast).
        /// </summary>
        public event EventHandler<ResponseReceivedEventArgs> ResponseReceived;

        /// <summary>
        /// Minimum constructor.
        /// </summary>
        /// <exception cref="ArgumentNullException">The <paramref name="socketFactory"/> argument is null.</exception>
        public SsdpCommunicationsServer(
            ISocketFactory socketFactory,
            INetworkManager networkManager,
            ILogger logger)
            : this(socketFactory, 0, SsdpConstants.SsdpDefaultMulticastTimeToLive, networkManager, logger)
        {

        }

        /// <summary>
        /// Full constructor.
        /// </summary>
        /// <exception cref="ArgumentNullException">The <paramref name="socketFactory"/> argument is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="multicastTimeToLive"/> argument is less than or equal to zero.</exception>
        public SsdpCommunicationsServer(
            ISocketFactory socketFactory,
            int localPort,
            int multicastTimeToLive,
            INetworkManager networkManager,
            ILogger logger)
        {
            if (socketFactory is null)
            {
                throw new ArgumentNullException(nameof(socketFactory));
            }

            if (multicastTimeToLive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(multicastTimeToLive), "multicastTimeToLive must be greater than zero.");
            }

            _BroadcastListenSocketSynchroniser = new();
            _SendSocketSynchroniser = new();

            _LocalPort = localPort;
            _SocketFactory = socketFactory;

            _RequestParser = new();
            _ResponseParser = new();

            _MulticastTtl = multicastTimeToLive;
            _networkManager = networkManager;
            _logger = logger;
        }

        /// <summary>
        /// Causes the server to begin listening for multicast messages, being SSDP search requests and notifications.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the <see cref="DisposableManagedObjectBase.IsDisposed"/> property is true (because <seealso cref="DisposableManagedObjectBase.Dispose()" /> has been called previously).</exception>
        public void BeginListeningForMulticast()
        {
            ThrowIfDisposed();

            lock (_BroadcastListenSocketSynchroniser)
            {
                if (_MulticastListenSockets is null)
                {
                    try
                    {
                        _MulticastListenSockets = CreateMulticastSocketsAndListen();
                    }
                    catch (SocketException ex)
                    {
                        _logger.LogError("Failed to bind to multicast address: {Message}. DLNA will be unavailable", ex.Message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in BeginListeningForMulticast");
                    }
                }
            }
        }

        /// <summary>
        /// Causes the server to stop listening for multicast messages, being SSDP search requests and notifications.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the <see cref="DisposableManagedObjectBase.IsDisposed"/> property is true (because <seealso cref="DisposableManagedObjectBase.Dispose()" /> has been called previously).</exception>
        public void StopListeningForMulticast()
        {
            lock (_BroadcastListenSocketSynchroniser)
            {
                if (_MulticastListenSockets is not null)
                {
                    _logger.LogInformation("{0} disposing _BroadcastListenSocket", GetType().Name);
                    foreach (var socket in _MulticastListenSockets)
                    {
                        socket.Dispose();
                    }

                    _MulticastListenSockets = null;
                }
            }
        }

        /// <summary>
        /// Sends a message to a particular address (uni or multicast) and port.
        /// </summary>
        public async Task SendMessage(byte[] messageData, IPEndPoint destination, IPAddress fromlocalIPAddress, CancellationToken cancellationToken)
        {
            if (messageData is null)
            {
                throw new ArgumentNullException(nameof(messageData));
            }

            ThrowIfDisposed();

            var sockets = GetSendSockets(fromlocalIPAddress, destination);

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

        private async Task SendFromSocket(Socket socket, byte[] messageData, IPEndPoint destination, CancellationToken cancellationToken)
        {
            try
            {
                await socket.SendToAsync(messageData, destination, cancellationToken).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                var localIP = ((IPEndPoint)socket.LocalEndPoint).Address;
                _logger.LogError(ex, "Error sending socket message from {0} to {1}", localIP.ToString(), destination.ToString());
            }
        }

        private List<Socket> GetSendSockets(IPAddress fromlocalIPAddress, IPEndPoint destination)
        {
            EnsureSendSocketCreated();

            lock (_SendSocketSynchroniser)
            {
                var sockets = _sendSockets.Where(s => s.AddressFamily == fromlocalIPAddress.AddressFamily);

                // Send from the Any socket and the socket with the matching address
                if (fromlocalIPAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    sockets = sockets.Where(s => ((IPEndPoint)s.LocalEndPoint).Address.Equals(IPAddress.Any)
                        || ((IPEndPoint)s.LocalEndPoint).Address.Equals(fromlocalIPAddress));

                    // If sending to the loopback address, filter the socket list as well
                    if (destination.Address.Equals(IPAddress.Loopback))
                    {
                        sockets = sockets.Where(s => ((IPEndPoint)s.LocalEndPoint).Address.Equals(IPAddress.Any)
                            || ((IPEndPoint)s.LocalEndPoint).Address.Equals(IPAddress.Loopback));
                    }
                }
                else if (fromlocalIPAddress.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    sockets = sockets.Where(s => ((IPEndPoint)s.LocalEndPoint).Address.Equals(IPAddress.IPv6Any)
                        || ((IPEndPoint)s.LocalEndPoint).Address.Equals(fromlocalIPAddress));

                    // If sending to the loopback address, filter the socket list as well
                    if (destination.Address.Equals(IPAddress.IPv6Loopback))
                    {
                        sockets = sockets.Where(s => ((IPEndPoint)s.LocalEndPoint).Address.Equals(IPAddress.IPv6Any)
                            || ((IPEndPoint)s.LocalEndPoint).Address.Equals(IPAddress.IPv6Loopback));
                    }
                }

                return sockets.ToList();
            }
        }

        public Task SendMulticastMessage(string message, IPAddress fromlocalIPAddress, CancellationToken cancellationToken)
        {
            return SendMulticastMessage(message, SsdpConstants.UdpResendCount, fromlocalIPAddress, cancellationToken);
        }

        /// <summary>
        /// Sends a message to the SSDP multicast address and port.
        /// </summary>
        public async Task SendMulticastMessage(string message, int sendCount, IPAddress fromlocalIPAddress, CancellationToken cancellationToken)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

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
                    fromlocalIPAddress,
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
                if (_sendSockets is not null)
                {
                    var sockets = _sendSockets.ToList();
                    _sendSockets = null;

                    _logger.LogInformation("{0} Disposing {1} sendSockets", GetType().Name, sockets.Count);

                    foreach (var socket in sockets)
                    {
                        var socketAddress = ((IPEndPoint)socket.LocalEndPoint).Address;
                        _logger.LogInformation("{0} disposing sendSocket from {1}", GetType().Name, socketAddress);
                        socket.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a boolean value indicating whether or not this instance is shared amongst multiple <see cref="SsdpDeviceLocator"/> and/or <see cref="ISsdpDevicePublisher"/> instances.
        /// </summary>
        /// <remarks>
        /// <para>If true, disposing an instance of a <see cref="SsdpDeviceLocator"/>or a <see cref="ISsdpDevicePublisher"/> will not dispose this comms server instance. The calling code is responsible for managing the lifetime of the server.</para>
        /// </remarks>
        public bool IsShared
        {
            get { return _IsShared; }

            set { _IsShared = value; }
        }

        /// <summary>
        /// Stops listening for requests, disposes this instance and all internal resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopListeningForMulticast();

                StopListeningForResponses();
            }
        }

        private Task SendMessageIfSocketNotDisposed(byte[] messageData, IPEndPoint destination, IPAddress fromlocalIPAddress, CancellationToken cancellationToken)
        {
            var sockets = _sendSockets;
            if (sockets is not null)
            {
                sockets = sockets.ToList();

                var tasks = sockets.Where(s => fromlocalIPAddress is null || fromlocalIPAddress.Equals(((IPEndPoint)s.LocalEndPoint).Address))
                    .Select(s => SendFromSocket(s, messageData, destination, cancellationToken));
                return Task.WhenAll(tasks);
            }

            return Task.CompletedTask;
        }

        private List<Socket> CreateMulticastSocketsAndListen()
        {
            var sockets = new List<Socket>();
            var multicastGroupAddress = IPAddress.Parse(SsdpConstants.MulticastLocalAdminAddress);

            // IPv6 is currently unsupported
            var validInterfaces = _networkManager.GetInternalBindAddresses()
                .Where(x => x.Address is not null)
                .Where(x => x.SupportsMulticast)
                .Where(x => x.AddressFamily == AddressFamily.InterNetwork)
                .DistinctBy(x => x.Index);

            foreach (var intf in validInterfaces)
            {
                try
                {
                    var socket = _SocketFactory.CreateUdpMulticastSocket(multicastGroupAddress, intf, _MulticastTtl, SsdpConstants.MulticastPort);
                    _ = ListenToSocketInternal(socket);
                    sockets.Add(socket);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create SSDP UDP multicast socket for {0} on interface {1} (index {2})", intf.Address, intf.Name, intf.Index);
                }
            }

            return sockets;
        }

        private List<Socket> CreateSendSockets()
        {
            var sockets = new List<Socket>();

            // IPv6 is currently unsupported
            var validInterfaces = _networkManager.GetInternalBindAddresses()
                .Where(x => x.Address is not null)
                .Where(x => x.SupportsMulticast)
                .Where(x => x.AddressFamily == AddressFamily.InterNetwork);

            if (OperatingSystem.IsMacOS())
            {
                // Manually remove loopback on macOS due to https://github.com/dotnet/runtime/issues/24340
                validInterfaces = validInterfaces.Where(x => !x.Address.Equals(IPAddress.Loopback));
            }

            foreach (var intf in validInterfaces)
            {
                try
                {
                    var socket = _SocketFactory.CreateSsdpUdpSocket(intf, _LocalPort);
                    _ = ListenToSocketInternal(socket);
                    sockets.Add(socket);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create SSDP UDP sender socket for {0} on interface {1} (index {2})", intf.Address, intf.Name, intf.Index);
                }
            }

            return sockets;
        }

        private async Task ListenToSocketInternal(Socket socket)
        {
            var cancelled = false;
            var receiveBuffer = new byte[8192];

            while (!cancelled && !IsDisposed)
            {
                try
                {
                    var result = await socket.ReceiveMessageFromAsync(receiveBuffer, new IPEndPoint(IPAddress.Any, _LocalPort), CancellationToken.None).ConfigureAwait(false);

                    if (result.ReceivedBytes > 0)
                    {
                        var remoteEndpoint = (IPEndPoint)result.RemoteEndPoint;
                        var localEndpointAdapter = _networkManager.GetAllBindInterfaces().First(a => a.Index == result.PacketInformation.Interface);

                        ProcessMessage(
                            Encoding.UTF8.GetString(receiveBuffer, 0, result.ReceivedBytes),
                            remoteEndpoint,
                            localEndpointAdapter.Address);
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
            if (_sendSockets is null)
            {
                lock (_SendSocketSynchroniser)
                {
                    _sendSockets ??= CreateSendSockets();
                }
            }
        }

        private void ProcessMessage(string data, IPEndPoint endPoint, IPAddress receivedOnlocalIPAddress)
        {
            // Responses start with the HTTP version, prefixed with HTTP/ while
            // requests start with a method which can vary and might be one we haven't
            // seen/don't know. We'll check if this message is a request or a response
            // by checking for the HTTP/ prefix on the start of the message.
            _logger.LogDebug("Received data from {From} on {Port} at {Address}:\n{Data}", endPoint.Address, endPoint.Port, receivedOnlocalIPAddress, data);
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

                if (responseMessage is not null)
                {
                    OnResponseReceived(responseMessage, endPoint, receivedOnlocalIPAddress);
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

                if (requestMessage is not null)
                {
                    OnRequestReceived(requestMessage, endPoint, receivedOnlocalIPAddress);
                }
            }
        }

        private void OnRequestReceived(HttpRequestMessage data, IPEndPoint remoteEndPoint, IPAddress receivedOnlocalIPAddress)
        {
            // SSDP specification says only * is currently used but other uri's might
            // be implemented in the future and should be ignored unless understood.
            // Section 4.2 - http://tools.ietf.org/html/draft-cai-ssdp-v1-03#page-11
            if (data.RequestUri.ToString() != "*")
            {
                return;
            }

            var handlers = RequestReceived;
            handlers?.Invoke(this, new RequestReceivedEventArgs(data, remoteEndPoint, receivedOnlocalIPAddress));
        }

        private void OnResponseReceived(HttpResponseMessage data, IPEndPoint endPoint, IPAddress localIPAddress)
        {
            var handlers = ResponseReceived;
            handlers?.Invoke(this, new ResponseReceivedEventArgs(data, endPoint)
            {
                LocalIPAddress = localIPAddress
            });
        }
    }
}
