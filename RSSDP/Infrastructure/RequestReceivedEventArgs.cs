using System;
using System.Net;
using System.Net.Http;

namespace Rssdp.Infrastructure
{
    /// <summary>
    /// Provides arguments for the <see cref="ISsdpCommunicationsServer.RequestReceived"/> event.
    /// </summary>
    public sealed class RequestReceivedEventArgs : EventArgs
    {
        private readonly HttpRequestMessage _message;
        private readonly IPEndPoint _receivedFrom;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestReceivedEventArgs"/> class.
        /// </summary>
        public RequestReceivedEventArgs(HttpRequestMessage message, IPEndPoint receivedFrom, IPAddress localIpAddress)
        {
            _message = message;
            _receivedFrom = receivedFrom;
            LocalIpAddress = localIpAddress;
        }

        public IPAddress LocalIpAddress { get; private set; }

        /// <summary>
        /// Gets the <see cref="HttpRequestMessage"/> that was received.
        /// </summary>
        public HttpRequestMessage Message
        {
            get { return _message; }
        }

        /// <summary>
        /// Gets the <see cref="UdpEndPoint"/> the request came from.
        /// </summary>
        public IPEndPoint ReceivedFrom
        {
            get { return _receivedFrom; }
        }
    }
}
