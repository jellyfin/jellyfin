using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jellyfin.Networking.Manager;
using Jellyfin.Networking.Structures;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Networking.Udp
{
    /// <summary>
    /// Processes a SSDP message.
    /// </summary>
    /// <param name="client">The client from which we received the message.</param>
    /// <param name="data">The data to process.</param>
    /// <param name="receivedFrom">The remote endpoint.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public delegate Task UdpProcessor(UdpProcess client, string data, IPEndPoint receivedFrom);

    /// <summary>
    /// Provides base level tracing facilities.
    /// </summary>
    /// <param name="msg">Message to send.</param>
    /// <param name="parameters">Parameters.</param>
    public delegate void TraceFunction(string msg, params object[] parameters);

    /// <summary>
    /// Provides the platform independent logic for publishing device existence and responding to search requests.
    ///
    /// Is designed to work in conjunction with ExternalPortForwarding.
    ///
    /// Lazy implementation. Socks will only be created at first use.
    /// </summary>
    public class UdpServer : IDisposable
    {
        private static NetCollection? _internalInterface;
        private static NetCollection? _allInterfaces;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpServer"/> class.
        /// </summary>
        /// <param name="networkManager">The networkManager<see cref="INetworkManager"/>.</param>
        /// <param name="configurationManager">The system configuration.</param>
        /// <param name="logger">The logger factory instance.<see cref="ILogger"/>.</param>
        public UdpServer(INetworkManager networkManager, IConfigurationManager configurationManager, ILogger logger)
        {
            ConfigurationManager = configurationManager ?? throw new NullReferenceException(nameof(configurationManager));
            NetManager = networkManager ?? throw new NullReferenceException(nameof(networkManager));
            Logger = logger ?? throw new NullReferenceException(nameof(logger));
            ConfigurationManager.ConfigurationUpdated += ConfigurationUpdated;
            UDPPortRange = ((ServerConfiguration)configurationManager.CommonConfiguration).UDPPortRange;

            if (_internalInterface == null)
            {
                _internalInterface = NetworkManager.Instance.GetInternalBindAddresses();
                _allInterfaces = NetworkManager.Instance.GetAllBindInterfaces();
            }

            NetManager.NetworkChanged += NetworkChanged;
        }

        /// <summary>
        /// Gets a value indicating whether is multi-socket binding available.
        /// </summary>
        public static bool EnableMultiSocketBinding => NetworkManager.EnableMultiSocketBinding;

        /// <summary>
        /// Gets a value indicating whether IP6 is enabled.
        /// </summary>
        public static bool IsIP6Enabled => NetworkManager.IsIP6Enabled;

        /// <summary>
        /// Gets a value indicating whether IP4 is enabled.
        /// </summary>
        public static bool IsIP4Enabled => NetworkManager.IsIP4Enabled;

        /// <summary>
        /// Gets uDPPort range to use.
        /// </summary>
        protected static string UDPPortRange { get; private set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        protected bool Disposed { get; private set; }

        /// <summary>
        /// Gets the logger instance.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Gets the NetworkManager instance.
        /// </summary>
        protected INetworkManager NetManager { get; }

        /// <summary>
        /// Gets the ConfigurationManager instance.
        /// </summary>
        protected IConfigurationManager ConfigurationManager { get; }

        /// <summary>
        /// Gets the common configuration.
        /// </summary>
        protected ServerConfiguration Configuration { get => (ServerConfiguration)ConfigurationManager.CommonConfiguration; }

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
                // Random Port.
                range.Min = 1;
                range.Max = 65535;
                return false;
            }

            // Remove all white space.
            rangeStr = Regex.Replace(rangeStr, @"\s+", string.Empty);

            var parts = rangeStr.Split('-');
            if (parts.Length == 2)
            {
                int minVal = int.TryParse(parts[0], out int min) ? min : 1;
                int maxVal = int.TryParse(parts[1], out int max) ? max : 65535;
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
            range.Min = 1;
            range.Max = 65535;
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
        /// <param name="port">Port to use.</param>
        /// <param name="localIPAddress">Optional IP address to use for comparison, or null to default.</param>
        /// <returns>IPEndpoint set to the port provided.</returns>
        public static IPEndPoint GetMulticastEndPoint(int port, IPAddress? localIPAddress = null)
        {
            if (localIPAddress == null)
            {
                localIPAddress = IsIP6Enabled ? IPAddress.IPv6Any : IPAddress.Any;
            }

            return new IPEndPoint(
                localIPAddress.AddressFamily == AddressFamily.InterNetwork ?
                    IPNetAddress.MulticastIPv4 : IPObject.IsIPv6LinkLocal(localIPAddress) ?
                        IPNetAddress.MulticastIPv6LinkLocal : IPNetAddress.MulticastIPv6SiteLocal, port);
        }

        /// <summary>
        /// Returns a udp port based upon the range specified in a string.
        /// </summary>
        /// <param name="portStr">Port Range, or empty/zero for a random port.</param>
        /// <returns>System.Int32.</returns>
        public static int GetPort(string portStr)
        {
            int port = 0;
            if (TryParseRange(portStr, out (int Min, int Max) range))
            {
                port = GetUdpPortFromRange(range);
            }

            if (port < 0 || port > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(portStr), $"UDP port in the range {portStr} cannot be allocated.");
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
        /// <param name="logger">Optional logger.</param>
        /// <returns>A Socket.</returns>
        public static Socket CreateUdpBroadcastSocket(int port, ILogger? logger = null)
        {
            if (port < 0 || port > 65536)
            {
                throw new ArgumentException("Port out of range", nameof(port));
            }

            IPAddress address = IsIP6Enabled ? IPAddress.IPv6Any : IPAddress.Any;
            Socket retVal = PrepareSocket(address, logger);
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
        /// Creates a new UDP socket that is a member of the SSDP multicast local admin group and binds it to the specified local port.
        /// </summary>
        /// <param name="address">IP Address to bind.</param>
        /// <param name="port">UDP port to bind.</param>
        /// <param name="logger">Logger instance.</param>
        /// <returns>A Socket.</returns>
        public static Socket CreateUdpMulticastSocket(IPAddress address, int port, ILogger? logger = null)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (port < 0 || port > 65536)
            {
                throw new ArgumentException("Port out of range", nameof(port));
            }

            Socket retVal = PrepareSocket(address, logger);
            try
            {
                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);
            }
            catch (SocketException ex)
            {
                logger?.LogDebug(ex, "Error setting multicast values on socket. {0}/{1}", address, port);
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                retVal.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, BitConverter.GetBytes((int)address.ScopeId));
            }
            else
            {
                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, address.GetAddressBytes());
            }

            try
            {
                retVal.Bind(new IPEndPoint(address, port));
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Unable to bind to {0}/{1}.", address, port);
                retVal?.Dispose();
                throw;
            }

            return retVal;
        }

        /// <summary>
        /// Creates a UdpProcess for use.
        /// </summary>
        /// <param name="address">Address of udp client.</param>
        /// <param name="port">Port to listen upon.</param>
        /// <param name="processor">Optional processing function for incoming packets.</param>
        /// <param name="logger">Optional logger.</param>
        /// <param name="failure">Optional. Method to call in case of listening failure.</param>
        /// <param name="startListening">Optional. Autostart listening.</param>
        /// <returns>Returns two UdpProcesss, one for listening and one for transmitting.</returns>
        public static UdpProcess? CreateUnicastClient(
            IPAddress address,
            int port,
            UdpProcessor? processor = null,
            ILogger? logger = null,
            FailureFunction? failure = null,
            bool startListening = true)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (port == 0)
            {
                port = GetPort(UDPPortRange);
                logger?.LogDebug("Selected udp port {0} from the config range : {1}", port, UDPPortRange);
            }

            try
            {
                UdpProcess sender = new UdpProcess(address, port, processor, logger, failure);
                sender.Bind();
                if (startListening)
                {
                    sender.BeginReceive(new AsyncCallback(OnReceive), sender);
                }

                return sender;
            }
            catch (SocketException ex)
            {
                logger?.LogError(ex, "Error creating socket {0}", address);
                return null;
            }
        }

        /// <summary>
        /// Creates a set of multicast clients, one for each internal network address.
        /// </summary>
        /// <param name="port">Port to listen upon.</param>
        /// <param name="processor">Optional processing function for incoming packets.</param>
        /// <param name="logger">Optional logger.</param>
        /// <param name="failure">Optional. Method to call in case of listening failure.</param>
        /// <param name="restrictedToLAN">Create clients only for the internal LAN.</param>
        /// <param name="enableTracing">Enables tracing on the ports.</param>
        /// <returns>A list of UdpProcesss.</returns>
        public static List<UdpProcess> CreateMulticastClients(
            int port,
            UdpProcessor? processor = null,
            ILogger? logger = null,
            FailureFunction? failure = null,
            bool restrictedToLAN = true,
            bool enableTracing = false)
        {
            if (_internalInterface == null || _allInterfaces == null)
            {
                _internalInterface = NetworkManager.Instance.GetInternalBindAddresses();
                _allInterfaces = NetworkManager.Instance.GetAllBindInterfaces();
            }

            var clients = new List<UdpProcess>();

            NetCollection ba = restrictedToLAN ? _internalInterface : _allInterfaces;

            foreach (IPObject ip in ba)
            {
                var client = CreateMulticastClient(ip.Address, port, processor, logger, failure);
                if (client != null)
                {
                    client.Tracing = enableTracing;
                    clients.Add(client);
                }
            }

            return clients;
        }

        /// <summary>
        /// Creates a UdpProcess for use.
        /// </summary>
        /// <param name="address">Address of udp client.</param>
        /// <param name="port">Port to listen upon.</param>
        /// <param name="processor">Optional processing function for incoming packets.</param>
        /// <param name="logger">Optional logger.</param>
        /// <param name="failure">Optional. Method to call in case of listening failure.</param>
        /// <param name="startListening">If set, a listener will be automatically created for the port.</param>
        /// <returns>Returns a UdpProcess instance.</returns>
        public static UdpProcess? CreateMulticastClient(
            IPAddress address,
            int port,
            UdpProcessor? processor,
            ILogger? logger = null,
            FailureFunction? failure = null,
            bool startListening = true)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            try
            {
                UdpProcess listener = new UdpProcess(address, port, processor, logger, failure);
                if (IsIP4Enabled && address.AddressFamily == AddressFamily.InterNetwork)
                {
                    try
                    {
                        listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    }
                    catch (SocketException)
                    {
                    }

                    try
                    {
                        listener.ExclusiveAddressUse = false;
                    }
                    catch (SocketException)
                    {
                    }

                    listener.Bind();
                    listener.EnableBroadcast = true;
                    listener.JoinMulticastGroup(IPNetAddress.MulticastIPv4, address);
                }
                else if (IsIP6Enabled && address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    try
                    {
                        listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    }
                    catch (SocketException)
                    {
                    }

                    try
                    {
                        listener.ExclusiveAddressUse = false;
                    }
                    catch (SocketException)
                    {
                    }

                    listener.Bind();
                    listener.EnableBroadcast = true;
                    if (IPObject.IsIPv6LinkLocal(address))
                    {
                        listener.JoinMulticastGroup((int)address.ScopeId, IPNetAddress.MulticastIPv6LinkLocal);
                    }
                    else
                    {
                        listener.JoinMulticastGroup((int)address.ScopeId, IPNetAddress.MulticastIPv6SiteLocal);
                    }
                }

                if (startListening)
                {
                    listener.BeginReceive(new AsyncCallback(OnReceive), listener);
                }

                return listener;
            }
            catch (SocketException)
            {
                return null;
            }
        }

        /// <summary>
        /// Sends a packet via unicast.
        /// </summary>
        /// <param name="client">UdpProcess to use.</param>
        /// <param name="packet">Packet to send.</param>
        /// <param name="remote">Destination endpoint.</param>
        /// <param name="sendCount">Optional number of times to transmit. Default is 1.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task SendUnicast(UdpProcess client, string packet, IPEndPoint remote, int sendCount = 1)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (remote == null)
            {
                throw new ArgumentNullException(nameof(remote));
            }

            byte[] buffer = Encoding.UTF8.GetBytes(packet);

            if (client.Tracing)
            {
                client.Track("->{0} : {1} count:{3} \r\n{4}", client.LocalEndPoint, remote, sendCount, packet);
            }

            for (int a = 0; a <= sendCount - 1; a++)
            {
                try
                {
                    await client.SendAsync(buffer, buffer.Length, remote).ConfigureAwait(false);
                }
                catch (SocketException ex)
                {
                    client.Logger?.LogDebug(ex, "Error sending on {0}", remote);
                }
            }
        }

        /// <summary>
        /// Sends a packet via multicast over mulltiple sockets.
        /// </summary>
        /// <param name="clients">UdpProcesses to use.</param>
        /// <param name="port">Port number to use.</param>
        /// <param name="packet">Packet to send.</param>
        /// <param name="sendCount">Optional number of times to trasmit. Default is 1.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task SendMulticasts(List<UdpProcess> clients, int port, string packet, int sendCount = 1)
        {
            if (clients == null)
            {
                throw new ArgumentNullException(nameof(clients));
            }

            foreach (var client in clients)
            {
                await SendMulticast(client, port, packet, sendCount).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sends a packet via multicast.
        /// </summary>
        /// <param name="client">UdpProcess to use.</param>
        /// <param name="port">Port number to use.</param>
        /// <param name="packet">Packet to send.</param>
        /// <param name="sendCount">Optional number of times to trasmit. Default is 1.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task SendMulticast(UdpProcess client, int port, string packet,  int sendCount = 1)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            var intf = client.LocalEndPoint.Address;
            IPEndPoint mcast = new IPEndPoint(IPNetAddress.MulticastIPv4, port);
            byte[] buffer = Encoding.UTF8.GetBytes(packet);
            if (intf.AddressFamily == AddressFamily.InterNetwork)
            {
                try
                {
                    client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, intf.GetAddressBytes());
                    client.Client.MulticastLoopback = true;
                }
                catch (SocketException ex)
                {
                    client.Logger?.LogDebug(ex, "Error setting multicast.");
                }
            }
            else if (intf.AddressFamily == AddressFamily.InterNetworkV6 && intf.ScopeId != 0)
            {
                try
                {
                    client.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, BitConverter.GetBytes((int)intf.ScopeId));
                    mcast = IPObject.IsIPv6LinkLocal(intf) ?
                        new IPEndPoint(IPNetAddress.MulticastIPv6LinkLocal, port) :
                        new IPEndPoint(IPNetAddress.MulticastIPv6SiteLocal, port);
                    client.Client.MulticastLoopback = true;
                }
                catch (SocketException ex)
                {
                    client.Logger?.LogDebug(ex, "Error setting multicast.");
                }
            }
            else
            {
                // Cannot use this address.
                return;
            }

            if (client.Tracing)
            {
                client.Track("->{0} : {1} count:{3} \r\n{4}", client.LocalEndPoint, mcast, sendCount, packet);
            }

            try
            {
                for (int a = 0; a < sendCount - 1; a++)
                {
                    await client.SendAsync(buffer, buffer.Length, mcast).ConfigureAwait(false);
                }
            }
            catch (SocketException ex)
            {
                client.Logger?.LogDebug(ex, "Error sending to {0}:{1}", intf, mcast);
            }
        }

        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Override to update settings on a config change.
        /// </summary>
        protected virtual void UpdateArguments()
        {
        }

        /// <summary>
        /// Protected dispose method.
        /// </summary>
        /// <param name="disposing">True if disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    NetManager.NetworkChanged -= NetworkChanged;
                    ConfigurationManager.ConfigurationUpdated -= ConfigurationUpdated;
                }

                Disposed = true;
            }
        }

        /// <summary>
        /// Creates a socket for use.
        /// </summary>
        /// <param name="address">Address of socket.</param>
        /// <param name="logger">Optional logger.</param>
        /// <returns>Socket instance.</returns>
        private static Socket PrepareSocket(IPAddress address, ILogger? logger)
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
                }
            }

            try
            {
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            }
            catch (SocketException ex)
            {
                logger?.LogWarning(ex, "Error setting socket as reusable. {0}", address);
            }

            try
            {
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
            }
            catch (SocketException ex)
            {
                logger?.LogWarning(ex, "Error setting socket as non exclusive. {0}", address);
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
                    logger?.LogWarning(ex, "Error enabling broadcast on socket. {0}", address);
                }
            }

            return retVal;
        }

        private static void OnReceive(IAsyncResult result)
        {
            if (result.AsyncState == null)
            {
                return;
            }

            UdpProcess client = (UdpProcess)result.AsyncState;

            if (client == null || client.Processor == null)
            {
                return;
            }

            try
            {
                IPEndPoint? remote = null;
                string data = Encoding.UTF8.GetString(client.EndReceive(result, ref remote));
                try
                {
                    if (client.Tracing)
                    {
                        client.Track("<- {0} : {1} : {2}", client.LocalEndPoint, remote, data);
                    }

                    _ = client.Processor(client, data, remote);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
                {
                    client.Logger?.LogError(ex, "Error processing message. {0}", data);
                }

                client.BeginReceive(new AsyncCallback(OnReceive), client);
            }
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                client.OnFailure?.Invoke(client, ex, $"Error listening to {client.LocalEndPoint}");
            }
        }

        private void NetworkChanged(object? sender, EventArgs args)
        {
            _internalInterface = NetManager.GetInternalBindAddresses();
            _allInterfaces = NetManager.GetAllBindInterfaces();
        }

        /// <summary>
        /// Triggered on a system configuration change.
        /// </summary>
        /// <param name="sender">Configuration object.</param>
        /// <param name="args">Event arguments.</param>
        private void ConfigurationUpdated(object? sender, EventArgs args)
        {
            UpdateArguments();
        }
    }
}
