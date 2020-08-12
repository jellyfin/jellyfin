#nullable enable
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Emby.Dlna.Rssdp.EventArgs;

namespace Emby.Dlna.Rsddp
{
    /// <summary>
    /// Interface for a component that manages network communication (sending and receiving HTTPU messages) for the SSDP protocol.
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
        /// Sends a message to the SSDP multicast address and port.
        /// </summary>
        /// <param name="messageData">The mesage to send.</param>
        /// <param name="destination">The destination endpoint.</param>
        /// <param name="from">The interface ip to use.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SendMessageAsync(byte[] messageData, IPEndPoint destination, IPAddress from);

        /// <summary>
        /// Sends a message to a particular address (unicast or multicast) via all available sockets.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="from">The destination address.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SendMulticastMessageAsync(string message, IPAddress from);

        /// <summary>
        /// Sends a message to a particular address (unicast or multicast) via all available sockets.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="sendCount">The number of times to transmit.</param>
        /// <param name="from">The interface ip to use.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SendMulticastMessageAsync(string message, int sendCount, IPAddress from);

        /// <summary>
        /// Processes an SSDP message.
        /// </summary>
        /// <param name="data">The data to process.</param>
        /// <param name="endPoint">The remote endpoint.</param>
        /// <param name="localIp">The interface ip upon which it was receieved.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task ProcessMessage(string data, IPEndPoint endPoint, IPAddress localIp);

        public void Dispose();
    }
}
