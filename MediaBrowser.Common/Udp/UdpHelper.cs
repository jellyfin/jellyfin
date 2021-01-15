#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Common.Udp
{
    /// <summary>
    /// Delagate that processes a UDP message.
    /// </summary>
    /// <param name="client">The client from which we received the message.</param>
    /// <param name="data">The data to process.</param>
    /// <param name="receivedFrom">The remote endpoint.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public delegate Task UdpProcessor(UdpProcess client, string data, IPEndPoint receivedFrom);

    /// <summary>
    /// Delegate that provides base level tracing functionality.
    /// </summary>
    /// <param name="msg">Message to send.</param>
    /// <param name="parameters">Parameters.</param>
    public delegate void TraceFunction(string msg, params object[] parameters);

    /// <summary>
    /// Defines a static instance of <see cref="UdpHelper"/>.
    /// </summary>
    public static class UdpHelper
    {
        /// <summary>
        /// Gets or sets a value indicating whether multi-socket binding should be enabled. Default is enabled.
        /// </summary>
        public static bool EnableMultiSocketBinding { get; set; } = true;

        /// <summary>
        /// Returns an unused UDP port number in the range specified.
        /// </summary>
        /// <param name="range">Upper and Lower boundary of ports to select.</param>
        /// <returns>A UDP port number within the range specified, or 0 if non available.</returns>
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
        /// <returns>A random available port number between 49152 and 65535, or 0 if all are in use.</returns>
        public static int GetRandomUnusedUdpPort()
        {
            // Get a port from the dynamic range.
            return GetUdpPortFromRange((49152, 65535));
        }

        /// <summary>
        /// Returns the correct multicast address based upon the value of the address provided.
        /// </summary>
        /// <param name="port">Port to use.</param>
        /// <param name="localIpAddress">IP address to use for comparison.</param>
        /// <returns>An <see cref="IPEndPoint"/> set to the port provided.</returns>
        public static IPEndPoint GetMulticastEndPoint(int port, IPAddress localIpAddress)
        {
            if (localIpAddress == null)
            {
                throw new ArgumentNullException(nameof(localIpAddress));
            }

            return new IPEndPoint(
                localIpAddress.AddressFamily == AddressFamily.InterNetwork ?
                    IPNetAddress.SSDPMulticastIPv4 : IPObject.IsIPv6LinkLocal(localIpAddress) ?
                        IPNetAddress.SSDPMulticastIPv6LinkLocal : IPNetAddress.SSDPMulticastIPv6SiteLocal, port);
        }

        /// <summary>
        /// Returns the correct multicast address based upon the value of the address provided.
        /// </summary>
        /// <param name="port">Port to use.</param>
        /// <param name="family">True if IP6 is enabled (default).</param>
        /// <returns>An <see cref="IPEndPoint"/> set to the port provided.</returns>
        public static IPEndPoint GetMulticastEndPoint(int port, AddressFamily family)
        {
            var localIpAddress = family == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any;

            return new IPEndPoint(
                localIpAddress.AddressFamily == AddressFamily.InterNetwork ?
                    IPNetAddress.SSDPMulticastIPv4 : IPObject.IsIPv6LinkLocal(localIpAddress) ?
                        IPNetAddress.SSDPMulticastIPv6LinkLocal : IPNetAddress.SSDPMulticastIPv6SiteLocal, port);
        }

        /// <summary>
        /// Returns a UDP port based upon the range specified in a string.
        /// </summary>
        /// <param name="portStr">Port Range, or empty/zero for a random port.</param>
        /// <returns>A UDP port in the range specified. If the range is invalid, a port number in the range 49152 and 65535 is returned.</returns>
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
        /// <param name="family">Network family for the broadcast.</param>
        /// <param name="logger">Optional logger.</param>
        /// <returns>A <see cref="Socket"/> instance.</returns>
        public static Socket CreateUdpBroadcastSocket(int port, AddressFamily family, ILogger? logger = null)
        {
            if (port < 0 || port > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(port), $"UDP port in the range {port} cannot be allocated.");
            }

            IPAddress address = family == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any;
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
        /// Creates a new UDP socket that is a member of the multicast local admin group and binds it to the specified local port.
        /// </summary>
        /// <param name="address">IP Address to bind.</param>
        /// <param name="port">UDP port to bind.</param>
        /// <param name="logger">Optional. A <see cref="ILogger"/> instance.</param>
        /// <returns>A <see cref="Socket"/> instance.</returns>
        public static Socket CreateUdpMulticastSocket(IPAddress address, int port, ILogger? logger = null)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (port < 0 || port > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(port), $"UDP port in the range {port} cannot be allocated.");
            }

            Socket retVal = PrepareSocket(address, logger);
            try
            {
                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);
            }
            catch (SocketException ex)
            {
                logger?.LogDebug(ex, "Error setting multicast values on socket. {Address}/{Port}", address, port);
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
                logger?.LogError(ex, "Unable to bind to {Address}/{Port}.", address, port);
                retVal?.Dispose();
                throw;
            }

            return retVal;
        }

        /// <summary>
        /// Creates an UdpProcess for use.
        /// </summary>
        /// <param name="address">Address of udp client.</param>
        /// <param name="port">Port to listen upon.</param>
        /// <param name="processor">Optional <see cref="UdpProcessor"/> delegate for the incoming packets.</param>
        /// <param name="logger">Optional <see cref="ILogger"/> instance.</param>
        /// <param name="failure">Optional. Method to call in case of listening failure.</param>
        /// <returns>The <see cref="UdpProcess"/> instance.</returns>
        public static UdpProcess? CreateUnicastClient(
            IPAddress address,
            int port,
            UdpProcessor? processor = null,
            ILogger? logger = null,
            FailureFunction? failure = null)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (address.AddressFamily != AddressFamily.InterNetwork && address.AddressFamily != AddressFamily.InterNetworkV6)
            {
                logger?.LogError("Invalid interface type of {AddressFamily} on {Address}", address.AddressFamily, address);
                return null;
            }

            try
            {
                UdpProcess sender = new UdpProcess(address, port, processor, logger, failure);
                sender.Client.Bind(sender.LocalEndPoint);
                sender.BeginReceive(new AsyncCallback(OnReceive), sender);
                return sender;
            }
            catch (SocketException ex)
            {
                logger?.LogError(ex, "Error creating socket {Address}", address);
                return null;
            }
        }

        /// <summary>
        /// Creates a set of multicast clients, one for each addresses in <paramref name="addresses"/>.
        /// </summary>
        /// <param name="port">Port to listen upon.</param>
        /// <param name="addresses">List of addresses to use.</param>
        /// <param name="ip4compliant">True if the port should be IPv4 compatible.</param>
        /// <param name="ip6compliant">True if the port should be IPv6 compatible.</param>
        /// <param name="processor">Optional. An <see cref="UdpProcessor"/> delegate for the incoming packets.</param>
        /// <param name="logger">Optional. An <see cref="ILogger"/> instance.</param>
        /// <param name="failure">Optional. An <see cref="FailureFunction"/> delegate to use in case of listening failure.</param>
        /// <param name="enableTracing">Optional. Enables tracing on the ports.</param>
        /// <returns>A <see cref="List{UdpProcess}"/>.</returns>
        public static List<UdpProcess> CreateMulticastClients(
            int port,
            Collection<IPObject> addresses,
            bool ip4compliant,
            bool ip6compliant,
            UdpProcessor? processor = null,
            ILogger? logger = null,
            FailureFunction? failure = null,
            bool enableTracing = false)
        {
            var clients = new List<UdpProcess>();

            foreach (IPObject ip in addresses ?? throw new ArgumentNullException(nameof(addresses)))
            {
                if (((!ip6compliant) && (ip.AddressFamily == AddressFamily.InterNetworkV6))
                    || ((!ip4compliant) && (ip.AddressFamily == AddressFamily.InterNetwork)))
                {
                    continue;
                }

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
        /// <param name="processor">Optional. An <see cref="UdpProcessor"/> delegate for the incoming packets.</param>
        /// <param name="logger">Optional. An <see cref="ILogger"/> instance.</param>
        /// <param name="failure">Optional. An <see cref="FailureFunction"/> delegate to use in case of listening failure.</param>
        /// <returns>The <see cref="UdpProcess"/> instance.</returns>
        public static UdpProcess? CreateMulticastClient(
            IPAddress address,
            int port,
            UdpProcessor? processor,
            ILogger? logger = null,
            FailureFunction? failure = null)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6 && address.ScopeId == 0)
            {
                logger?.LogError("Cannot create multicast client on {Address}. ScopeId: {ScopeId}", address, address.ScopeId);
                return null;
            }

            if (address.AddressFamily != AddressFamily.InterNetwork && address.AddressFamily != AddressFamily.InterNetworkV6)
            {
                logger?.LogError("Invalid interface type of {AddressFamily} on {Address}", address.AddressFamily, address);
                return null;
            }

            try
            {
                UdpProcess udp = new UdpProcess(address, port, processor, logger, failure);

                try
                {
                    udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                }
                catch (SocketException ex)
                {
                    logger?.LogError(ex, "Error setting re-use on udp port.");
                }

                try
                {
                    udp.ExclusiveAddressUse = false;
                }
                catch (SocketException ex)
                {
                    logger?.LogError(ex, "Error disabling exclusivitiy on udp port.");
                }

                udp.EnableBroadcast = true;
                udp.Client.MulticastLoopback = true;

                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    udp.JoinMulticastGroup(IPNetAddress.SSDPMulticastIPv4, address);
                    // udp.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, address.GetAddressBytes());
                }
                else
                {
                    if (IPObject.IsIPv6LinkLocal(address))
                    {
                        udp.JoinMulticastGroup((int)address.ScopeId, IPNetAddress.SSDPMulticastIPv6LinkLocal);
                    }
                    else
                    {
                        udp.JoinMulticastGroup((int)address.ScopeId, IPNetAddress.SSDPMulticastIPv6SiteLocal);
                    }

                    // udp.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, address.GetAddressBytes());
                }

                udp.IsMulticast = true;
                udp.Client.Bind(udp.LocalEndPoint);

                udp.BeginReceive(new AsyncCallback(OnReceive), udp);

                return udp;
            }
            catch (SocketException ex)
            {
                logger?.LogError(ex, "Unable to create multicast client.");
                return null;
            }
        }

        /// <summary>
        /// Sends a packet via unicast.
        /// </summary>
        /// <param name="client">The <see cref="UdpProcess"/> to use.</param>
        /// <param name="packet">Packet to send.</param>
        /// <param name="remote">The destination <see cref="IPEndPoint"/>.</param>
        /// <param name="sendCount">Optional number of times to transmit <paramref name="packet"/>. Default is 1.</param>
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

            if (client.LocalEndPoint.AddressFamily != remote.AddressFamily)
            {
                throw new ArgumentException($"Address families don't match. {client.LocalEndPoint} {remote}");
            }

            byte[] buffer = Encoding.UTF8.GetBytes(packet);

            if (client.Tracing)
            {
                client.Track("->{Endpoint} : {Remote} count:{Count}\r\n{Packet}", client.LocalEndPoint, remote, sendCount, packet);
            }

            for (int a = 0; a <= sendCount - 1; a++)
            {
                try
                {
                    await client.SendAsync(buffer, buffer.Length, remote).ConfigureAwait(false);
                }
                catch (SocketException ex)
                {
                    client.Logger?.LogDebug(ex, "Error sending on {Remote}", remote);
                }
            }
        }

        /// <summary>
        /// Sends a packet via multicast over multiple sockets.
        /// </summary>
        /// <param name="clients">The <see cref="List{UdpProcess}"/> to use.</param>
        /// <param name="port">UDP port number to use.</param>
        /// <param name="packet">Packet to send.</param>
        /// <param name="sendCount">Optional. The number of times to transmit <paramref name="packet" />. Default is 1.</param>
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
        /// <param name="client">The <see cref="UdpProcess"/> to use.</param>
        /// <param name="port">UDP destination port.</param>
        /// <param name="packet">Packet to send.</param>
        /// <param name="sendCount">Optional. The number of times to transmit <paramref name="packet" />. Default is 1.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task SendMulticast(UdpProcess client, int port, string packet, int sendCount = 1)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (!client.IsMulticast)
            {
                throw new ArgumentException("Client is not a multicast client.");
            }

            var intf = client.LocalEndPoint.Address;
            IPEndPoint? mcast;
            byte[] buffer = Encoding.UTF8.GetBytes(packet);
            if (intf.AddressFamily == AddressFamily.InterNetwork)
            {
                mcast = new IPEndPoint(IPNetAddress.SSDPMulticastIPv4, port);
            }
            else
            {
                mcast = new IPEndPoint(IPObject.IsIPv6LinkLocal(intf) ? IPNetAddress.SSDPMulticastIPv6LinkLocal : IPNetAddress.SSDPMulticastIPv6SiteLocal, port);
            }

            if (client.Tracing)
            {
                client.Track("->{Endpoint} : {Multicast} count:{Count}\r\n{Packet}", client.LocalEndPoint, mcast!, sendCount, packet);
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
                client.Logger?.LogDebug(ex, "Error sending to {Interface}:{Muliticast}", intf, mcast);
            }
        }

        /// <summary>
        /// Disposes an UDP client.
        /// </summary>
        /// <param name="client">A <see cref="UdpProcess"/>.</param>
        public static void DisposeClient(UdpProcess? client)
        {
            client?.Dispose();
        }

        /// <summary>
        /// Disposes multiple of UDP clients.
        /// </summary>
        /// <param name="clients">A <see cref="List{UdpProcess}"/>.</param>
        public static void DisposeClients(List<UdpProcess>? clients)
        {
            if (clients == null)
            {
                return;
            }

            foreach (var client in clients)
            {
                DisposeClient(client);
            }
        }

        /// <summary>
        /// Creates a socket for use.
        /// </summary>
        /// <param name="address">An <see cref="IPAddress"/> containing the address of socket.</param>
        /// <param name="logger">Optional. An <see cref="ILogger"/> instance.</param>
        /// <returns>A <see cref="Socket"/> instance.</returns>
        private static Socket PrepareSocket(IPAddress address, ILogger? logger)
        {
            Socket retVal;

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                retVal = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }
            else
            {
                retVal = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            }

            try
            {
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            }
            catch (SocketException ex)
            {
                logger?.LogWarning(ex, "Error setting socket as reusable. {Address}", address);
            }

            try
            {
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
            }
            catch (SocketException ex)
            {
                logger?.LogWarning(ex, "Error setting socket as non exclusive. {Address}", address);
            }

            return retVal;
        }

        /// <summary>
        /// Processing function for incoming packets.
        /// </summary>
        /// <param name="result">An <see cref="IAsyncResult"/> containing the <see cref="UdpProcess"/>.</param>
        private static void OnReceive(IAsyncResult result)
        {
            if (result.AsyncState == null)
            {
                return;
            }

            UdpProcess client = (UdpProcess)result.AsyncState;

            if (client.Processor == null)
            {
                IPEndPoint? remote = null;
                client.EndReceive(result, ref remote);
                // If we are not interesting in inbound data, then ignore it.
                client.BeginReceive(new AsyncCallback(OnReceive), client);
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
                        client.Track("<- {EndPoint} : {Remote}\r\n{Data}", client.LocalEndPoint, remote!, data);
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
                    client.Logger?.LogError(ex, "Error processing message. {Data}", data);
                }

                client.BeginReceive(new AsyncCallback(OnReceive), client);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                client.OnFailure?.Invoke(client, ex, $"Error listening to {client.LocalEndPoint}");
            }
        }
    }
}
