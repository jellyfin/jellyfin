using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Emby.Server.Implementations.Networking;
using MediaBrowser.Model.Net;

namespace Emby.Server.Implementations.Net
{
    public class SocketFactory : ISocketFactory
    {
        // THIS IS A LINKED FILE - SHARED AMONGST MULTIPLE PLATFORMS
        // Be careful to check any changes compile and work for all platform projects it is shared in.

        // Not entirely happy with this. Would have liked to have done something more generic/reusable,
        // but that wasn't really the point so kept to YAGNI principal for now, even if the
        // interfaces are a bit ugly, specific and make assumptions.

        public ISocket CreateTcpSocket(IPAddress remoteAddress, int remotePort)
        {
            if (remotePort < 0)
            {
                throw new ArgumentException("remotePort cannot be less than zero.", nameof(remotePort));
            }

            var addressFamily = remoteAddress.AddressFamily == AddressFamily.InterNetwork
                ? AddressFamily.InterNetwork
                : AddressFamily.InterNetworkV6;

            var retVal = new Socket(addressFamily, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);

            try
            {
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            }
            catch (SocketException)
            {
                // This is not supported on all operating systems (qnap)
            }

            try
            {
                return new UdpSocket(retVal, new IPEndPoint(remoteAddress, remotePort));
            }
            catch
            {
                retVal?.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Creates a new UDP acceptSocket and binds it to the specified local port.
        /// </summary>
        /// <param name="localPort">An integer specifying the local port to bind the acceptSocket to.</param>
        public ISocket CreateUdpSocket(int localPort)
        {
            if (localPort < 0)
            {
                throw new ArgumentException("localPort cannot be less than zero.", nameof(localPort));
            }

            var retVal = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            try
            {
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                return new UdpSocket(retVal, localPort, IPAddress.Any);
            }
            catch
            {
                retVal?.Dispose();

                throw;
            }
        }

        public ISocket CreateUdpBroadcastSocket(int localPort)
        {
            if (localPort < 0)
            {
                throw new ArgumentException("localPort cannot be less than zero.", nameof(localPort));
            }

            var retVal = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            try
            {
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);

                return new UdpSocket(retVal, localPort, IPAddress.Any);
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
        /// <returns>An implementation of the <see cref="ISocket"/> interface used by RSSDP components to perform acceptSocket operations.</returns>
        public ISocket CreateSsdpUdpSocket(IPAddress localIpAddress, int localPort)
        {
            if (localPort < 0)
            {
                throw new ArgumentException("localPort cannot be less than zero.", nameof(localPort));
            }

            var retVal = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            try
            {
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 4);

                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPAddress.Parse("239.255.255.250"), localIpAddress));
                return new UdpSocket(retVal, localPort, localIpAddress);
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
        /// <param name="ipAddress">The multicast IP address to make the acceptSocket a member of.</param>
        /// <param name="multicastTimeToLive">The multicast time to live value for the acceptSocket.</param>
        /// <param name="localPort">The number of the local port to bind to.</param>
        /// <returns></returns>
        public ISocket CreateUdpMulticastSocket(string ipAddress, int multicastTimeToLive, int localPort)
        {
            if (ipAddress == null)
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            if (ipAddress.Length == 0)
            {
                throw new ArgumentException("ipAddress cannot be an empty string.", nameof(ipAddress));
            }

            if (multicastTimeToLive <= 0)
            {
                throw new ArgumentException("multicastTimeToLive cannot be zero or less.", nameof(multicastTimeToLive));
            }

            if (localPort < 0)
            {
                throw new ArgumentException("localPort cannot be less than zero.", nameof(localPort));
            }

            var retVal = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);

            try
            {
                // not supported on all platforms. throws on ubuntu with .net core 2.0
                retVal.ExclusiveAddressUse = false;
            }
            catch (SocketException)
            {

            }

            try
            {
                // seeing occasional exceptions thrown on qnap
                // System.Net.Sockets.SocketException (0x80004005): Protocol not available
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            }
            catch (SocketException)
            {

            }

            try
            {
                //retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, multicastTimeToLive);

                var localIp = IPAddress.Any;

                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPAddress.Parse(ipAddress), localIp));
                retVal.MulticastLoopback = true;

                return new UdpSocket(retVal, localPort, localIp);
            }
            catch
            {
                retVal?.Dispose();

                throw;
            }
        }

        public Stream CreateNetworkStream(ISocket socket, bool ownsSocket)
            => new NetworkStream(((UdpSocket)socket).Socket, ownsSocket);
    }
}
