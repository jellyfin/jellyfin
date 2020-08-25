using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Emby.Dlna.Net.EventArgs;

namespace Emby.Dlna.Net
{
    /// <summary>
    /// Interface for SocketServer class.
    /// </summary>
    public interface ISocketServer
    {
        /// <summary>
        /// Raised when a HTTPU request message is received by a socket (unicast or multicast).
        /// </summary>
        event EventHandler<RequestReceivedEventArgs> RequestReceived;

         /// <summary>
        /// Raised when an HTTPU response message is received by a socket (unicast or multicast).
        /// </summary>
        event EventHandler<ResponseReceivedEventArgs> ResponseReceived;

        /// <summary>
        /// Gets a value indicating whether is multi-socket binding available.
        /// </summary>
        bool EnableMultiSocketBinding { get; }

        /// <summary>
        /// Gets a value indicating whether IP4 is enabled.
        /// </summary>
        bool IsIP4Enabled { get; }

        /// <summary>
        /// Gets a value indicating whether IP6 is enabled.
        /// </summary>
        bool IsIP6Enabled { get; }

        /// <summary>
        /// Gets the number of times each udp packet should be sent.
        /// </summary>
        int ResendCount { get; }

        /// <summary>
        /// Gets a value indicating whether detailed DNLA debug logging is active.
        /// </summary>
        bool Tracing { get; }

        /// <summary>
        /// Creates an UDP Socket.
        /// </summary>
        /// <param name="port">UDP port to bind.</param>
        /// <returns>A Socket.</returns>
        Socket CreateUdpBroadcastSocket(int port);

        /// <summary>
        /// Creates a new UDP acceptSocket that is a member of the SSDP multicast local admin group and binds it to the specified local port.
        /// </summary>
        /// <param name="address">IP Address to bind.</param>
        /// <param name="port">UDP port to bind.</param>
        /// <returns>A Socket.</returns>
        Socket CreateUdpMulticastSocket(IPAddress address, int port);

        /// <summary>
        /// Dispose function.
        /// This object will only dispose if it has no event listeners.
        /// </summary>
        void Dispose();

        /// <summary>
        /// Returns the correct multicast address based upon the IsIPEnabled.
        /// </summary>
        /// <param name="port">Port to use.</param>
        /// <returns>IPEndpoint set to the port provided.</returns>
        IPEndPoint GetMulticastEndPoint(int port);

        /// <summary>
        /// Returns a udp port based upon Configuration.UDPPort.
        /// </summary>
        /// <param name="portStr">Port Range, or empty/zero for a random port.</param>
        /// <returns>System.Int32.</returns>
        int GetPort(string portStr);

        /// <summary>
        /// Processes a SSDP message.
        /// </summary>
        /// <param name="data">The data to process.</param>
        /// <param name="receivedFrom">The remote endpoint.</param>
        /// <param name="localIPAddress">The interface ip upon which it was receieved.</param>
        /// <param name="sourceInternal">True if the data didn't arrive through JF's UDP ports.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task ProcessMessage(string data, IPEndPoint receivedFrom, IPAddress localIPAddress, bool sourceInternal);

        /// <summary>
        /// Sends a message to the SSDP multicast address and port.
        /// </summary>
        /// <param name="message">The mesage to send.</param>
        /// <param name="localIPAddress">The interface ip to use.</param>
        /// <param name="endPoint">The destination endpoint.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SendMessageAsync(string message, IPAddress localIPAddress, IPEndPoint endPoint);

        /// <summary>
        /// Sends a message to the SSDP multicast address and port.
        /// </summary>
        /// <param name="message">The mesage to send.</param>
        /// <param name="sendCount">The number of times to send it.</param>
        /// <param name="localIPAddress">The interface ip to use.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SendMulticastMessageAsync(string message, int sendCount, IPAddress localIPAddress);

        /// <summary>
        /// Multicasts a message to a particular address (unicast or multicast) via all available sockets.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="localIPAddress">The interface ip address to use.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SendMulticastMessageAsync(string message, IPAddress localIPAddress);
    }
}
