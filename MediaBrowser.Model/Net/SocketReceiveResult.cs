#pragma warning disable CS1591
#pragma warning disable SA1600

using System.Net;

namespace MediaBrowser.Model.Net
{
    /// <summary>
    /// Used by the sockets wrapper to hold raw data received from a UDP socket.
    /// </summary>
    public sealed class SocketReceiveResult
    {
        /// <summary>
        /// The buffer to place received data into.
        /// </summary>
        public byte[] Buffer { get; set; }

        /// <summary>
        /// The number of bytes received.
        /// </summary>
        public int ReceivedBytes { get; set; }

        /// <summary>
        /// The <see cref="IPEndPoint"/> the data was received from.
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; set; }
        public IPAddress LocalIPAddress { get; set; }
    }
}
