#pragma warning disable CS1591

using System;
using System.Net;
using System.Net.Sockets;
using MediaBrowser.Model.Net;

namespace Emby.Server.Implementations.Net
{
    public class SocketFactory : ISocketFactory
    {
        /// <inheritdoc />
        public ISocket CreateUdpBroadcastSocket(int localPort)
        {
            if (localPort < 0)
            {
                throw new ArgumentException("localPort cannot be less than zero.", nameof(localPort));
            }

            var retVal = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                retVal.EnableBroadcast = true;
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

        /// <inheritdoc />
        public ISocket CreateSsdpUdpSocket(IPAddress localIp, int localPort)
        {
            if (localPort < 0)
            {
                throw new ArgumentException("localPort cannot be less than zero.", nameof(localPort));
            }

            var retVal = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                retVal.EnableBroadcast = true;
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 4);

                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPAddress.Parse("239.255.255.250"), localIp));
                return new UdpSocket(retVal, localPort, localIp);
            }
            catch
            {
                retVal?.Dispose();

                throw;
            }
        }

        /// <inheritdoc />
        public ISocket CreateUdpMulticastSocket(IPAddress ipAddress, int multicastTimeToLive, int localPort)
        {
            ArgumentNullException.ThrowIfNull(ipAddress);

            if (multicastTimeToLive <= 0)
            {
                throw new ArgumentException("multicastTimeToLive cannot be zero or less.", nameof(multicastTimeToLive));
            }

            if (localPort < 0)
            {
                throw new ArgumentException("localPort cannot be less than zero.", nameof(localPort));
            }

            var retVal = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            retVal.ExclusiveAddressUse = false;

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
                retVal.EnableBroadcast = true;
                // retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, multicastTimeToLive);

                var localIp = IPAddress.Any;

                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ipAddress, localIp));
                retVal.MulticastLoopback = true;

                return new UdpSocket(retVal, localPort, localIp);
            }
            catch
            {
                retVal?.Dispose();

                throw;
            }
        }
    }
}
