using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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
        private IUdpSocket _SendSocket;

        private HttpRequestParser _RequestParser;
        private HttpResponseParser _ResponseParser;

        private ISocketFactory _SocketFactory;

        private int _LocalPort;
        private int _MulticastTtl;

        private bool _IsShared;

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
        /// <param name="socketFactory">An implementation of the <see cref="ISocketFactory"/> interface that can be used to make new unicast and multicast sockets. Cannot be null.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="socketFactory"/> argument is null.</exception>
        public SsdpCommunicationsServer(ISocketFactory socketFactory)
            : this(socketFactory, 0, SsdpConstants.SsdpDefaultMulticastTimeToLive)
        {
        }

        /// <summary>
        /// Full constructor.
        /// </summary>
        /// <param name="socketFactory">An implementation of the <see cref="ISocketFactory"/> interface that can be used to make new unicast and multicast sockets. Cannot be null.</param>
        /// <param name="localPort">The specific local port to use for all sockets created by this instance. Specify zero to indicate the system should choose a free port itself.</param>
        /// <param name="multicastTimeToLive">The multicast time to live value for multicast sockets. Technically this is a number of router hops, not a 'Time'. Must be greater than zero.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="socketFactory"/> argument is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">The <paramref name="multicastTimeToLive"/> argument is less than or equal to zero.</exception>
        public SsdpCommunicationsServer(ISocketFactory socketFactory, int localPort, int multicastTimeToLive)
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
        /// <exception cref="System.ArgumentNullException">Thrown if the <paramref name="messageData"/> argument is null.</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the <see cref="DisposableManagedObjectBase.IsDisposed"/> property is true (because <seealso cref="DisposableManagedObjectBase.Dispose()" /> has been called previously).</exception>
        public async Task SendMessage(byte[] messageData, IpEndPointInfo destination)
        {
            if (messageData == null) throw new ArgumentNullException("messageData");

            ThrowIfDisposed();

            EnsureSendSocketCreated();

            // SSDP spec recommends sending messages multiple times (not more than 3) to account for possible packet loss over UDP.
            for (var i = 0; i < SsdpConstants.UdpResendCount; i++)
            {
                await SendMessageIfSocketNotDisposed(messageData, destination).ConfigureAwait(false);

                await Task.Delay(100).ConfigureAwait(false);
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
                    IpAddress = new IpAddressInfo { Address = SsdpConstants.MulticastLocalAdminAddress },
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
                var socket = _SendSocket;
                _SendSocket = null;
                if (socket != null)
                    socket.Dispose();
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
                    if (_SendSocket != null)
                        _SendSocket.Dispose();
                }
            }
        }

        #endregion

        #region Private Methods

        private Task SendMessageIfSocketNotDisposed(byte[] messageData, IpEndPointInfo destination)
        {
            var socket = _SendSocket;
            if (socket != null)
            {
                return _SendSocket.SendAsync(messageData, messageData.Length, destination);
            }

            ThrowIfDisposed();
            return Task.FromResult(true);
        }

        private IUdpSocket ListenForBroadcastsAsync()
        {
            var socket = _SocketFactory.CreateUdpMulticastSocket(SsdpConstants.MulticastLocalAdminAddress, _MulticastTtl, SsdpConstants.MulticastPort);

            ListenToSocket(socket);

            return socket;
        }

        private IUdpSocket CreateSocketAndListenForResponsesAsync()
        {
            _SendSocket = _SocketFactory.CreateSsdpUdpSocket(_LocalPort);

            ListenToSocket(_SendSocket);

            return _SendSocket;
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
                            ProcessMessage(System.Text.UTF8Encoding.UTF8.GetString(result.Buffer, 0, result.ReceivedBytes), result.RemoteEndPoint);
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
            if (_SendSocket == null)
            {
                lock (_SendSocketSynchroniser)
                {
                    if (_SendSocket == null)
                    {
                        _SendSocket = CreateSocketAndListenForResponsesAsync();
                    }
                }
            }
        }

        private void ProcessMessage(string data, IpEndPointInfo endPoint)
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
                catch (ArgumentException) { } // Ignore invalid packets.

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
                catch (ArgumentException) { } // Ignore invalid packets.

                if (requestMessage != null)
                    OnRequestReceived(requestMessage, endPoint);
            }
        }

        private void OnRequestReceived(HttpRequestMessage data, IpEndPointInfo endPoint)
        {
            //SSDP specification says only * is currently used but other uri's might
            //be implemented in the future and should be ignored unless understood.
            //Section 4.2 - http://tools.ietf.org/html/draft-cai-ssdp-v1-03#page-11
            if (data.RequestUri.ToString() != "*") return;

            var handlers = this.RequestReceived;
            if (handlers != null)
                handlers(this, new RequestReceivedEventArgs(data, endPoint));
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