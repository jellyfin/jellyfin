using System.Net;
using System.Net.Http;
using System.Text;
using MediaBrowser.Common.Net;
using SddpMessage = System.Collections.Generic.Dictionary<string, string>;

namespace MediaBrowser.Common.Networking
{
    /// <summary>
    /// Provides arguments for the <see cref="ISsdpServer"/>.
    /// </summary>
    public sealed class SsdpEventArgs
    {
        private readonly byte[] _raw;

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpEventArgs"/> class.
        /// </summary>
        /// <param name="rawData">The pre-processed raw data.</param>
        /// <param name="message">Request message.</param>
        /// <param name="receivedFrom">Received from.</param>
        /// <param name="localIPAddress">Interface IP Address upon which it was received.</param>
        /// <param name="passedInternally">True if the message didn't arrive via JF UDP.</param>
        public SsdpEventArgs(string rawData, SddpMessage message, IPEndPoint receivedFrom, IPAddress localIPAddress, bool passedInternally)
        {
            _raw = Encoding.UTF8.GetBytes(rawData);
            Message = message;
            ReceivedFrom = receivedFrom;
            LocalIPAddress = localIPAddress;
            Internal = passedInternally;
        }

        /// <summary>
        /// Gets a value indicating whether the data arrived through a UDP port or by other means.
        /// </summary>
        public bool Internal { get; }

        /// <summary>
        /// Gets the Local IP Address.
        /// </summary>
        public IPAddress LocalIPAddress { get; }

        /// <summary>
        /// Gets the <see cref="HttpRequestMessage"/> that was received.
        /// </summary>
        public SddpMessage Message { get; }

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
