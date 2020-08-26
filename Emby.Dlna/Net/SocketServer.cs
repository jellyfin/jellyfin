#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Emby.Dlna.Configuration;
using Emby.Dlna.Net.EventArgs;
using Emby.Dlna.Net.Parsers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Networking;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging;
using Mono.Nat;

namespace Emby.Dlna.Net
{
    /// <summary>
    /// Provides the platform independent logic for publishing device existence and responding to search requests.
    ///
    /// Is designed to work in conjunction with ExternalPortForwarding.
    ///
    /// Lazy implementation. Socks will only be created at first use.
    /// </summary>
    public class SocketServer
    {
        /// <summary>
        /// Default ttl.
        /// </summary>
        public const int DefaultMulticastTimeToLive = 4;

        private static SocketServer? _instance;
        private readonly object _socketSynchroniser;
        private readonly HttpRequestParser _requestParser;
        private readonly HttpResponseParser _responseParser;
        private readonly ILogger<SocketServer> _logger;
        private readonly INetworkManager _networkManager;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly IServerApplicationHost _appHost;
        private readonly List<Socket> _sockets;
        private readonly IPAddress _anyAddressIP4;
        private readonly IPAddress _anyAddressIP6;
        private ServerConfiguration _serverConfiguration;
        private DlnaOptions _options;
        private bool _oldState;
        private bool _running;

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketServer"/> class.
        /// </summary>
        /// <param name="networkManager">The networkManager<see cref="INetworkManager"/>.</param>
        /// <param name="configurationManager">The system configuration.</param>
        /// <param name="loggerFactory">The logger factory instance.<see cref="ILoggerFactory"/>.</param>
        /// <param name="appHost">The application host.</param>
        public SocketServer(
            INetworkManager networkManager,
            IServerConfigurationManager configurationManager,
            ILoggerFactory loggerFactory,
            IServerApplicationHost appHost)
        {
            _appHost = appHost;
            _configurationManager = configurationManager ?? throw new NullReferenceException(nameof(configurationManager));
            _logger = loggerFactory.CreateLogger<SocketServer>();
            _networkManager = networkManager;
            _socketSynchroniser = new object();
            _sockets = new List<Socket>();
            _requestParser = new HttpRequestParser();
            _responseParser = new HttpResponseParser();
            _serverConfiguration = configurationManager.Configuration;
            _options = configurationManager.GetConfiguration<DlnaOptions>("dlna");
            _anyAddressIP4 = AnyIP(IPAddress.Any);
            _anyAddressIP6 = AnyIP(IPAddress.IPv6Any);
            Instance = this;
            _oldState = IsUPnPActive;
            configurationManager.ConfigurationUpdated += OnConfigurationUpdated;
        }

        /// <summary>
        /// Raised when a HTTPU request message is received by a socket (unicast or multicast).
        /// </summary>
        public event EventHandler<RequestReceivedEventArgs>? RequestReceived
        {
            add
            {
                lock (_socketSynchroniser)
                {
                    EventRequestReceived += value;
                    Start();
                }
            }

            remove
            {
                lock (_socketSynchroniser)
                {
                    EventRequestReceived -= value;
                    Stop();
                }
            }
        }

        /// <summary>
        /// Raised when an HTTPU response message is received by a socket (unicast or multicast).
        /// </summary>
        public event EventHandler<ResponseReceivedEventArgs>? ResponseReceived
        {
            add
            {
                lock (_socketSynchroniser)
                {
                    EventResponseReceived += value;
                    Start();
                }
            }

            remove
            {
                lock (_socketSynchroniser)
                {
                    EventResponseReceived -= value;
                    Stop();
                }
            }
        }

        private event EventHandler<RequestReceivedEventArgs>? EventRequestReceived;

        private event EventHandler<ResponseReceivedEventArgs>? EventResponseReceived;

        /// <summary>
        /// Gets or sets the singleton instance of this object.
        /// </summary>
        public static SocketServer Instance
        {
            get => GetInstance();

            set
            {
                _instance = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether uPNP port forwarding is active.
        /// </summary>
        public bool IsUPnPActive => _serverConfiguration.EnableUPnP &&
            _serverConfiguration.EnableRemoteAccess &&
            (_appHost.ListenWithHttps || (!_appHost.ListenWithHttps && _serverConfiguration.UPnPCreateHttpPortMap));

        /// <summary>
        /// Gets the number of times each udp packet should be sent.
        /// </summary>
        public int UDPSendCount { get => _options.UDPSendCount; }

        /// <summary>
        /// Gets a value indicating whether is multi-socket binding available.
        /// </summary>
        public bool EnableMultiSocketBinding => _serverConfiguration.EnableMultiSocketBinding;

        /// <summary>
        /// Gets a value indicating whether detailed DNLA debug logging is active.
        /// </summary>
        public bool Tracing => _options.EnableDebugLog;

        /// <summary>
        /// Gets a value indicating whether IP6 is enabled.
        /// </summary>
        public bool IsIP6Enabled
        {
            get
            {
                return Socket.OSSupportsIPv6 && _serverConfiguration.EnableIPV6;
            }
        }

        /// <summary>
        /// Gets a value indicating whether IP4 is enabled.
        /// </summary>
        public bool IsIP4Enabled
        {
            get
            {
                return Socket.OSSupportsIPv4 && _serverConfiguration.EnableIPV4;
            }
        }

        /// <summary>
        /// Parses a string and returns a range value if possible.
        /// </summary>
        /// <param name="rangeStr">String to parse.</param>
        /// <param name="range">Range value contained in rangeStr.</param>
        /// <returns>Result of the operation.</returns>
        public static bool TryParseRange(string rangeStr, out (int Min, int Max) range)
        {
            if (string.IsNullOrEmpty(rangeStr))
            {
                range.Min = range.Max = 0; // Random Port.
                return false;
            }

            // Remove all white space.
            rangeStr = Regex.Replace(rangeStr, @"\s+", string.Empty);

            var i = rangeStr.IndexOf('-', StringComparison.OrdinalIgnoreCase);
            if (i != -1)
            {
                int minVal = int.TryParse(rangeStr.Substring(0, i), out int min) ? min : 1;
                int maxVal = int.TryParse(rangeStr.Substring(i + 1), out int max) ? max : 65535;
                if (minVal < 1)
                {
                    minVal = 1;
                }

                if (maxVal > 65535)
                {
                    maxVal = 65535;
                }

                range.Max = Math.Max(minVal, maxVal);
                range.Min = Math.Min(minVal, maxVal);
                return true;
            }

            if (int.TryParse(rangeStr, out int start))
            {
                if (start < 1 || start > 65535)
                {
                    start = 0; // Random Port.
                }

                range.Min = range.Max = start;
                return true;
            }

            // Random Port.
            range.Min = range.Max = 0;
            return false;
        }

        /// <summary>
        /// Returns an unused UDP port number in the range specified.
        /// </summary>
        /// <param name="range">Upper and Lower boundary of ports to select.</param>
        /// <returns>System.Int32.</returns>
        public static int GetUdpPortFromRange((int Min, int Max) range)
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();

            // Get active udp listeners.
            var udpListenerPorts = properties.GetActiveUdpListeners()
                        .Where(n => n.Port >= range.Min && n.Port <= range.Max)
                        .Select(n => n.Port);

            return Enumerable.Range(range.Min, range.Max)
                .Where(i => !udpListenerPorts.Contains(i))
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets a random port number that is currently available.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public static int GetRandomUnusedUdpPort()
        {
            // Get a port from the dynamic range.
            return GetUdpPortFromRange((49152, 65535));
        }

        /// <summary>
        /// Returns the correct multicast address based upon the value of the address provided.
        /// </summary>
        /// <param name="localIPAddress">IP address to use for comparison.</param>
        /// <param name="port">Port to use.</param>
        /// <returns>IPEndpoint set to the port provided.</returns>
        public static IPEndPoint GetMulticastEndPoint(IPAddress localIPAddress, int port)
        {
            if (localIPAddress == null)
            {
                throw new ArgumentNullException(nameof(localIPAddress));
            }

            return new IPEndPoint(
                localIPAddress.AddressFamily == AddressFamily.InterNetwork ?
                    IPNetAddress.MulticastIPv4 : localIPAddress.IsIPv6LinkLocal ?
                        IPNetAddress.MulticastIPv6LinkLocal : IPNetAddress.MulticastIPv6SiteLocal, port);
        }

        /// <summary>
        /// Returns a udp port based upon Configuration.UDPPort.
        /// </summary>
        /// <param name="portStr">Port Range, or empty/zero for a random port.</param>
        /// <returns>System.Int32.</returns>
        public int GetPort(string portStr)
        {
            int port = 0;
            if (TryParseRange(portStr, out (int Min, int Max) range))
            {
                port = GetUdpPortFromRange(range);
            }

            if (port < 0 || port > 65535)
            {
                _logger.LogError("UDP port in the range {0} cannot be allocated. Assigning random.", portStr);
                port = 0;
            }

            if (port == 0)
            {
                port = GetRandomUnusedUdpPort();
            }

            return port;
        }

        /// <summary>
        /// Creates an UDP Socket.
        /// </summary>
        /// <param name="port">UDP port to bind.</param>
        /// <returns>A Socket.</returns>
        public Socket CreateUdpBroadcastSocket(int port)
        {
            if (port < 0 || port > 65536)
            {
                throw new ArgumentException("Port out of range", nameof(port));
            }

            IPAddress address = IsIP6Enabled ? IPAddress.IPv6Any : IPAddress.Any;
            Socket retVal = PrepareSocket(address);
            try
            {
                retVal.Bind(new IPEndPoint(address, port));
            }
            catch
            {
                retVal?.Dispose();
                throw;
            }

            return retVal;
        }

        /// <summary>
        /// Creates a new UDP acceptSocket that is a member of the SSDP multicast local admin group and binds it to the specified local port.
        /// </summary>
        /// <param name="address">IP Address to bind.</param>
        /// <param name="port">UDP port to bind.</param>
        /// <returns>A Socket.</returns>
        public Socket CreateUdpMulticastSocket(IPAddress address, int port)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (port < 0 || port > 65536)
            {
                throw new ArgumentException("Port out of range", nameof(port));
            }

            Socket retVal = PrepareSocket(address);
            try
            {
                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, DefaultMulticastTimeToLive);
                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);
            }
            catch (SocketException ex)
            {
                _logger.LogDebug(ex, "Error setting multicast values on socket. {0}/{1}", address, port);
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                IPv6MulticastOption opt = new IPv6MulticastOption(
                    address.IsIPv6LinkLocal ?
                    IPNetAddress.MulticastIPv6LinkLocal : IPNetAddress.MulticastIPv6SiteLocal,
                    address.ScopeId);

                retVal.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, opt);
            }
            else
            {
                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPNetAddress.MulticastIPv4, address));
            }

            try
            {
                retVal.Bind(new IPEndPoint(address, port));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to bind to {0}/{1}.", address, port);
                retVal?.Dispose();
                throw;
            }

            return retVal;
        }

        /// <summary>
        /// Returns the correct multicast address based upon the IsIPEnabled.
        /// </summary>
        /// <param name="port">Port to use.</param>
        /// <returns>IPEndpoint set to the port provided.</returns>
        public IPEndPoint GetMulticastEndPoint(int port)
        {
            IPAddress addr = IsIP6Enabled ? IPAddress.IPv6Any : IPAddress.Any;
            return new IPEndPoint(
                addr.AddressFamily == AddressFamily.InterNetwork ?
                    IPNetAddress.MulticastIPv4 : addr.IsIPv6LinkLocal ?
                        IPNetAddress.MulticastIPv6LinkLocal : IPNetAddress.MulticastIPv6SiteLocal, port);
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
            var sendSockets = GetSendSockets(localIPAddress);
            if (!sendSockets.Any())
            {
                _logger.LogError("Unable to locate socket for {0}:{1}", localIPAddress, endPoint.Address);
                return;
            }

            await SendFromSocketsAsync(sendSockets, message, endPoint, _options.UDPSendCount, false).ConfigureAwait(false);
        }

        /// <summary>
        /// Multicasts a message to a particular address (unicast or multicast) via all available sockets.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="localIPAddress">The interface ip address to use.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SendMulticastMessageAsync(string message, IPAddress localIPAddress)
        {
            await SendMulticastMessageAsync(message, _options.UDPSendCount, localIPAddress).ConfigureAwait(false);
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
            if (localIPAddress == null)
            {
                throw new ArgumentNullException(nameof(localIPAddress));
            }

            var sendSockets = GetSendSockets(localIPAddress);

            if (sendSockets.Count <= 0)
            {
                _logger.LogError("No socket found for {0}", localIPAddress);
                return;
            }

            // Get the correct FamilyAdddress endpoint.
            IPEndPoint endPoint = GetMulticastEndPoint(localIPAddress, 1900);
            await SendFromSocketsAsync(sendSockets, message, endPoint, sendCount, true).ConfigureAwait(false);
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

            // Responses start with the HTTP version, prefixed with HTTP/ while requests start with a method which can
            // vary and might be one we haven't seen/don't know. We'll check if this message is a request or a response
            // by checking for the HTTP/ prefix on the start of the message.
            if (data.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
            {
                HttpResponseMessage msg;
                if (EventResponseReceived == null)
                {
                    // If no events then don't bother processing.
                    return Task.CompletedTask;
                }

                try
                {
                    msg = _responseParser.Parse(data);
                    EventResponseReceived.Invoke(this, new ResponseReceivedEventArgs(msg, receivedFrom, localIPAddress, sourceInternal));
                }
                catch (ArgumentException)
                {
                    // Ignore invalid packets.
                }
            }
            else
            {
                HttpRequestMessage msg;
                if (EventRequestReceived == null)
                {
                    // If no events then don't bother processing.
                    return Task.CompletedTask;
                }

                try
                {
                    msg = _requestParser.Parse(data);

                    // SSDP specification says only * is currently used but other uri's might be implemented in the future
                    // and should be ignored unless understood.
                    // Section 4.2 - http://tools.ietf.org/html/draft-cai-ssdp-v1-03#page-11
                    if (msg.RequestUri.ToString() != "*")
                    {
                        return Task.CompletedTask;
                    }

                    EventRequestReceived.Invoke(this, new RequestReceivedEventArgs(data, msg, receivedFrom, localIPAddress, sourceInternal));
                }
                catch (ArgumentException)
                {
                    // Ignore invalid packets.
                }
            }

            return Task.CompletedTask;
        }

        private static SocketServer GetInstance()
        {
            if (_instance == null)
            {
                throw new ApplicationException("SocketServer is not initialised.");
            }

            return _instance;
        }

        private void OnConfigurationUpdated(object sender, System.EventArgs e)
        {
            _serverConfiguration = _configurationManager.Configuration;
            _options = _configurationManager.GetConfiguration<DlnaOptions>("dlna");
        }

        private List<Socket> GetSendSockets(IPAddress localIPAddress)
        {
            // If IP dual mode is enabled, then IPAddress.Any and IPAddress.
            // Any are not used internally in Socket, hence the use of _anyAddressIP4 and _anyAddressIP6.
            lock (_socketSynchroniser)
            {
                IEnumerable<Socket> sockets;

                if (IsIP4Enabled)
                {
                    if (IsIP6Enabled)
                    {
                        // Send from IP4 or IP6 sockets where they are ANY socket and the socket with the matching address.
                        sockets = _sockets.Where(s =>
                                        (localIPAddress.AddressFamily == AddressFamily.InterNetwork && EndPointEquals(s, _anyAddressIP4)) ||
                                        (localIPAddress.AddressFamily == AddressFamily.InterNetworkV6 && EndPointEquals(s, _anyAddressIP6)) ||
                                        EndPointEquals(s, localIPAddress, true));
                    }
                    else
                    {
                        // Send from IP4 sockets where they are ANY socket and the socket with the matching address.
                        sockets = _sockets.Where(s =>
                                        (localIPAddress.AddressFamily == AddressFamily.InterNetwork && EndPointEquals(s, _anyAddressIP4)) ||
                                        EndPointEquals(s, localIPAddress, true));
                    }
                }
                else
                {
                    // Send from IP6 sockets where they are ANY socket and the socket with the matching address.
                    sockets = _sockets.Where(s =>
                                    (localIPAddress.AddressFamily == AddressFamily.InterNetworkV6 && EndPointEquals(s, _anyAddressIP6)) ||
                                    EndPointEquals(s, localIPAddress, true));
                }

                return new List<Socket>(sockets);
            }
        }

        /// <summary>
        /// Triggered on a system configuration change.
        /// </summary>
        /// <param name="sender">Configuration object.</param>
        /// <param name="args">Event arguments.</param>
        private void ConfigurationUpdated(object sender, System.EventArgs args)
        {
            bool lastState = _oldState;
            _oldState = IsUPnPActive;
            if (_oldState != lastState)
            {
                CreateSockets();
                // _udpResendCount = UDPResentCount;
            }
        }

        /// <summary>
        /// Creates a socket for use.
        /// </summary>
        /// <param name="address">Address of socket.</param>
        /// <returns>Socket instance.</returns>
        private Socket PrepareSocket(IPAddress address)
        {
            Socket retVal;

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                retVal = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }
            else
            {
                // IPv6 is enabled so create a dual IP4/IP6 socket
                retVal = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                if (IsIP4Enabled)
                {
                    retVal.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 0);
                    retVal.DualMode = true;
                }
            }

            try
            {
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            }
            catch (SocketException ex)
            {
                _logger.LogWarning(ex, "Error setting socket as reusable. {0}", address);
            }

            try
            {
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
            }
            catch (SocketException ex)
            {
                _logger.LogWarning(ex, "Error setting socket as non exclusive. {0}", address);
            }

            if (IsIP4Enabled)
            {
                try
                {
                    retVal.EnableBroadcast = true;
                    retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                }
                catch (SocketException ex)
                {
                    _logger.LogWarning(ex, "Error enabling broadcast on socket. {0}", address);
                }
            }

            return retVal;
        }

        private void Start()
        {
            if (!_running)
            {
                CreateSockets();

                _configurationManager.ConfigurationUpdated += ConfigurationUpdated;
                _configurationManager.NamedConfigurationUpdated += OnNamedConfigurationUpdated;
                _networkManager.NetworkChanged += NetworkChanged;
                _running = true;
            }
        }

        private void Stop()
        {
            if (_running)
            {
                _configurationManager.ConfigurationUpdated -= ConfigurationUpdated;
                _configurationManager.NamedConfigurationUpdated -= OnNamedConfigurationUpdated;
                _networkManager.NetworkChanged += NetworkChanged;
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

                _running = false;
            }
        }

        /// <summary>
        /// Compares a socket's LocalEndPoint with the ipaddress provided, taking into consideration the IP protocols enabled,
        /// and IP4 mapping.
        /// </summary>
        /// <param name="socket">The socket instance to use.</param>
        /// <param name="other">The IPAddress to compare to.</param>
        /// <param name="ip4Mapping">True to check IP4 to IP6 mapping.</param>
        /// <returns>True if both addresses are equal and the IP protocol conditions are met.</returns>
        private bool EndPointEquals(Socket socket, IPAddress other, bool ip4Mapping = false)
        {
            if (!IsIP4Enabled && other.AddressFamily == AddressFamily.InterNetwork)
            {
                return false;
            }

            if (!IsIP6Enabled && other.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return false;
            }

            IPAddress address = ((IPEndPoint)socket.LocalEndPoint).Address;
            var addressFamily = address.AddressFamily;
            var otherFamily = other.AddressFamily;

            // If addresses family's don't map and we aren't mapping to IP6 then return false.
            if (!ip4Mapping && addressFamily != otherFamily)
            {
                return false;
            }

            // If any of the values are IP6, then remove the scopes.
            if (addressFamily == AddressFamily.InterNetworkV6 && address.ScopeId != 0)
            {
                address.ScopeId = 0;
            }

            if (otherFamily == AddressFamily.InterNetworkV6 && other.ScopeId != 0)
            {
                other.ScopeId = 0;
            }

            if (addressFamily == otherFamily)
            {
                return address.Equals(other);
            }

            // Ensure both addresses are IP6.
            if (addressFamily == AddressFamily.InterNetwork)
            {
                address = address.MapToIPv6();
            }

            if (otherFamily == AddressFamily.InterNetwork)
            {
                other = other.MapToIPv6();
            }

            return address.Equals(other);
        }

        private void OnNamedConfigurationUpdated(object sender, ConfigurationUpdateEventArgs e)
        {
            if (string.Equals(e.Key, "dlna", StringComparison.OrdinalIgnoreCase))
            {
                _options = _configurationManager.GetConfiguration<DlnaOptions>("dlna");
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
        private bool IsInvalid(IPAddress localIPAddress, IPEndPoint endPoint, bool matchSockets, bool sourceInternal = false)
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
                    if (sourceInternal && IsUPnPActive && endPoint.Port == 1900 && _networkManager.IsGatewayInterface(endPoint.Address))
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
        /// <param name="multicast">True of this a multicast message.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SendFromSocketsAsync(List<Socket> sockets, string message, IPEndPoint destination, int sendCount, bool multicast)
        {
            // IPv6 addresses can overlapp and we don't want to transmit multiple times over the same scope.
            var scopes = new List<long>();

            byte[] messageData = Encoding.UTF8.GetBytes(message);

            while (sendCount-- >= 0)
            {
                foreach (var socket in sockets)
                {
                    var addr = ((IPEndPoint)socket.LocalEndPoint).Address;
                    if (addr.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        if (multicast && addr.ScopeId == 0 && scopes.IndexOf(addr.ScopeId) != -1)
                        {
                            // Only transmit to one scope once, and don't transmit with IP6 interface addresses.
                            continue;
                        }

                        scopes.Add(addr.ScopeId);
                    }

                    if (_options.EnableDebugLog)
                    {
                        _logger.LogDebug("{0}->{1}:{2}", addr, destination.Address, message);
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

                scopes.Clear();
                await Task.Delay(100).ConfigureAwait(false);
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
            if (!_sockets.Where(s => EndPointEquals(s, localIPAddress, true)).Any())
            {
                try
                {
                    _logger.LogDebug("Creating socket for {0}.", localIPAddress);
                    _sockets.Add(CreateUdpMulticastSocket(localIPAddress, port));
                    return true;
                }
                catch (SocketException ex)
                {
                    _logger.LogError(ex, "Error creating socket {0} port {1}", localIPAddress, port);
                }
            }
            else
            {
                _logger.LogDebug("Skipping creating duplicate socket {0} port {1}.", localIPAddress, port);
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
                if (EnableMultiSocketBinding && _sockets.Count > 0)
                {
                    // Rather than destroying all sockets and re-creating them, only destroy invalid ones.
                    List<Socket>? removeThese = null;
                    lock (_socketSynchroniser)
                    {
                        // Elaborate code as you cannot alter an enum whilst in an enum.
                        foreach (var socket in _sockets)
                        {
                            // If not an IPAny/v6Any and socket doesn't exist any more, then dispose of it.
                            IPAddress addr = ((IPEndPoint)socket.LocalEndPoint).Address;
                            if (!addr.Equals(_anyAddressIP4) && !addr.Equals(_anyAddressIP6) && !ba.Exists(addr))
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
                    if (IsIP6Enabled)
                    {
                        CreateUniqueSocket(IPAddress.IPv6Any, 1900);
                        CreateUniqueSocket(IPAddress.IPv6Loopback);
                    }
                    else if (IsIP4Enabled)
                    {
                        CreateUniqueSocket(IPAddress.Any, 1900);
                        CreateUniqueSocket(IPAddress.Loopback);
                    }
                }

                if (EnableMultiSocketBinding)
                {
                    foreach (IPObject ip in ba)
                    {
                        CreateUniqueSocket(ip.Address, 1900);
                    }
                }
            }

            _ = StartListening();
        }

        /// <summary>
        /// Gets the correct version of IP Any address if dualsockets is enabled.
        /// </summary>
        /// <param name="any">IP Any version to check, or null to return the first valid ANY address.</param>
        /// <returns>The corrected version if DualSockets is enabled.</returns>
        private IPAddress AnyIP(IPAddress? any = null)
        {
            // Interesting fact: When an ANY socket is created in dualmode - it gets given a new value that is neither IPAddress.Any nor IPAddress.IPv6Any.
            if (IsIP4Enabled && IsIP6Enabled)
            {
                return IPNetAddress.DualIpAny;
            }

            if (any != null)
            {
                return any;
            }

            if (IsIP6Enabled)
            {
                return IPAddress.IPv6Any;
            }

            return IPAddress.Any;
        }

        /// <summary>
        /// Enables the SSDP injection of devices found by Mono.Nat.
        /// </summary>
        /// <param name="sender">Mono.Nat instance.</param>
        /// <param name="e">Information Mono received, but doesn't use.</param>
        private async void UnknownDeviceFound(object sender, DeviceEventUnknownArgs e)
        {
            IPEndPoint ep = (IPEndPoint)e.EndPoint;

            // Only process the IP address family that we are configured for.
            if (!IsIP4Enabled && ep.AddressFamily == AddressFamily.InterNetwork)
            {
                return;
            }

            if (!IsIP6Enabled && ep.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return;
            }

            // _logger.LogDebug("Mono.NAT passing information to our SSDP processor.");
            await ProcessMessage(e.Data, ep, e.Address, true).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the sockets listening tasks.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task StartListening()
        {
            if (IsUPnPActive)
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
            var receiveBuffer = ArrayPool<byte>.Shared.Rent(1024);
            IPEndPoint listeningOn = (IPEndPoint)socket.LocalEndPoint;
            IPEndPoint remote;
            string buffer;

            _logger.LogDebug("Listening on {0}/{1}", listeningOn.Address, listeningOn.Port);
            try
            {
                var endPoint = socket.LocalEndPoint;
                while (true)
                {
                    try
                    {
                        var result = await socket.ReceiveFromAsync(receiveBuffer, SocketFlags.None, endPoint).ConfigureAwait(false);

                        if (result.ReceivedBytes > 0)
                        {
                            remote = (IPEndPoint)result.RemoteEndPoint;
                            buffer = Encoding.UTF8.GetString(receiveBuffer, 0, result.ReceivedBytes);

                            if (_options.EnableDebugLog)
                            {
                                _logger.LogDebug("{0}->{1}:{2}", remote.Address, listeningOn.Address, buffer);
                            }

                            await ProcessMessage(buffer, remote, ((IPEndPoint)socket.LocalEndPoint).Address, false).ConfigureAwait(false);
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
