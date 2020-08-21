#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Emby.Dlna.Rssdp.EventArgs;
using Emby.Dlna.Rssdp.Parsers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Networking;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Logging;
using Mono.Nat;

namespace Emby.Dlna.Rssdp
{
    /// <summary>
    /// Provides the platform independent logic for publishing device existence and responding to search requests.
    ///
    /// Is designed to work in conjunction with ExternalPortForwarding.
    /// </summary>
    public class SocketServer : IDisposable
    {
        private const int UdpResendCount = 3;

        /* We could technically use one socket listening on port 1900 for everything. This should get both multicast (notifications)
         * and unicast (search response) messages, however this often doesn't work under Windows because the MS SSDP service is running.
         * If that service is running then it will steal the unicast messages and we will never see search responses.
         * Since stopping the service would be a bad idea (might not be allowed security wise and might break other apps running on
         * the system) the only other work around is to use two sockets.
         *
         * We use one socket to listen for/receive notifications and search requests.
         * We use a second socket, bound to a different local port, to send search requests and if depending upon the state of uPNP
         * listen for responses. The responses are sent to the local port this socket is bound to, which isn't port 1900 so the MS
         * service doesn't steal them. While the caller can specify a local port to use, we will default to 0 which allows the
         * underlying system to auto-assign a free port.
         */

        private readonly object _socketSynchroniser;
        private readonly HttpRequestParser _requestParser;
        private readonly HttpResponseParser _responseParser;
        private readonly ILogger<SocketServer> _logger;
        private readonly INetworkManager _networkManager;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly List<Socket> _sockets;
        private bool _disposed;
        private bool _oldState;

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketServer"/> class.
        /// </summary>
        /// <param name="networkManager">The networkManager<see cref="INetworkManager"/>.</param>
        /// <param name="configurationManager">The system configuration.</param>
        /// <param name="loggerFactory">The logger<see cref="ILogger"/>.</param>
        public SocketServer(
            INetworkManager networkManager,
            IServerConfigurationManager configurationManager,
            ILoggerFactory loggerFactory)
        {
            _configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            _logger = loggerFactory?.CreateLogger<SocketServer>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _socketSynchroniser = new object();
            _sockets = new List<Socket>();
            _requestParser = new HttpRequestParser();
            _responseParser = new HttpResponseParser();

            Instance = this;
            _oldState = _networkManager.IsuPnPActive;
            Tracing = configurationManager.GetDlnaConfiguration().EnableDebugLog;
            CreateSockets();

            _configurationManager.ConfigurationUpdated += ConfigurationUpdated;
            _configurationManager.NamedConfigurationUpdated += OnNamedConfigurationUpdated;
            _networkManager.NetworkChanged += NetworkChanged;
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
        /// Gets or sets the singleton instance of this object.
        /// </summary>
        public static SocketServer? Instance { get; set; }

        /// <summary>
        /// Gets a value indicating whether detailed DNLA debug logging is active.
        /// </summary>
        public bool Tracing { get; private set; }

        /// <inheritdoc/>
#pragma warning disable CA1063 // Implement IDisposable Correctly : UsageCount implementation causes warning.
        public void Dispose()
        {
            // If we still have delegates, then don't dispose as we're still in use.
            if (RequestReceived?.GetInvocationList().Length != 0)
            {
                return;
            }

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Triggered on a system configuration change.
        /// </summary>
        /// <param name="sender">Configuration object.</param>
        /// <param name="args">Event arguments.</param>
        public void ConfigurationUpdated(object sender, System.EventArgs args)
        {
            if (_oldState != _networkManager.IsuPnPActive)
            {
                CreateSockets();
                _oldState = _networkManager.IsuPnPActive;
            }
        }

        /// <summary>
        /// Sends a message to the SSDP multicast address and port.
        /// </summary>
        /// <param name="message">The mesage to send.</param>
        /// <param name="localIPAddress">The interface ip to use.</param>
        /// <param name="endPoint">The destination endpoint.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SendMessageAsync(string message, IPAddress localIPAddress, IPEndPoint endPoint)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            if (endPoint == null)
            {
                throw new ArgumentNullException(nameof(endPoint));
            }

            if (localIPAddress == null)
            {
                throw new ArgumentNullException(nameof(localIPAddress));
            }

            if (IsInvalid(localIPAddress, endPoint, false, false))
            {
                return;
            }

            // Calculate which sockets to send the message through.
            IEnumerable<Socket> sockets;
            List<Socket> sendSockets;

            lock (_socketSynchroniser)
            {
                sockets = _sockets.Where(i => i.LocalEndPoint.AddressFamily == localIPAddress.AddressFamily);

                if (sockets.Any())
                {
                    // Send from the Any socket and the socket with the matching address
                    if (localIPAddress.AddressFamily == AddressFamily.InterNetwork)
                    {
                        sockets = sockets.Where(i => i.LocalAddressEquals(IPAddress.Any) || localIPAddress.Equals(i.LocalAddress()));

                        // If sending to the loopback address, filter the socket list as well
                        if (endPoint.Address.Equals(IPAddress.Loopback))
                        {
                            sockets = sockets.Where(i => i.LocalAddressEquals(IPAddress.Any) || i.LocalAddressEquals(IPAddress.Loopback));
                        }
                    }
                    else if (localIPAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        sockets = sockets.Where(i => i.LocalAddressEquals(IPAddress.IPv6Any) || localIPAddress.Equals(i.LocalAddress()));

                        // If sending to the loopback address, filter the socket list as well
                        if (endPoint.Equals(IPAddress.IPv6Loopback))
                        {
                            sockets = sockets.Where(i => i.LocalAddressEquals(IPAddress.IPv6Any) || i.LocalAddressEquals(IPAddress.IPv6Loopback));
                        }
                    }
                }

                if (!sockets.Any())
                {
                    _logger.LogError("Unable to locate socket for {0}:{1}", localIPAddress, endPoint);
                    return;
                }

                // Make a copy so that changes can occurr in the dictionary whilst we're using this.
                sendSockets = new List<Socket>(sockets);
            }

            await SendFromSocketsAsync(sendSockets, message, endPoint, UdpResendCount).ConfigureAwait(false);
        }

        /// <summary>
        /// Multicasts a message to a particular address (unicast or multicast) via all available sockets.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="localIPAddress">The destination address.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SendMulticastMessageAsync(string message, IPAddress localIPAddress)
        {
            await SendMulticastMessageAsync(message, UdpResendCount, localIPAddress).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a message to the SSDP multicast address and port.
        /// </summary>
        /// <param name="message">The mesage to send.</param>
        /// <param name="sendCount">The number of times to send it.</param>
        /// <param name="localIPAddress">The interface ip to use.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SendMulticastMessageAsync(string message, int sendCount, IPAddress localIPAddress)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (localIPAddress == null)
            {
                throw new ArgumentNullException(nameof(localIPAddress));
            }

            if (IsInvalid(localIPAddress, null, false, false))
            {
                return;
            }

            // Get the correct FamilyAdddress endpoint.
            IPEndPoint endPoint = _networkManager.GetMulticastEndPoint(localIPAddress, 1900);

            List<Socket> sendSockets;
            lock (_socketSynchroniser)
            {
                sendSockets = new List<Socket>(_sockets
                    .Where(s => s.LocalAddressEquals(IPAddress.Any) || s.LocalAddressEquals(IPAddress.IPv6Any) || s.LocalAddressEquals(localIPAddress)
                    .Where(s => localIPAddress.AddressFamily == s.LocalEndPoint.AddressFamily));

                if (sendSockets.Count <= 0)
                {
                    return;
                }
            }

            await SendFromSocketsAsync(sendSockets, message, endPoint, sendCount).ConfigureAwait(false);
        }

        /// <summary>
        /// Processes a SSDP message.
        /// </summary>
        /// <param name="data">The data to process.</param>
        /// <param name="receivedFrom">The remote endpoint.</param>
        /// <param name="localIPAddress">The interface ip upon which it was receieved.</param>
        /// <param name="sourceInternal">True if the data didn't arrive through JF's UDP ports.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task ProcessMessage(string data, IPEndPoint receivedFrom, IPAddress localIPAddress, bool sourceInternal)
        {
            if (receivedFrom == null)
            {
                throw new ArgumentNullException(nameof(receivedFrom));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (localIPAddress == null)
            {
                throw new ArgumentNullException(nameof(localIPAddress));
            }

            if (IsInvalid(localIPAddress, receivedFrom, true, sourceInternal))
            {
                return Task.CompletedTask;
            }

            // _logger.LogDebug("Processing inbound SSDP from {0}.", endPoint.Address);
            try
            {
                // Responses start with the HTTP version, prefixed with HTTP/ while requests start with a method which can
                // vary and might be one we haven't seen/don't know. We'll check if this message is a request or a response
                // by checking for the HTTP/ prefix on the start of the message.
                if (data.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
                {
                    HttpResponseMessage? responseMessage = _responseParser.Parse(data);
                    if (responseMessage != null)
                    {
                        try
                        {
                            ResponseReceived?.Invoke(this, new ResponseReceivedEventArgs(responseMessage, receivedFrom, localIPAddress, sourceInternal));
                        }
                        finally
                        {
                            responseMessage?.Dispose();
                        }
                    }
                }
                else
                {
                    HttpRequestMessage? requestMessage = _requestParser.Parse(data);
                    if (requestMessage != null)
                    {
                        try
                        {
                            // SSDP specification says only * is currently used but other uri's might be implemented in the future
                            // and should be ignored unless understood.
                            // Section 4.2 - http://tools.ietf.org/html/draft-cai-ssdp-v1-03#page-11
                            if (requestMessage.RequestUri.ToString() != "*")
                            {
                                return Task.CompletedTask;
                            }

                            RequestReceived?.Invoke(this, new RequestReceivedEventArgs(data, requestMessage, receivedFrom, localIPAddress, sourceInternal));
                        }
                        finally
                        {
                            requestMessage?.Dispose();
                        }
                    }
                }
            }
            catch (ArgumentException)
            {
                // Ignore invalid packets.
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops listening for requests, disposes this instance and all internal relocalIPAddresss.
        /// </summary>
        /// <param name="disposing">True if objects are to be disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposed = true;
                Instance = null;

                _configurationManager.ConfigurationUpdated -= ConfigurationUpdated;
                _configurationManager.NamedConfigurationUpdated -= OnNamedConfigurationUpdated;
                _networkManager.NetworkChanged -= ConfigurationUpdated;
                NatUtility.UnknownDeviceFound -= UnknownDeviceFound;

                lock (_socketSynchroniser)
                {
                    Socket socket;
                    while (_sockets.Count > 0)
                    {
                        socket = _sockets[0];
                        _sockets.RemoveAt(0);
                        _logger.LogInformation("Disposing socket {1}", socket.LocalEndPoint);
                        socket.Dispose();
                    }

                    _sockets.Clear();
                }
            }
        }

        private void OnNamedConfigurationUpdated(object sender, ConfigurationUpdateEventArgs e)
        {
            if (string.Equals(e.Key, "dlna", StringComparison.OrdinalIgnoreCase))
            {
                Tracing = _configurationManager.GetDlnaConfiguration().EnableDebugLog;
            }
        }

        /// <summary>
        /// Triggered on a network change.
        /// </summary>
        /// <param name="sender">NetworkManager object.</param>
        /// <param name="args">Event arguments.</param>
        private void NetworkChanged(object sender, System.EventArgs args)
        {
            CreateSockets();
        }

        /// <summary>
        /// Checks to see if the networks where the traffic flows are valid.
        /// </summary>
        /// <param name="localIPAddress">IP address.</param>
        /// <param name="endPoint">Endpoint address.</param>
        /// <param name="matchSockets">Check the endpoint address and port against the sockets to see if it's from us.</param>
        /// <param name="sourceInternal">Was the message passed to us by Mono.NAT.</param>
        /// <returns>True if the communication is permitted.</returns>
        private bool IsInvalid(IPAddress localIPAddress, IPEndPoint? endPoint, bool matchSockets, bool sourceInternal = false)
        {
            bool isInterfaceAddress = false;

            // Is the remote endpoint outside the LAN?
            if (endPoint != null)
            {
                if (_networkManager.IsExcluded(endPoint.Address))
                {
                    return true;
                }

                isInterfaceAddress = _networkManager.IsValidInterfaceAddress(endPoint.Address);

                // Windows can use a random interface to talk to apps running on the same machine.
                if (isInterfaceAddress)
                {
                    return false;
                }

                if (!_networkManager.IsInLocalNetwork(endPoint.Address))
                {
                    _logger.LogDebug("FILTERED: Sending to non-LAN address: {0}.", endPoint.Address);
                    return true;
                }

                if (matchSockets)
                {
                    // Did we send this?
                    lock (_socketSynchroniser)
                    {
                        if (_sockets.Where(s => s.LocalEndPoint.Equals(endPoint)).Any())
                        {
                            _logger.LogDebug("FILTERED: Sending to Self: {0} -> {0}/{1}. uPnP?", localIPAddress, endPoint.Address, endPoint.Port);
                            return true;
                        }
                    }

                    // Did it come from Mono.NAT?
                    if (sourceInternal && _networkManager.IsuPnPActive && endPoint.Port == 1900 && _networkManager.IsGatewayInterface(endPoint.Address))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Transmits the message to the socket.
        /// </summary>
        /// <param name="sockets">Socket to use.</param>
        /// <param name="message">Message to transmit.</param>
        /// <param name="destination">Destination endpoint.</param>
        /// <param name="sendCount">Number of times to send it. SSDP spec recommends sending messages not more than 3 times
        /// to account for possible packet loss over UDP.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SendFromSocketsAsync(List<Socket> sockets, string message, IPEndPoint destination, int sendCount)
        {
            // Assuming each transmittion takes 5ms, reduce the delay accordingly.
            int delay = 100 - (sockets.Count * 5);

            byte[] messageData = Encoding.UTF8.GetBytes(message);

            while (sendCount-- > 0)
            {
                foreach (var socket in sockets)
                {
                    if (Tracing)
                    {
                        _logger.LogDebug("{0}->{1}:{2}", socket.LocalAddress(), destination.Address, message);
                    }

                    try
                    {
                        await socket.SendToAsync(messageData, SocketFlags.None, destination).ConfigureAwait(false);
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (SocketException ex)
                    {
                        _logger.LogError(ex, "Error sending socket message from {0} to {1}", socket.LocalEndPoint, destination);
                    }
                }

                await Task.Delay(delay).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Creates UDP port if one doesn't already exist for the ip address.
        /// </summary>
        /// <param name="localIPAddress">Interface IP upon which to listen.</param>
        /// <param name="port">Port upon which to listen. If the port number isn't zero, the socket is initialised for multicasts.</param>
        /// <returns>Success of the operation.</returns>
        private bool CreateUniqueSocket(IPAddress localIPAddress, int port = 0)
        {
            if (!_sockets.Where(k => k.LocalAddressEquals(localIPAddress)).Any())
            {
                try
                {
                    _logger.LogDebug("Creating socket for {0}.", localIPAddress);
                    _sockets.Add(_networkManager.CreateUdpMulticastSocket(localIPAddress, port));
                    return true;
                }
                catch (SocketException ex)
                {
                    _logger.LogError(ex, "Error creating socket {0} port {1}", localIPAddress, port);
                }
            }

            return false;
        }

        /// <summary>
        /// Creates and removes invalid sockets.
        /// </summary>
        private void CreateSockets()
        {
            var ba = _networkManager.GetInternalBindAddresses();

            lock (_socketSynchroniser)
            {
                if (_networkManager.EnableMultiSocketBinding && _sockets.Count > 0)
                {
                    // Rather than destroying all sockets and re-creating them, only destroy invalid ones.

                    List<Socket>? removeThese = null;
                    lock (_socketSynchroniser)
                    {
                        // Elaborate code as you cannot alter an enum whilst in an enum.
                        foreach (var socket in _sockets)
                        {
                            // If not an IPAny/v6Any and socket doesn't exist any more, then dispose of it.
                            IPAddress addr = socket.LocalAddress();
                            if (!addr.Equals(IPAddress.Any) && !addr.Equals(IPAddress.IPv6Any) && !ba.Exists(addr))
                            {
                                if (removeThese == null)
                                {
                                    removeThese = new List<Socket>();
                                }

                                removeThese.Add(socket);
                            }
                        }

                        if (removeThese != null)
                        {
                            foreach (var socket in removeThese)
                            {
                                _sockets.Remove(socket);
                                socket.Dispose();
                            }

                            removeThese.Clear();
                        }
                    }
                }

                // Only create the IPAny/v6Any and multicast ports once.
                if (_sockets.Count == 0)
                {
                    if (!CreateUniqueSocket(IPAddress.Any, 1900))
                    {
                        CreateUniqueSocket(IPAddress.Any);
                    }

                    if (_networkManager.IsIP6Enabled)
                    {
                        CreateUniqueSocket(IPAddress.IPv6Any);
                    }
                }

                if (_networkManager.EnableMultiSocketBinding)
                {
                    foreach (IPObject ip in ba)
                    {
                        CreateUniqueSocket(ip.Address);
                    }
                }
            }

            _ = StartListening();
        }

        /// <summary>
        /// Enables the SSDP injection of devices found by Mono.Nat.
        /// </summary>
        /// <param name="sender">Mono.Nat instance.</param>
        /// <param name="e">Information Mono received, but doesn't use.</param>
        private async void UnknownDeviceFound(object sender, DeviceEventUnknownArgs e)
        {
            // _logger.LogDebug("Mono.NAT passing information to our SSDP processor.");
            await ProcessMessage(e.Data, (IPEndPoint)e.EndPoint, e.Address, true).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the sockets listening tasks.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task StartListening()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            if (_networkManager.IsuPnPActive)
            {
                NatUtility.UnknownDeviceFound += UnknownDeviceFound;
            }
            else
            {
                NatUtility.UnknownDeviceFound -= UnknownDeviceFound;
            }

            try
            {
                List<Socket> sockets;
                lock (_socketSynchroniser)
                {
                    sockets = new List<Socket>(_sockets);
                }

                var tasks = sockets.Select(i => ListenToSocketAsync(i)).ToList();
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types: Catches unknown task exceptions and logs them.
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting Listeners.");
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        /// <summary>
        /// Async Listening Method. (One task per socket.)
        /// </summary>
        /// <param name="socket">Socket to listen to.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task ListenToSocketAsync(Socket socket)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            var receiveBuffer = ArrayPool<byte>.Shared.Rent(1024);
            IPEndPoint listeningOn = (IPEndPoint)socket.LocalEndPoint;
            IPEndPoint remote;
            string buffer;

            _logger.LogDebug("Listening on {0}/{1}", listeningOn.Address, listeningOn.Port);
            try
            {
                var endPoint = socket.LocalEndPoint;
                while (!_disposed)
                {
                    try
                    {
                        var result = await socket.ReceiveFromAsync(receiveBuffer, SocketFlags.None, endPoint).ConfigureAwait(false);

                        if (result.ReceivedBytes > 0)
                        {
                            remote = (IPEndPoint)result.RemoteEndPoint;
                            buffer = Encoding.UTF8.GetString(receiveBuffer, 0, result.ReceivedBytes);
                            if (Tracing)
                            {
                                _logger.LogDebug("{0}->{1}:{2}", remote.Address, listeningOn.Address, buffer);
                            }

                            await ProcessMessage(buffer, remote, socket.LocalAddress(), false).ConfigureAwait(false);
                        }
                    }
                    catch (Exception e) when (e is ObjectDisposedException || e is TaskCanceledException)
                    {
                        _logger.LogInformation("Listening shutting down. {0}", listeningOn);
                        break;
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
