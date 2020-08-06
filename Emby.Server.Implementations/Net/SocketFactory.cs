#pragma warning disable CS1591

using System;
using System.Net;
using System.Net.Sockets;
using MediaBrowser.Model.Net;
using Rssdp.Infrastructure;

namespace Emby.Server.Implementations.Net
{
    /// <summary>
    /// Implementation of SocketFactory class.
    /// </summary>
    public class SocketFactory : ISocketFactory
    {
        /// <summary>
        /// Creates an UDP Socket.
        /// </summary>
        /// <param name="localPort">UDP port to bind.</param>
        /// <param name="addr">Address to use.</param>
        /// <returns>Socket interface object.</returns>
        public ISocket CreateUdpBroadcastSocket(int localPort, IPAddress addr)
        {
            if (addr == null)
            {
                throw new ArgumentNullException(nameof(addr));
            }

            if (localPort < 0)
            {
                throw new ArgumentException("localPort cannot be less than zero.", nameof(localPort));
            }

            Socket retVal;
            if (addr.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // IPv6 is enabled so create a dual IP4/IP6 socket
                retVal = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                retVal.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                retVal.DualMode = true;

                if (addr.Equals(IPAddress.IPv6Any))
                {
                    // Simulate a broadcast on IP6.
                    retVal.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastLoopback, true);
                    retVal.SetSocketOption(
                        SocketOptionLevel.IPv6,
                        SocketOptionName.AddMembership,
                        new IPv6MulticastOption(SsdpConstants.MulticastLocalAdminAddressV6));
                }
            }
            else
            {
                retVal = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }

            try
            {
                retVal.EnableBroadcast = true;
                // seeing occasional exceptions thrown on qnap
                // System.Net.Sockets.SocketException (0x80004005): Protocol not available
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            }
            catch (SocketException)
            {
            }

            try
            {
                // Without this an access denied occurs on some systems.
                retVal.EnableBroadcast = true;

                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                return new UdpSocket(retVal, localPort, addr);
            }
            catch
            {
                retVal?.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Creates a new UDP acceptSocket that is a member of the SSDP multicast local admin group and binds it to the specified local port.
        /// </summary>
        /// <param name="localIP">IP Address to bind.</param>
        /// <param name="localPort">UDP port to bind.</param>
        /// <returns>An implementation of the <see cref="ISocket"/> interface used by RSSDP components to perform acceptSocket operations.</returns>
        public ISocket CreateSsdpUdpSocket(IPAddress localIP, int localPort)
        {
            if (localIP == null)
            {
                throw new ArgumentNullException(nameof(localIP));
            }

            if (localPort < 0)
            {
                throw new ArgumentException("localPort cannot be less than zero.", nameof(localPort));
            }

            Socket retVal;

            if (localIP.Equals(IPAddress.IPv6Any))
            {
                // IPv6 is enabled so create a dual IP4/IP6 socket
                retVal = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                retVal.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                retVal.DualMode = true;
                localIP = IPAddress.Any;
            }
            else
            {
                retVal = new Socket(localIP.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            }

            try
            {
                retVal.EnableBroadcast = true;
                // seeing occasional exceptions thrown on qnap
                // System.Net.Sockets.SocketException (0x80004005): Protocol not available
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            }
            catch (SocketException)
            {
            }

            try
            {
                // Without this an access denied occurs on some systems.
                retVal.EnableBroadcast = true;
                if (localIP.AddressFamily == AddressFamily.InterNetwork)
                {
                    retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, SsdpConstants.SsdpDefaultMulticastTimeToLive);
                    retVal.SetSocketOption(
                        SocketOptionLevel.IP,
                        SocketOptionName.AddMembership,
                        new MulticastOption(SsdpConstants.MulticastLocalAdminAddress, localIP));
                }
                else
                {
                    retVal.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastTimeToLive, SsdpConstants.SsdpDefaultMulticastTimeToLive);
                    retVal.SetSocketOption(
                        SocketOptionLevel.IPv6,
                        SocketOptionName.AddMembership,
                        new IPv6MulticastOption(SsdpConstants.MulticastLocalAdminAddressV6));
                }

                return new UdpSocket(retVal, localPort, localIP);
            }
            catch
            {
                retVal?.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Creates a new UDP acceptSocket that is a member of the specified multicast IP address, and binds it to the specified local port.
        /// </summary>
        /// <param name="isIP6Enabled">True if IP6 is enabled.</param>
        /// <param name="multicastTimeToLive">The multicast time to live value for the acceptSocket.</param>
        /// <returns>Socket interface object.</returns>
        public ISocket CreateUdpMulticastSocket(bool isIP6Enabled, int multicastTimeToLive)
        {
            Socket retVal;

            if (isIP6Enabled)
            {
                // IPv6 is enabled so create a dual IP4/IP6 socket
                retVal = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                retVal.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                retVal.DualMode = true;
            }
            else
            {
                retVal = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }

            try
            {
                // not supported on all platforms. throws on ubuntu with .net core 2.0
                retVal.ExclusiveAddressUse = false;
            }
            catch (SocketException)
            {
                // Ignore.
            }

            try
            {
                // Seeing occasional exceptions thrown on qnap : System.Net.Sockets.SocketException (0x80004005): Protocol not available
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            }
            catch (SocketException)
            {
                // Ignore.
            }

            try
            {
                // Without this an access denied occurs on some systems.
                retVal.EnableBroadcast = true;
                retVal.MulticastLoopback = true;

                IPAddress localIp;
                if (isIP6Enabled)
                {
                    localIp = IPAddress.IPv6Any;
                    retVal.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastTimeToLive, multicastTimeToLive);
                    retVal.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(SsdpConstants.MulticastLocalAdminAddressV6));
                }
                else
                {
                    localIp = IPAddress.Any;
                    retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, multicastTimeToLive);
                    retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(SsdpConstants.MulticastLocalAdminAddress, 1900));
                }

                return new UdpSocket(retVal, 1900, localIp);
            }
            catch
            {
                retVal?.Dispose();

                throw;
            }
        }
    }
}
