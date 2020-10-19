#nullable disable

using System.Net;

namespace MediaBrowser.Model.Net
{
    /// <summary>
    /// Used by the sockets wrapper to hold raw data received from a UDP socket.
    /// </summary>
    public sealed class SocketReceiveResult
    {
        /// <summary>
        /// Gets or sets the buffer to place received data into.
        /// </summary>
        public byte[] Buffer { get; set; }

        /// <summary>
        /// Gets or sets the number of bytes received.
        /// </summary>
        public int ReceivedBytes { get; set; }

        /// <summary>
        /// The <see cref="IPEndPoint"/> the data was received from.
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; set; }

        /// <summary>
        /// The local <see cref="IPAddress"/>.
        /// </summary>
        public IPAddress LocalIPAddress { get; set; }
    }
}
