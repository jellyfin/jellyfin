#pragma warning disable CS1591

using System.Net;

namespace MediaBrowser.Model.Net
{
    /// <summary>
    /// Implemented by components that can create a platform specific UDP socket implementation, and wrap it in the cross platform <see cref="ISocket"/> interface.
    /// </summary>
    public interface ISocketFactory
    {
        /// <summary>
        /// Creates an UDP Socket.
        /// </summary>
        /// <param name="localPort">UDP port to bind.</param>
        /// <param name="addr">Address to use</param>
        /// <returns>Socket interface object.</returns>
        ISocket CreateUdpBroadcastSocket(int localPort, IPAddress addr);

        /// <summary>
        /// Creates a new UDP acceptSocket that is a member of the SSDP multicast local admin group and binds it to the specified local port.
        /// </summary>
        /// <param name="localIpAddress">IP Address to bind.</param>
        /// <param name="localPort">UDP port to bind.</param>
        /// <returns>An implementation of the <see cref="ISocket"/> interface used by RSSDP components to perform acceptSocket operations.</returns>
        ISocket CreateSsdpUdpSocket(IPAddress localIp, int localPort);

        /// <summary>
        /// Creates a new multicast socket using the specified multicast IP address, multicast time to live and local port.
        /// </summary>
        /// <param name="ipAddress">The multicast IP address to bind to.</param>
        /// <param name="multicastTimeToLive">The multicast time to live value. Actually a maximum number of network hops for UDP packets.</param>
        /// <param name="localPort">The local port to bind to.</param>
        /// <returns>Socket interface object.</returns>
        ISocket CreateUdpMulticastSocket(string ipAddress, int multicastTimeToLive, int localPort);
    }
}
