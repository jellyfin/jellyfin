using System;
using System.Net;
using System.Net.Sockets;
using MediaBrowser.Model.Net;

namespace Jellyfin.Networking.Udp;

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

        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        try
        {
            socket.EnableBroadcast = true;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            socket.Bind(new IPEndPoint(IPAddress.Any, localPort));

            return socket;
        }
        catch
        {
            socket.Dispose();

            throw;
        }
    }
}
