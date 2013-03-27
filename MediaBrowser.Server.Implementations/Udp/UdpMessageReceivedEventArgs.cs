using System;

namespace MediaBrowser.Server.Implementations.Udp
{
    /// <summary>
    /// Class UdpMessageReceivedEventArgs
    /// </summary>
    public class UdpMessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the bytes.
        /// </summary>
        /// <value>The bytes.</value>
        public byte[] Bytes { get; set; }
        /// <summary>
        /// Gets or sets the remote end point.
        /// </summary>
        /// <value>The remote end point.</value>
        public string RemoteEndPoint { get; set; }
    }
}
