#nullable enable
using System.Net;
using System.Text;
using SddpMessage = System.Collections.Generic.Dictionary<string, string>;

namespace Emby.Dlna.Ssdp
{
    /// <summary>
    /// Ssdp arguments class.
    /// </summary>
    public sealed class SsdpEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpEventArgs"/> class.
        /// </summary>
        /// <param name="message">Request message.</param>
        /// <param name="receivedFrom">Received from.</param>
        /// <param name="localIpAddress">Interface IP Address upon which it was received.</param>
        public SsdpEventArgs(SddpMessage message, IPEndPoint receivedFrom, IPAddress localIpAddress)
        {
            Message = message;
            ReceivedFrom = receivedFrom;
            LocalIpAddress = localIpAddress;
        }

        /// <summary>
        /// Gets the Local IP Address.
        /// </summary>
        public IPAddress LocalIpAddress { get; }

        /// <summary>
        /// Gets the <see cref="SddpMessage"/> that was received.
        /// </summary>
        public SddpMessage Message { get; }

        /// <summary>
        /// Gets the <see cref="IPEndPoint"/> the request came from.
        /// </summary>
        public IPEndPoint ReceivedFrom { get; }
    }
}
