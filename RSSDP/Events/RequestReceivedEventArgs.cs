using System;
using System.Net;
using System.Net.Http;

namespace Rssdp.Events
{
    /// <summary>
    /// Provides arguments for the <see cref="ISsdpCommunicationsServer.RequestReceived"/> event.
    /// </summary>
    public sealed class RequestReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Full constructor.
        /// </summary>
        public RequestReceivedEventArgs(HttpRequestMessage message, IPEndPoint receivedFrom, IPAddress localIpAddress)
        {
            Message = message;
            ReceivedFrom = receivedFrom;
            LocalIpAddress = localIpAddress;
        }
        public IPAddress LocalIpAddress { get; private set; }

        /// <summary>
        /// The <see cref="HttpRequestMessage"/> that was received.
        /// </summary>
        public HttpRequestMessage Message { get; }

        /// <summary>
        /// The <see cref="UdpEndPoint"/> the request came from.
        /// </summary>
        public IPEndPoint ReceivedFrom { get; }
    }
}
