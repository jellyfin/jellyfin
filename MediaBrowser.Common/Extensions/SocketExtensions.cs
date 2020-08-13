#nullable enable
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MediaBrowser.Common.Extensions
{
    /// <summary>
    /// Socket extension to ease code reading.
    /// </summary>
    public static class SocketExtensions
    {
        /// <summary>
        /// Retreives the sockets local ip address.
        /// </summary>
        /// <param name="socket">The Socket instance.</param>
        /// <returns>Sockets LocalEndPoint IP Address.</returns>
        public static IPAddress LocalAddress(this Socket socket) => ((IPEndPoint)socket.LocalEndPoint).Address;

        /// <summary>
        /// Retreives the sockets remote ip address.
        /// </summary>
        /// <param name="socket">The Socket instance.</param>
        /// <returns>Sockets RemoteEndPoint IP Address.</returns>
        public static IPAddress RemoteAddress(this Socket socket) => ((IPEndPoint)socket.RemoteEndPoint).Address;

        /// <summary>
        /// Compares the local endpoint ip address with the ip address provided.
        /// </summary>
        /// <param name="socket">The Socket instance.</param>
        /// <param name="ipAddress">The IP Address.</param>
        /// <returns>Equality result.</returns>
        public static bool LocalAddressEquals(this Socket socket, IPAddress ipAddress)
        {
            return ((IPEndPoint)socket.LocalEndPoint).Address.Equals(ipAddress);
        }
    }
}
