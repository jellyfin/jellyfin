using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using MediaBrowser.Model.Net;

namespace Emby.Server.Implementations.Net
{
    /// <summary>
    /// Factory class to create different kinds of sockets.
    /// </summary>
    public class SocketFactory : ISocketFactory
    {
        /// <inheritdoc />
        public Socket CreateUdpBroadcastSocket(int localPort)
        {
            if (localPort < 0)
            {
                throw new ArgumentException("localPort cannot be less than zero.", nameof(localPort));
            }

            int min = 33000;
            int max = 34000;
            int remainingAttempts = 1;
            Random rnd = new Random();

            if (localPort == 0 && min != 0 && max != 0)
            {
                // If a min and max is set, try to use a port within that range
                remainingAttempts = 5;
                localPort = rnd.Next(min, max);
            }

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            while (remainingAttempts > 0)
            {
                remainingAttempts--;
                try
                {
                    socket.EnableBroadcast = true;
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                    socket.Bind(new IPEndPoint(IPAddress.Any, localPort));
                    break;
                }
                catch
                {
                    socket?.Dispose();

                    if (remainingAttempts > 0)
                    {
                        // Try another port in the range, with a new socket.
                        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        localPort = rnd.Next(min, max);
                        continue;
                    }

                    throw;
                }
            }

            return socket;
        }

        /// <inheritdoc />
        public Socket CreateSsdpUdpSocket(IPData bindInterface, int localPort)
        {
            var interfaceAddress = bindInterface.Address;
            ArgumentNullException.ThrowIfNull(interfaceAddress);

            if (localPort < 0)
            {
                throw new ArgumentException("localPort cannot be less than zero.", nameof(localPort));
            }

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                socket.Bind(new IPEndPoint(interfaceAddress, localPort));

                return socket;
            }
            catch
            {
                socket.Dispose();

                throw;
            }
        }

        /// <inheritdoc />
        public Socket CreateUdpMulticastSocket(IPAddress multicastAddress, IPData bindInterface, int multicastTimeToLive, int localPort)
        {
            var bindIPAddress = bindInterface.Address;
            ArgumentNullException.ThrowIfNull(multicastAddress);
            ArgumentNullException.ThrowIfNull(bindIPAddress);

            if (multicastTimeToLive <= 0)
            {
                throw new ArgumentException("multicastTimeToLive cannot be zero or less.", nameof(multicastTimeToLive));
            }

            if (localPort < 0)
            {
                throw new ArgumentException("localPort cannot be less than zero.", nameof(localPort));
            }

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            try
            {
                socket.MulticastLoopback = false;
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, multicastTimeToLive);

                if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                {
                    socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(multicastAddress));
                    socket.Bind(new IPEndPoint(multicastAddress, localPort));
                }
                else
                {
                    // Only create socket if interface supports multicast
                    var interfaceIndex = bindInterface.Index;
                    var interfaceIndexSwapped = IPAddress.HostToNetworkOrder(interfaceIndex);

                    socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(multicastAddress, interfaceIndex));
                    socket.Bind(new IPEndPoint(bindIPAddress, localPort));
                }

                return socket;
            }
            catch
            {
                socket.Dispose();

                throw;
            }
        }
    }
}
