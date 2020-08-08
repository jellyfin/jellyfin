using System;
using System.Net;
using System.Net.Http;

namespace Rssdp.Infrastructure
{
    /// <summary>
    /// Provides arguments for the <see cref="ISsdpCommunicationsServer.ResponseReceived"/> event.
    /// </summary>
    public sealed class ResponseReceivedEventArgs : EventArgs
    {
        private readonly HttpResponseMessage _message;
        private readonly IPEndPoint _receivedFrom;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseReceivedEventArgs"/> class.
        /// </summary>
        public ResponseReceivedEventArgs(HttpResponseMessage message, IPEndPoint receivedFrom)
        {
            _message = message;
            _receivedFrom = receivedFrom;
        }

        public IPAddress LocalIpAddress { get; set; }

        /// <summary>
        /// Gets the <see cref="HttpResponseMessage"/> that was received.
        /// </summary>
        public HttpResponseMessage Message
        {
            get { return _message; }
        }

        /// <summary>
        /// Gets the <see cref="UdpEndPoint"/> the response came from.
        /// </summary>
        public IPEndPoint ReceivedFrom
        {
            get { return _receivedFrom; }
        }
    }
}
