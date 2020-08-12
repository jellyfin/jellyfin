using System;
using System.Net;
using System.Net.Http;

namespace Emby.Dlna.Rssdp.EventArgs
{
    /// <summary>
    /// Provides arguments for the <see cref="ISsdpCommunicationsServer.RequestReceived"/> event.
    /// </summary>
    public sealed class RequestReceivedEventArgs : System.EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="message">Request message.</param>
        /// <param name="receivedFrom">Received from.</param>
        /// <param name="localIPAddress">Interface IP Address upon which it was received.</param>
        public RequestReceivedEventArgs(HttpRequestMessage message, IPEndPoint receivedFrom, IPAddress localIPAddress)
        {
            Message = message;
            ReceivedFrom = receivedFrom;
            LocalIpAddress = localIPAddress;
        }

        /// <summary>
        /// Gets the Local IP Address.
        /// </summary>
        public IPAddress LocalIpAddress { get; private set; }

        /// <summary>
        /// Gets the <see cref="HttpRequestMessage"/> that was received.
        /// </summary>
        public HttpRequestMessage Message { get; }

        /// <summary>
        /// Gets the <see cref="IPEndPoint"/> the request came from.
        /// </summary>
        public IPEndPoint ReceivedFrom { get; }
    }
}
