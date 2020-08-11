using System;
using System.Net;
using System.Net.Http;

namespace Rssdp.Events
{
    /// <summary>
    /// Provides arguments for the <see cref="ISsdpCommunicationsServer.ResponseReceived"/> event.
    /// </summary>
    public sealed class ResponseReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Full constructor.
        /// </summary>
        public ResponseReceivedEventArgs(HttpResponseMessage message, IPEndPoint receivedFrom, IPAddress localIPAddress)
        {
            Message = message;
            ReceivedFrom = receivedFrom;
            LocalIpAddress = localIPAddress;
        }

        public IPAddress LocalIpAddress { get; }

        /// <summary>
        /// The <see cref="HttpResponseMessage"/> that was received.
        /// </summary>
        public HttpResponseMessage Message { get; }

        /// <summary>
        /// The <see cref="UdpEndPoint"/> the response came from.
        /// </summary>
        public IPEndPoint ReceivedFrom { get; }
    }
}
