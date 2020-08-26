using System.Net;
using System.Net.Http;
using System.Text;

namespace Emby.Dlna.Net.EventArgs
{
    /// <summary>
    /// Provides arguments for the <see cref="SocketServer.RequestReceived"/> event.
    /// </summary>
    /// <remarks>
    /// Part of this code take from RSSDP.
    /// Copyright (c) 2015 Troy Willmot.
    /// Copyright (c) 2015-2018 Luke Pulverenti.
    /// </remarks>
    public sealed class RequestReceivedEventArgs : System.EventArgs
    {
        private readonly byte[] _raw;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="rawData">The pre-processed raw data.</param>
        /// <param name="message">Request message.</param>
        /// <param name="receivedFrom">Received from.</param>
        /// <param name="localIPAddress">Interface IP Address upon which it was received.</param>
        /// <param name="simulated">True if the message didn't arrive via JF UDP.</param>
        public RequestReceivedEventArgs(string rawData, HttpRequestMessage message, IPEndPoint receivedFrom, IPAddress localIPAddress, bool simulated)
        {
            _raw = Encoding.UTF8.GetBytes(rawData);
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
        /// Gets the Local IP Address.
        /// </summary>
        public IPAddress LocalIPAddress { get; }

        /// <summary>
        /// Gets the <see cref="HttpRequestMessage"/> that was received.
        /// </summary>
        public HttpRequestMessage Message { get; }

        /// <summary>
        /// Gets the <see cref="IPEndPoint"/> the request came from.
        /// </summary>
        public IPEndPoint ReceivedFrom { get; }

        /// <summary>
        /// Gets the pre-processed raw data.
        /// </summary>
        /// <returns>A Byte array containing the original message.</returns>
        public byte[] Raw()
        {
            return _raw;
        }
    }
}
