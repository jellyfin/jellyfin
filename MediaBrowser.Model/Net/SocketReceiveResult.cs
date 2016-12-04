
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
        /// The <see cref="IpEndPointInfo"/> the data was received from.
        /// </summary>
        public IpEndPointInfo RemoteEndPoint { get; set; }
        public IpAddressInfo LocalIPAddress { get; set; }
    }
}
