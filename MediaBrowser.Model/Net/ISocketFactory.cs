using System.Net;
using System.Net.Sockets;

namespace MediaBrowser.Model.Net;

/// <summary>
/// Implemented by components that can create specific socket configurations.
/// </summary>
public interface ISocketFactory
{
    /// <summary>
    /// Creates a new unicast socket using the specified local port number.
    /// </summary>
    /// <param name="localPort">The local port to bind to.</param>
    /// <returns>A new unicast socket using the specified local port number.</returns>
    Socket CreateUdpBroadcastSocket(int localPort);

    /// <summary>
    /// Creates a new unicast socket using the specified local port number.
    /// </summary>
    /// <param name="bindInterface">The bind interface.</param>
    /// <param name="localPort">The local port to bind to.</param>
    /// <returns>A new unicast socket using the specified local port number.</returns>
    Socket CreateSsdpUdpSocket(IPData bindInterface, int localPort);

    /// <summary>
    /// Creates a new multicast socket using the specified multicast IP address, multicast time to live and local port.
    /// </summary>
    /// <param name="multicastAddress">The multicast IP address to bind to.</param>
    /// <param name="bindInterface">The bind interface.</param>
    /// <param name="multicastTimeToLive">The multicast time to live value. Actually a maximum number of network hops for UDP packets.</param>
    /// <param name="localPort">The local port to bind to.</param>
    /// <returns>A new multicast socket using the specfied bind interface, multicast address, multicast time to live and port.</returns>
    Socket CreateUdpMulticastSocket(IPAddress multicastAddress, IPData bindInterface, int multicastTimeToLive, int localPort);
}
