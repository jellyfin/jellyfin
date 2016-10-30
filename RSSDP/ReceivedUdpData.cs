using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rssdp.Infrastructure
{
	/// <summary>
	/// Used by the sockets wrapper to hold raw data received from a UDP socket.
	/// </summary>
	public sealed class ReceivedUdpData
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
		/// The <see cref="UdpEndPoint"/> the data was received from.
		/// </summary>
		public UdpEndPoint ReceivedFrom { get; set; }
	}
}
