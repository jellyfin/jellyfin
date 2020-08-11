using System;
using System.Buffers;
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
using Microsoft.Extensions.Logging;
using Rssdp.Events;
using Rssdp.Parsers;

namespace Rssdp.Infrastructure
{
    /// <summary>
    /// Provides the platform independent logic for publishing device existence and responding to search requests.
    ///
    /// Is designed to work in conjunction with ExternalPortForwarding.
    /// </summary>
    public class SocketServer : IDisposable
    {
        const int UdpResendCount = 3;

        /* We could technically use one socket listening on port 1900 for everything. This should get both multicast (notifications)
         * and unicast (search response) messages, however this often doesn't work under Windows because the MS SSDP service is running.
         * If that service is running then it will steal the unicast messages and we will never see search responses.
         * Since stopping the service would be a bad idea (might not be allowed security wise and might break other apps running on
         * the system) the only other work around is to use two sockets.
         *
         * We use one socket to listen for/receive notifications and search requests.
         * We use a second socket, bound to a different local port, to send search requests and if depending upon the state of uPNP listen for responses.
         * The responses are sent to the local port this socket is bound to, which isn't port 1900 so the MS service doesn't steal them.
         * While the caller can specify a local port to use, we will default to 0 which allows the underlying system to auto-assign a free port.
         */


        /// <summary>
        /// Multicast IP Address used for SSDP multicast messages.
        /// </summary>
        private readonly IPAddress _multicastLocalAdminAddress = IPAddress.Parse("239.255.255.250");

        /// <summary>
        /// Multicast IP6 Address used for SSDP multicast messages.
        /// </summary>
        private readonly IPAddress _multicastLocalAdminAddressV6 = IPAddress.Parse("ff01::2");
        private readonly object _socketSynchroniser;
        private readonly HttpRequestParser _requestParser;
        private readonly HttpResponseParser _responseParser;
        private readonly ILogger _logger;
        private readonly INetworkManager _networkManager;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly Dictionary<Socket, SocketState> _sockets;
        private readonly int _multicastTtl;
        private bool _externalPortForwardEnabled;
        private bool _disposed;

        // <summary>
        /// Initializes a new instance of the <see cref="SsdpCommunicationsServer"/> class.
        /// </summary>
        /// <param name="networkManager">The networkManager<see cref="INetworkManager"/>.</param>
        /// <param name="config">The system configuration.</param>
        /// <param name="logger">The logger<see cref="ILogger"/>.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="socketFactory"/> argument is null.</exception>
        public SocketServer(
            INetworkManager networkManager,
            IServerConfigurationManager configurationManager,
            ILoggerFactory loggerFactory)
        {
            _configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            _logger = loggerFactory?.CreateLogger<SocketServer>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _socketSynchroniser = new object();
            _sockets = new Dictionary<Socket, SocketState>();
            _requestParser = new HttpRequestParser();
            _responseParser = new HttpResponseParser();

            _multicastTtl = 4;
            _externalPortForwardEnabled = _configurationManager.Configuration.EnableUPnP && _configurationManager.Configuration.EnableRemoteAccess;

            CreateSockets();

            _configurationManager.ConfigurationUpdated += ConfigurationUpdated;
            _networkManager.NetworkChanged += NetworkChanged;
        }

        public int UsageCount { get; set; } = 1;

        /// <summary>
        /// Raised when a HTTPU request message is received by a socket (unicast or multicast).
        /// </summary>
        public event EventHandler<RequestReceivedEventArgs>? RequestReceived;

        /// <summary>
        /// Raised when an HTTPU response message is received by a socket (unicast or multicast).
        /// </summary>
        public event EventHandler<ResponseReceivedEventArgs>? ResponseReceived;

#pragma warning disable CA1063 // Implement IDisposable Correctly : UsageCpunt implementation causes warning.
        public void Dispose()
#pragma warning restore CA1063 // Implement IDisposable Correctly
        {
            if (UsageCount-- > 0)
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
        public void ConfigurationUpdated(object sender, EventArgs args)
        {
            // Check to see if uPNP has changed status.
            bool newValue = (_configurationManager.Configuration.EnableUPnP && _configurationManager.Configuration.EnableRemoteAccess);

            if (newValue != _externalPortForwardEnabled)
            {
                // If it has create the missing sockets.
                _externalPortForwardEnabled = newValue;
                CreateSockets();
            }
        }

        /// <summary>
        /// Triggered on a network change.
        /// </summary>
        /// <param name="sender">NetworkManager object.</param>
        /// <param name="args">Event arguments.</param>
        public void NetworkChanged(object sender, EventArgs args)
        {
            CreateSockets();
        }

        /// <summary>
        /// Sends a message to a particular address (unicast or multicast) via all available sockets.
        /// </summary>
        /// <param name="messageData">The message to send.</param>
        /// <param name="destination">The destination address.</param>
        /// <param name="localIp">The local endpoint address to use.</param>
        public async Task SendMessageAsync(byte[] messageData, IPEndPoint destination, IPAddress localIp)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

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

            IEnumerable<KeyValuePair<Socket, SocketState>> sockets;
            Dictionary<Socket, SocketState> sendSockets;

            lock (_socketSynchroniser)
            {

                sockets = _sockets.Where(i => i.Key.LocalEndPoint.AddressFamily == localIp.AddressFamily);

                if (sockets.Any())
                {
                    // Send from the Any socket and the socket with the matching address
                    if (localIp.AddressFamily == AddressFamily.InterNetwork)
                    {
                        sockets = sockets.Where(
                            i => ((IPEndPoint)i.Key.LocalEndPoint).Address.Equals(IPAddress.Any)
                            || localIp.Equals(((IPEndPoint)i.Key.LocalEndPoint).Address));

                        // If sending to the loopback address, filter the socket list as well
                        if (((IPEndPoint)destination).Address.Equals(IPAddress.Loopback))
                        {
                            sockets = sockets.Where(
                                i => ((IPEndPoint)i.Key.LocalEndPoint).Address.Equals(IPAddress.Any) ||
                                ((IPEndPoint)i.Key.LocalEndPoint).Address.Equals(IPAddress.Loopback));
                        }
                    }
                    else if (localIp.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        sockets = sockets.Where(
                            i => ((IPEndPoint)i.Key.LocalEndPoint).Address.Equals(IPAddress.IPv6Any) ||
                            ((IPEndPoint)i.Key.LocalEndPoint).Address.Equals(i.Key.LocalEndPoint));

                        // If sending to the loopback address, filter the socket list as well
                        if (((IPEndPoint)destination).Equals(IPAddress.IPv6Loopback))
                        {
                            sockets = sockets.Where(
                                i => ((IPEndPoint)i.Key.LocalEndPoint).Address.Equals(IPAddress.IPv6Any) ||
                                ((IPEndPoint)i.Key.LocalEndPoint).Address.Equals(IPAddress.IPv6Loopback));
                        }
                    }
                }

                if (!sockets.Any())
                {
                    _logger.LogError("Unable to locate or create socket for {0}:{1}", localIp, destination);
                    return;
                }

                // Make a copy so that changes can occurr in the dictionary whilst we're using this.
                sendSockets = new Dictionary<Socket, SocketState>(sockets);
            }

            var tasks = sendSockets.Select(i => SendFromSocketAsync(i.Key, messageData, destination, UdpResendCount));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Transmits the message to the socket.
        /// </summary>
        /// <param name="socket">Socket to use.</param>
        /// <param name="messageData">Message to transmit.</param>
        /// <param name="destination"></param>
        /// <param name="sendCount">Number of times to send it. SSDP spec recommends sending messages not more than 3 times
        /// to account for possible packet loss over UDP.</param>
        private async Task SendFromSocketAsync(Socket socket, byte[] messageData, IPEndPoint destination, int sendCount)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            try
            {
                while (sendCount-- > 0)
                {
                    await socket.SendToAsync(messageData, SocketFlags.None, destination).ConfigureAwait(false);
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Error sending socket message from {0} to {1}", socket.LocalEndPoint, destination);
            }
        }

        public async Task SendMulticastMessageAsync(string message, IPAddress from)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            await SendMulticastMessageAsync(message, UdpResendCount, from).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a message to the SSDP multicast address and port.
        /// </summary>x
        public async Task SendMulticastMessageAsync(string message, int sendCount, IPAddress from)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            byte[] messageData = Encoding.UTF8.GetBytes(message);

            IPEndPoint ep = new IPEndPoint(from.AddressFamily == AddressFamily.InterNetwork ? _multicastLocalAdminAddress : _multicastLocalAdminAddressV6, 1900);

            // SSDP spec recommends sending messages multiple times (not more than 3) to account for possible packet loss over UDP.
            await SendMessageViaAllSocketsAsync(messageData, ep, from, sendCount).ConfigureAwait(false);
        }

        /// <summary>
        /// Stops listening for requests, disposes this instance and all internal resources.
        /// </summary>
        /// <param name="disposing">True if objects are to be disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposed = true;
                _configurationManager.ConfigurationUpdated -= ConfigurationUpdated;
                _networkManager.NetworkChanged -= ConfigurationUpdated;
                lock (_socketSynchroniser)
                {
                    if (_sockets.Count > 0)
                    {
                        foreach (var (socket, state) in _sockets)
                        {
                            _logger.LogInformation("{0} disposing socket {1}", this.GetType().Name, socket.LocalEndPoint);
                            socket.Dispose();
                        }
                    }
                    _sockets.Clear();
                }
            }
        }

        /// <summary>
        /// Sends the same message out to all sockets in @sockets.
        /// </summary>
        /// <param name="messageData">Message to transmit.</param>
        /// <param name="destination">Destination endpoint.</param>
        /// <param name="localIp">Source endpoint to use.</param>
        /// <param name="sendCount">Number of times to transmit.</param>
        private async Task SendMessageViaAllSocketsAsync(byte[] messageData, IPEndPoint destination, IPAddress localIp, int sendCount)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            Dictionary<Socket, SocketState>.KeyCollection sockets;
            lock (_socketSynchroniser)
            {
                sockets = _sockets.Keys;
            }

            if (sockets.Count > 0)
            {
                var tasks = sockets
                    .Where(s => (localIp.Equals(IPAddress.Any) || localIp.Equals(IPAddress.IPv6Any) || localIp.Equals(s.LocalEndPoint)))
                    .Where(s => (((IPEndPoint)destination).Address.AddressFamily == s.LocalEndPoint.AddressFamily))
                    .Select(s => SendFromSocketAsync(s, messageData, destination, sendCount));
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Creates UDP port if the endpoint doesn't already have one defined.
        /// </summary>
        /// <param name="listen">True if the socket is to be a listener.</param>
        /// <param name="ip">Interface IP upon which to listen.</param>
        /// <param name="port">Port upon which to listen. If the port number isn't zero, the socket is initialised for multicasts.</param>
        private void CreateUniqueSocket(bool listen, IPAddress ip, int port = 0)
        {
            var sockets = _sockets.Keys.Where(k => ((IPEndPoint)k.LocalEndPoint).Address.Equals(ip));
            if (!sockets.Any())
            {
                try
                {
                    Socket socket;
                    if (port == 0)
                    {
                        _logger.LogDebug("Creating socket for {0}.", ip);
                        socket = _networkManager.CreateSsdpUdpSocket(ip, port);
                    }
                    else
                    {
                        _logger.LogDebug("Creating multicast socket for {0} on {1}.", ip, port);
                        socket = _networkManager.CreateUdpMulticastSocket(_multicastTtl, port);
                    }

                    _sockets.Add(socket, listen ? SocketState.Listener : SocketState.SendOnly);
                }
                catch (SocketException ex)
                {
                    _logger.LogError(ex, "Error creating socket {0} port {1}", ip, port);
                }
            }
            else
            {
                // Have to work on a copy to avoid "Collection was modified; enumeration operation may not execute." exception.
                var matches = new List<Socket>(sockets);

                // For all matching ports, ensure that they are in the correct state. (Sendonly/listener).
                foreach (var socket in matches)
                {
                    SocketState state = _sockets[socket];
                    if (state == SocketState.Listening && !listen)
                    {
                        // As we are using ReceiveFromAsync, the only way to break out of the method is to close the socket.
                        _logger.LogDebug("Closing listening socket on {0}", ((IPEndPoint)socket.LocalEndPoint).Address);
                        _sockets.Remove(socket);
                        socket.Dispose();

                        // As we have to closed this port - we'll need another one creating.
                        CreateUniqueSocket(listen, ip, port);
                    }
                    else if (state == SocketState.SendOnly && listen)
                    {
                        // Tag the socket so that a listener task will be launched.
                        _sockets[socket] = SocketState.Listener;
                    }
                }
            }
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
                    var sockets = _sockets.Keys;
                    foreach (var (socket, state) in _sockets)
                    {
                        // If not an IPAny/v6Any and socket doesn't exist any more, then dispose of it.
                        IPEndPoint addr = ((IPEndPoint)socket.LocalEndPoint);
                        if (!addr.Address.Equals(IPAddress.Any) &&
                            !addr.Address.Equals(IPAddress.IPv6Any) &&
                            !ba.Exists(addr.Address))
                        {
                            _sockets.Remove(socket);
                            socket.Dispose();
                        }
                    }
                }

                // Only create the IPAny/v6Any and multicast ports once.
                if (_sockets.Count == 0)
                {
                    CreateUniqueSocket(true, IPAddress.Any, 1900); // Multicast.
                    CreateUniqueSocket(true, IPAddress.Any);
                    if (_networkManager.IsIP6Enabled)
                    {
                        CreateUniqueSocket(true, IPAddress.IPv6Any);
                    }
                }

                if (_networkManager.EnableMultiSocketBinding)
                {
                    foreach (IPObject ip in ba)
                    {
                        // An interface with a negative tag has a gateway address and so will be listened to by Mono.NAT, if port forwarding is enabled.
                        // In this instance, Mono.NAT will send us this traffic, so we don't need to listen to these sockets.
                        // Mono isn't IPv6 compliant yet, so we will listen on IPv6 interfaces
                        CreateUniqueSocket(!(_externalPortForwardEnabled && ip.Tag < 0 && ip.Address.AddressFamily == AddressFamily.InterNetwork), ip.Address);
                    }
                }
            }

            _ = StartListening();
        }

        /// <summary>
        /// Creates the sockets listening tasks.
        /// </summary>
        private async Task StartListening()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            try
            {
                Dictionary<Socket, SocketState> sockets;
                lock (_socketSynchroniser)
                {
                    // Get all the listener sockets in a separate dictionary.
                    // This must be done, otherwise an IEnumerable exception is generated.
                    sockets = new Dictionary<Socket, SocketState>(_sockets.Where(i => i.Value == SocketState.Listener));
                }

                var tasks = sockets.Select(i => ListenToSocketAsync(i.Key)).ToList();
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types: Catches unknown task exceptions and logs them.
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.LogError(ex, "Error starting Listeners.");
            }
        }

        /// <summary>
        /// Async Listening Method.
        /// </summary>
        /// <param name="socket">Socket to listen to.</param>
        private async Task ListenToSocketAsync(Socket socket)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            var receiveBuffer = ArrayPool<byte>.Shared.Rent(8192);

            IPEndPoint listeningOn = (IPEndPoint)socket.LocalEndPoint;
            _logger.LogDebug("Listening on {0}/{1}", listeningOn.Address, listeningOn.Port);

            try
            {
                // Update our status to show that we are now actively listening.
                lock (_socketSynchroniser)
                {
                    _sockets[socket] = SocketState.Listening;
                }

                while (!_disposed)
                {
                    try
                    {
                        var result = await socket.ReceiveFromAsync(receiveBuffer, SocketFlags.None, new IPEndPoint(IPAddress.Any, 0)).ConfigureAwait(false);

                        if (result.ReceivedBytes > 0)
                        {
                            var endpoint = (IPEndPoint)result.RemoteEndPoint;
                            if (!_networkManager.IsInLocalNetwork(endpoint.Address))
                            {
                                _logger.LogDebug("SSDP filtered from non-LAN address {0}.", endpoint.Address);
                                return;
                            }

                            await ProcessMessage(
                                System.Text.UTF8Encoding.UTF8.GetString(receiveBuffer, 0, result.ReceivedBytes),
                                endpoint,
                                ((IPEndPoint)socket.LocalEndPoint).Address
                                ).ConfigureAwait(false);
                        }

                        await Task.Delay(10).ConfigureAwait(false);
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


        /// <inheritdoc/>
        public Task ProcessMessage(string data, IPEndPoint endPoint, IPAddress localIp)
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

            var epAddr = ((IPEndPoint)endPoint).Address;
            if (!_networkManager.IsInLocalNetwork(epAddr))
            {
                _logger.LogDebug("SSDP filtered from sending to non-LAN address {0}.", epAddr);
                return Task.CompletedTask;
            }

            if (!localIp.Equals(IPAddress.Any) && !localIp.Equals(IPAddress.IPv6Any))
            {
                if (!_networkManager.IsInLocalNetwork(localIp))
                {
                    _logger.LogDebug("SSDP filtered due to arrive on a non LAN interface {0}.", localIp);
                    return Task.CompletedTask;
                }
            }

            // _logger.LogDebug("Processing inbound SSDP from {0}.", endPoint.Address);

            // Responses start with the HTTP version, prefixed with HTTP/ while requests start with a method which can
            // vary and might be one we haven't seen/don't know. We'll check if this message is a request or a response
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
                    ResponseReceived?.Invoke(this, new ResponseReceivedEventArgs(responseMessage, endPoint, localIp));
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
                    // SSDP specification says only * is currently used but other uri's might be implemented in the future
                    // and should be ignored unless understood.
                    // Section 4.2 - http://tools.ietf.org/html/draft-cai-ssdp-v1-03#page-11
                    if (requestMessage.RequestUri.ToString() != "*")
                    {
                        return Task.CompletedTask;
                    }

                    RequestReceived?.Invoke(this, new RequestReceivedEventArgs(requestMessage, endPoint, localIp));
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Represents the states of the Sockets.
        /// </summary>
        private enum SocketState
        {
            SendOnly,
            Listener,
            Listening
        }
    }
}
