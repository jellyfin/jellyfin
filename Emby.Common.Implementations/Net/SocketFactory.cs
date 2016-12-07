using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Emby.Common.Implementations.Networking;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;

namespace Emby.Common.Implementations.Net
{
    public class SocketFactory : ISocketFactory
    {
        // THIS IS A LINKED FILE - SHARED AMONGST MULTIPLE PLATFORMS	
        // Be careful to check any changes compile and work for all platform projects it is shared in.

        // Not entirely happy with this. Would have liked to have done something more generic/reusable,
        // but that wasn't really the point so kept to YAGNI principal for now, even if the 
        // interfaces are a bit ugly, specific and make assumptions.

        private readonly ILogger _logger;

        public SocketFactory(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _logger = logger;
        }

        public ISocket CreateSocket(IpAddressFamily family, MediaBrowser.Model.Net.SocketType socketType, MediaBrowser.Model.Net.ProtocolType protocolType, bool dualMode)
        {
            try
            {
                var addressFamily = family == IpAddressFamily.InterNetwork
                    ? AddressFamily.InterNetwork
                    : AddressFamily.InterNetworkV6;

                var socket = new Socket(addressFamily, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);

                if (dualMode)
                {
                    socket.DualMode = true;
                }

                return new NetSocket(socket, _logger, dualMode);
            }
            catch (SocketException ex)
            {
                throw new SocketCreateException(ex.SocketErrorCode.ToString(), ex);
            }
        }

        #region ISocketFactory Members

        /// <summary>
        /// Creates a new UDP socket and binds it to the specified local port.
        /// </summary>
        /// <param name="localPort">An integer specifying the local port to bind the socket to.</param>
        public IUdpSocket CreateUdpSocket(int localPort)
        {
            if (localPort < 0) throw new ArgumentException("localPort cannot be less than zero.", "localPort");

            var retVal = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            try
            {
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                return new UdpSocket(retVal, localPort, IPAddress.Any);
            }
            catch
            {
                if (retVal != null)
                    retVal.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Creates a new UDP socket that is a member of the SSDP multicast local admin group and binds it to the specified local port.
        /// </summary>
        /// <returns>An implementation of the <see cref="IUdpSocket"/> interface used by RSSDP components to perform socket operations.</returns>
        public IUdpSocket CreateSsdpUdpSocket(IpAddressInfo localIpAddress, int localPort)
        {
            if (localPort < 0) throw new ArgumentException("localPort cannot be less than zero.", "localPort");

            var retVal = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            try
            {
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 4);

                var localIp = NetworkManager.ToIPAddress(localIpAddress);

                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPAddress.Parse("239.255.255.250"), localIp));
                return new UdpSocket(retVal, localPort, localIp);
            }
            catch
            {
                if (retVal != null)
                    retVal.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Creates a new UDP socket that is a member of the specified multicast IP address, and binds it to the specified local port.
        /// </summary>
        /// <param name="ipAddress">The multicast IP address to make the socket a member of.</param>
        /// <param name="multicastTimeToLive">The multicast time to live value for the socket.</param>
        /// <param name="localPort">The number of the local port to bind to.</param>
        /// <returns></returns>
        public IUdpSocket CreateUdpMulticastSocket(string ipAddress, int multicastTimeToLive, int localPort)
        {
            if (ipAddress == null) throw new ArgumentNullException("ipAddress");
            if (ipAddress.Length == 0) throw new ArgumentException("ipAddress cannot be an empty string.", "ipAddress");
            if (multicastTimeToLive <= 0) throw new ArgumentException("multicastTimeToLive cannot be zero or less.", "multicastTimeToLive");
            if (localPort < 0) throw new ArgumentException("localPort cannot be less than zero.", "localPort");

            var retVal = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);

            try
            {
#if NETSTANDARD1_3
				// The ExclusiveAddressUse socket option is a Windows-specific option that, when set to "true," tells Windows not to allow another socket to use the same local address as this socket
				// See https://github.com/dotnet/corefx/pull/11509 for more details
				if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
				{
					retVal.ExclusiveAddressUse = false;
				}
#else
                retVal.ExclusiveAddressUse = false;
#endif
                //retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, multicastTimeToLive);

                var localIp = IPAddress.Any;

                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPAddress.Parse(ipAddress), localIp));
                retVal.MulticastLoopback = true;

                return new UdpSocket(retVal, localPort, localIp);
            }
            catch
            {
                if (retVal != null)
                    retVal.Dispose();

                throw;
            }
        }

        #endregion
    }
}
