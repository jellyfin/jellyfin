using System;
using System.Net;
using System.Net.Http;

namespace Emby.Dlna.Rssdp.EventArgs
{
    /// <summary>
    /// Provides arguments for the <see cref="ISsdpCommunicationsServer.ResponseReceived"/> event.
    /// </summary>
    public sealed class ResponseReceivedEventArgs : System.EventArgs
    {
         /// <summary>
        /// Initializes a new instance of the <see cref="ResponseReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="message">Response message.</param>
        /// <param name="receivedFrom">Received from.</param>
        /// <param name="localIPAddress">Interface IP Address upon which it was received.</param>
        public ResponseReceivedEventArgs(HttpResponseMessage message, IPEndPoint receivedFrom, IPAddress localIPAddress, bool simulated)
        {
            Message = message;
            ReceivedFrom = receivedFrom;
            LocalIPAddress = localIPAddress;
            Simulated = simulated;
        }

        /// <summary>
        /// Gets a value indicating whether the data arrived through a UDP port or by other means.
        /// </summary>
        public bool Simulated { get; }

        /// <summary>
        /// Gets the interface ip upon which the message was received.
        /// </summary>
        public IPAddress LocalIPAddress { get; }

        /// <summary>
        /// Gets the <see cref="HttpResponseMessage"/> that was received.
        /// </summary>
        public HttpResponseMessage Message { get; }

        /// <summary>
        /// Gets the <see cref="UdpEndPoint"/> the response came from.
        /// </summary>
        public IPEndPoint ReceivedFrom { get; }
    }
}
