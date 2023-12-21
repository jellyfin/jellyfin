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
}
