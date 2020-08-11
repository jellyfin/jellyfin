using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Rssdp.Events;

namespace Rssdp.Infrastructure
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
        /// Sends a message to a particular address (uni or multicast) and port.
        /// </summary>
        Task SendMessageAsync(byte[] messageData, IPEndPoint destination, IPAddress from);

        /// <summary>
        /// Sends a message to the SSDP multicast address and port.
        /// </summary>
        Task SendMulticastMessageAsync(string message, IPAddress from);
        Task SendMulticastMessageAsync(string message, int sendCount, IPAddress from);

        /// <summary>
        /// Processes an SSDP message.
        /// </summary>
        /// <param name="data">The data to process.</param>
        /// <param name="endPoint">The remote endpoint.</param>
        /// <param name="localIp">The interface ip upon which it was receieved.</param>
        public Task ProcessMessage(string data, IPEndPoint endPoint, IPAddress localIp);

        public void Dispose();
    }
}
