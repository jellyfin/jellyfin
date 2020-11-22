#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Common.Udp
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
    public static class UdpHelper
    {
        /// <summary>
        /// Gets or sets a value indicating whether multi-socket binding should be enabled.
        /// </summary>
        public static bool EnableMultiSocketBinding { get; set; } = true;

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
        /// <param name="isIP6Enabled">True if IP6 is enabled (default).</param>
        /// <returns>IPEndpoint set to the port provided.</returns>
        public static IPEndPoint GetMulticastEndPoint(int port, IPAddress? localIPAddress = null, bool isIP6Enabled = true)
        {
            if (localIPAddress == null)
            {
                localIPAddress = isIP6Enabled ? IPAddress.IPv6Any : IPAddress.Any;
            }

            return new IPEndPoint(
                localIPAddress.AddressFamily == AddressFamily.InterNetwork ?
                    IPNetAddress.SSDPMulticastIPv4 : IPObject.IsIPv6LinkLocal(localIPAddress) ?
                        IPNetAddress.SSDPMulticastIPv6LinkLocal : IPNetAddress.SSDPMulticastIPv6SiteLocal, port);
        }

        /// <summary>
        /// Returns a udp port based upon the range specified in a string.
        /// </summary>
        /// <param name="portStr">Port Range, or empty/zero for a random port.</param>
        /// <returns>System.Int32.</returns>
        public static int GetPort(string portStr)
        {
            int port = 0;
            if (portStr.TryParseRange(out (int Min, int Max) range))
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
        /// <param name="ip4">Create an udp IPv4 socket.</param>
        /// <param name="ip6">Create an udp IPv6 socket.</param>
        /// <returns>A Socket.</returns>
        public static Socket CreateUdpBroadcastSocket(int port, ILogger? logger = null, bool ip4 = true, bool ip6 = true)
        {
            if (port < 0 || port > 65536)
            {
                throw new ArgumentException("Port out of range", nameof(port));
            }

            IPAddress address = ip6 ? IPAddress.IPv6Any : IPAddress.Any;
            Socket retVal = PrepareSocket(address, logger, ip4);
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
        /// <param name="dualsocket">Create a udp for both IPv4 and IPv6 regardless of the address family of the IP bind address.</param>
        /// <returns>A Socket.</returns>
        public static Socket CreateUdpMulticastSocket(IPAddress address, int port, ILogger? logger = null, bool dualsocket = true)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (port < 0 || port > 65536)
            {
                throw new ArgumentException("Port out of range", nameof(port));
            }

            Socket retVal = PrepareSocket(address, logger, dualsocket);
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
        /// <param name="udpPortRange">Optional port range to use.</param>
        /// <returns>Returns two UdpProcesss, one for listening and one for transmitting.</returns>
        public static UdpProcess? CreateUnicastClient(
            IPAddress address,
            int port,
            UdpProcessor? processor = null,
            ILogger? logger = null,
            FailureFunction? failure = null,
            bool startListening = true,
            string udpPortRange = "1024-65535")
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (port == 0)
            {
                port = GetPort(udpPortRange);

                logger?.LogDebug("Selected udp port {0} from the config range : {1}", port, udpPortRange);
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
        /// <param name="addresses">List of addresses to use.</param>
        /// <param name="processor">Optional processing function for incoming packets.</param>
        /// <param name="logger">Optional logger.</param>
        /// <param name="failure">Optional. Method to call in case of listening failure.</param>
        /// <param name="enableTracing">Enables tracing on the ports.</param>
        /// <returns>A list of UdpProcesss.</returns>
        public static List<UdpProcess> CreateMulticastClients(
            int port,
            Collection<IPObject> addresses,
            UdpProcessor? processor = null,
            ILogger? logger = null,
            FailureFunction? failure = null,
            bool enableTracing = false)
        {
            var clients = new List<UdpProcess>();

            foreach (IPObject ip in addresses ?? throw new ArgumentNullException(nameof(addresses)))
            {
                UdpProcess? client = CreateMulticastClient(ip.Address, port, processor, logger, failure);
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
        /// <param name="ip4">True if IP4 addresses should be used (default).</param>
        /// <param name="ip6">True if IP6 is enabled (default).</param>
        /// <returns>Returns a UdpProcess instance.</returns>
        public static UdpProcess? CreateMulticastClient(
            IPAddress address,
            int port,
            UdpProcessor? processor,
            ILogger? logger = null,
            FailureFunction? failure = null,
            bool startListening = true,
            bool ip4 = true,
            bool ip6 = true)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            try
            {
                UdpProcess listener = new UdpProcess(address, port, processor, logger, failure);
                if (ip4 && address.AddressFamily == AddressFamily.InterNetwork)
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
                    listener.JoinMulticastGroup(IPNetAddress.SSDPMulticastIPv4, address);
                }
                else if (ip6 && address.AddressFamily == AddressFamily.InterNetworkV6)
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
                        listener.JoinMulticastGroup((int)address.ScopeId, IPNetAddress.SSDPMulticastIPv6LinkLocal);
                    }
                    else
                    {
                        listener.JoinMulticastGroup((int)address.ScopeId, IPNetAddress.SSDPMulticastIPv6SiteLocal);
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
        /// Sends a packet via multicast over multiple sockets.
        /// </summary>
        /// <param name="clients">UdpProcesses to use.</param>
        /// <param name="port">Port number to use.</param>
        /// <param name="packet">Packet to send.</param>
        /// <param name="sendCount">Optional number of times to transmit. Default is 1.</param>
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
        /// <param name="sendCount">Optional number of times to transmit. Default is 1.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task SendMulticast(UdpProcess client, int port, string packet, int sendCount = 1)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            var intf = client.LocalEndPoint.Address;
            IPEndPoint mcast = new IPEndPoint(IPNetAddress.SSDPMulticastIPv4, port);
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
                        new IPEndPoint(IPNetAddress.SSDPMulticastIPv6LinkLocal, port) :
                        new IPEndPoint(IPNetAddress.SSDPMulticastIPv6SiteLocal, port);
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
        /// Creates a socket for use.
        /// </summary>
        /// <param name="address">Address of socket.</param>
        /// <param name="logger">Optional logger.</param>
        /// <param name="dualSocket">Create a dual-socket regardless of what the address family specified is.</param>
        /// <returns>Socket instance.</returns>
        private static Socket PrepareSocket(IPAddress address, ILogger? logger, bool dualSocket = true)
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
                if (dualSocket)
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

            if (dualSocket)
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

            if (client.Processor == null)
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
                        client.Track("<- {0} : {1} : {2}", client.LocalEndPoint, remote!, data);
                    }

                    _ = client.Processor(client, data, remote!);
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
    }
}
