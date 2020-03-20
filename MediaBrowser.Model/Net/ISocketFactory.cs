#pragma warning disable CS1591

using System.Net;

namespace MediaBrowser.Model.Net
{
    /// <summary>
    /// Implemented by components that can create a platform specific UDP socket implementation, and wrap it in the cross platform <see cref="ISocket"/> interface.
    /// </summary>
    public interface ISocketFactory
    {
        ISocket CreateUdpBroadcastSocket(int localPort);

        /// <summary>
        /// Creates a new unicast socket using the specified local port number.
        /// </summary>
        ISocket CreateSsdpUdpSocket(IPAddress localIp, int localPort);

        /// <summary>
        /// Creates a new multicast socket using the specified multicast IP address, multicast time to live and local port.
        /// </summary>
        /// <param name="ipAddress">The multicast IP address to bind to.</param>
        /// <param name="multicastTimeToLive">The multicast time to live value. Actually a maximum number of network hops for UDP packets.</param>
        /// <param name="localPort">The local port to bind to.</param>
        /// <returns>A <see cref="ISocket"/> implementation.</returns>
        ISocket CreateUdpMulticastSocket(string ipAddress, int multicastTimeToLive, int localPort);
    }
}
