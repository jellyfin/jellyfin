using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;

namespace Rssdp.Infrastructure
{
    /// <summary>
    /// Provides the platform independent logic for publishing device existence and responding to search requests.
    /// </summary>
    public sealed class SsdpCommunicationsServer : DisposableManagedObjectBase, ISsdpCommunicationsServer
    {

        #region Fields

        /* 
		  
		 We could technically use one socket listening on port 1900 for everything.
		 This should get both multicast (notifications) and unicast (search response) messages, however 
		 this often doesn't work under Windows because the MS SSDP service is running. If that service 
		 is running then it will steal the unicast messages and we will never see search responses. 
		 Since stopping the service would be a bad idea (might not be allowed security wise and might 
		 break other apps running on the system) the only other work around is to use two sockets.
		
		 We use one socket to listen for/receive notifications and search requests (_BroadcastListenSocket).
		 We use a second socket, bound to a different local port, to send search requests and listen for 
		 responses (_SendSocket). The responses  are sent to the local port this socket is bound to, 
		 which isn't port 1900 so the MS service doesn't steal them. While the caller can specify a  local 
		 port to use, we will default to 0 which allows the underlying system to auto-assign a free port.
		  
		*/

        private object _BroadcastListenSocketSynchroniser = new object();
        private IUdpSocket _BroadcastListenSocket;

        private object _SendSocketSynchroniser = new object();
        private List<IUdpSocket> _sendSockets;

        private HttpRequestParser _RequestParser;
        private HttpResponseParser _ResponseParser;
        private readonly ILogger _logger;
        private ISocketFactory _SocketFactory;
        private readonly INetworkManager _networkManager;

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
        /// <exception cref="System.ArgumentNullException">The <paramref name="socketFactory"/> argument is null.</exception>
        public SsdpCommunicationsServer(ISocketFactory socketFactory, INetworkManager networkManager, ILogger logger, bool enableMultiSocketBinding)
            : this(socketFactory, 0, SsdpConstants.SsdpDefaultMulticastTimeToLive, networkManager, logger, enableMultiSocketBinding)
        {
        }

        /// <summary>
        /// Full constructor.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">The <paramref name="socketFactory"/> argument is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">The <paramref name="multicastTimeToLive"/> argument is less than or equal to zero.</exception>
        public SsdpCommunicationsServer(ISocketFactory socketFactory, int localPort, int multicastTimeToLive, INetworkManager networkManager, ILogger logger, bool enableMultiSocketBinding)
        {
            if (socketFactory == null) throw new ArgumentNullException("socketFactory");
            if (multicastTimeToLive <= 0) throw new ArgumentOutOfRangeException("multicastTimeToLive", "multicastTimeToLive must be greater than zero.");

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
        /// <exception cref="System.ObjectDisposedException">Thrown if the <see cref="DisposableManagedObjectBase.IsDisposed"/> property is true (because <seealso cref="DisposableManagedObjectBase.Dispose()" /> has been called previously).</exception>
        public void BeginListeningForBroadcasts()
        {
            ThrowIfDisposed();

            if (_BroadcastListenSocket == null)
            {
                lock (_BroadcastListenSocketSynchroniser)
                {
                    if (_BroadcastListenSocket == null)
                        _BroadcastListenSocket = ListenForBroadcastsAsync();
                }
            }
        }

        /// <summary>
        /// Causes the server to stop listening for multicast messages, being SSDP search requests and notifications.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">Thrown if the <see cref="DisposableManagedObjectBase.IsDisposed"/> property is true (because <seealso cref="DisposableManagedObjectBase.Dispose()" /> has been called previously).</exception>
        public void StopListeningForBroadcasts()
        {
            ThrowIfDisposed();

            lock (_BroadcastListenSocketSynchroniser)
            {
                if (_BroadcastListenSocket != null)
                {
                    _BroadcastListenSocket.Dispose();
                    _BroadcastListenSocket = null;
                }
            }
        }

        /// <summary>
        /// Sends a message to a particular address (uni or multicast) and port.
        /// </summary>
        /// <param name="messageData">A byte array containing the data to send.</param>
        /// <param name="destination">A <see cref="IpEndPointInfo"/> representing the destination address for the data. Can be either a multicast or unicast destination.</param>
        /// <param name="fromLocalIpAddress">A <see cref="IpEndPointInfo"/> The local ip address to send from, or .Any if sending from all available</param>
        /// <exception cref="System.ArgumentNullException">Thrown if the <paramref name="messageData"/> argument is null.</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the <see cref="DisposableManagedObjectBase.IsDisposed"/> property is true (because <seealso cref="DisposableManagedObjectBase.Dispose()" /> has been called previously).</exception>
        public async Task SendMessage(byte[] messageData, IpEndPointInfo destination, IpAddressInfo fromLocalIpAddress)
        {
            if (messageData == null) throw new ArgumentNullException("messageData");

            ThrowIfDisposed();

            var sockets = GetSendSockets(fromLocalIpAddress, destination);

            if (sockets.Count == 0)
            {
                return;
            }

            // SSDP spec recommends sending messages multiple times (not more than 3) to account for possible packet loss over UDP.
            for (var i = 0; i < SsdpConstants.UdpResendCount; i++)
            {
                var tasks = sockets.Select(s => SendFromSocket(s, messageData, destination)).ToArray();
                await Task.WhenAll(tasks).ConfigureAwait(false);

                await Task.Delay(100).ConfigureAwait(false);
            }
        }

        private async Task SendFromSocket(IUdpSocket socket, byte[] messageData, IpEndPointInfo destination)
        {
            try
            {
                await socket.SendAsync(messageData, messageData.Length, destination).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {

            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending socket message from {0} to {1}", ex, socket.LocalIPAddress.ToString(), destination.ToString());
            }
        }

        private List<IUdpSocket> GetSendSockets(IpAddressInfo fromLocalIpAddress, IpEndPointInfo destination)
        {
            EnsureSendSocketCreated();

            lock (_SendSocketSynchroniser)
            {
                var sockets = _sendSockets.Where(i => i.LocalIPAddress.AddressFamily == fromLocalIpAddress.AddressFamily);

                // Send from the Any socket and the socket with the matching address
                if (fromLocalIpAddress.AddressFamily == IpAddressFamily.InterNetwork)
                {
                    sockets = sockets.Where(i => i.LocalIPAddress.Equals(IpAddressInfo.Any) || fromLocalIpAddress.Equals(i.LocalIPAddress));

                    // If sending to the loopback address, filter the socket list as well
                    if (destination.IpAddress.Equals(IpAddressInfo.Loopback))
                    {
                        sockets = sockets.Where(i => i.LocalIPAddress.Equals(IpAddressInfo.Any) || i.LocalIPAddress.Equals(IpAddressInfo.Loopback));
                    }
                }
                else if (fromLocalIpAddress.AddressFamily == IpAddressFamily.InterNetworkV6)
                {
                    sockets = sockets.Where(i => i.LocalIPAddress.Equals(IpAddressInfo.IPv6Any) || fromLocalIpAddress.Equals(i.LocalIPAddress));

                    // If sending to the loopback address, filter the socket list as well
                    if (destination.IpAddress.Equals(IpAddressInfo.IPv6Loopback))
                    {
                        sockets = sockets.Where(i => i.LocalIPAddress.Equals(IpAddressInfo.IPv6Any) || i.LocalIPAddress.Equals(IpAddressInfo.IPv6Loopback));
                    }
                }

                return sockets.ToList();
            }
        }

        /// <summary>
        /// Sends a message to the SSDP multicast address and port.
        /// </summary>
        public async Task SendMulticastMessage(string message)
        {
            if (message == null) throw new ArgumentNullException("messageData");

            byte[] messageData = Encoding.UTF8.GetBytes(message);

            ThrowIfDisposed();

            EnsureSendSocketCreated();

            // SSDP spec recommends sending messages multiple times (not more than 3) to account for possible packet loss over UDP.
            for (var i = 0; i < SsdpConstants.UdpResendCount; i++)
            {
                await SendMessageIfSocketNotDisposed(messageData, new IpEndPointInfo
                {
                    IpAddress = new IpAddressInfo
                    {
                        Address = SsdpConstants.MulticastLocalAdminAddress
                    },
                    Port = SsdpConstants.MulticastPort

                }).ConfigureAwait(false);

                await Task.Delay(100).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Stops listening for search responses on the local, unicast socket.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">Thrown if the <see cref="DisposableManagedObjectBase.IsDisposed"/> property is true (because <seealso cref="DisposableManagedObjectBase.Dispose()" /> has been called previously).</exception>
        public void StopListeningForResponses()
        {
            ThrowIfDisposed();

            lock (_SendSocketSynchroniser)
            {
                if (_sendSockets != null)
                {
                    var sockets = _sendSockets.ToList();
                    _sendSockets = null;

                    foreach (var socket in sockets)
                    {
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
                lock (_BroadcastListenSocketSynchroniser)
                {
                    if (_BroadcastListenSocket != null)
                        _BroadcastListenSocket.Dispose();
                }

                lock (_SendSocketSynchroniser)
                {
                    if (_sendSockets != null)
                    {
                        var sockets = _sendSockets.ToList();
                        _sendSockets = null;

                        foreach (var socket in sockets)
                        {
                            socket.Dispose();
                        }
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private async Task SendMessageIfSocketNotDisposed(byte[] messageData, IpEndPointInfo destination)
        {
            var sockets = _sendSockets;
            if (sockets != null)
            {
                sockets = sockets.ToList();

                foreach (var socket in sockets)
                {
                    await socket.SendAsync(messageData, messageData.Length, destination).ConfigureAwait(false);
                }
            }

            ThrowIfDisposed();
        }

        private IUdpSocket ListenForBroadcastsAsync()
        {
            var socket = _SocketFactory.CreateUdpMulticastSocket(SsdpConstants.MulticastLocalAdminAddress, _MulticastTtl, SsdpConstants.MulticastPort);

            ListenToSocket(socket);

            return socket;
        }

        private List<IUdpSocket> CreateSocketAndListenForResponsesAsync()
        {
            var sockets = new List<IUdpSocket>();

            sockets.Add(_SocketFactory.CreateSsdpUdpSocket(IpAddressInfo.Any, _LocalPort));

            if (_enableMultiSocketBinding)
            {
                foreach (var address in _networkManager.GetLocalIpAddresses().ToList())
                {
                    try
                    {
                        sockets.Add(_SocketFactory.CreateSsdpUdpSocket(address, _LocalPort));
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error in CreateSsdpUdpSocket. IPAddress: {0}", ex, address);
                    }
                }
            }

            foreach (var socket in sockets)
            {
                ListenToSocket(socket);
            }

            return sockets;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "t", Justification = "Capturing task to local variable removes compiler warning, task is not otherwise required.")]
        private void ListenToSocket(IUdpSocket socket)
        {
            // Tasks are captured to local variables even if we don't use them just to avoid compiler warnings.
            var t = Task.Run(async () =>
            {
                var cancelled = false;
                while (!cancelled)
                {
                    try
                    {
                        var result = await socket.ReceiveAsync().ConfigureAwait(false);

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
            });
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

        private void ProcessMessage(string data, IpEndPointInfo endPoint, IpAddressInfo receivedOnLocalIpAddress)
        {
            //Responses start with the HTTP version, prefixed with HTTP/ while
            //requests start with a method which can vary and might be one we haven't 
            //seen/don't know. We'll check if this message is a request or a response
            //by checking for the static HTTP/ prefix on the start of the message.
            if (data.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
            {
                HttpResponseMessage responseMessage = null;
                try
                {
                    responseMessage = _ResponseParser.Parse(data);
                }
                catch (ArgumentException ex)
                {
                    // Ignore invalid packets.
                }

                if (responseMessage != null)
                    OnResponseReceived(responseMessage, endPoint);
            }
            else
            {
                HttpRequestMessage requestMessage = null;
                try
                {
                    requestMessage = _RequestParser.Parse(data);
                }
                catch (ArgumentException ex)
                {
                    // Ignore invalid packets.
                }

                if (requestMessage != null)
                {
                    OnRequestReceived(requestMessage, endPoint, receivedOnLocalIpAddress);
                }
            }
        }

        private void OnRequestReceived(HttpRequestMessage data, IpEndPointInfo remoteEndPoint, IpAddressInfo receivedOnLocalIpAddress)
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

        private void OnResponseReceived(HttpResponseMessage data, IpEndPointInfo endPoint)
        {
            var handlers = this.ResponseReceived;
            if (handlers != null)
                handlers(this, new ResponseReceivedEventArgs(data, endPoint));
        }

        #endregion

    }
}