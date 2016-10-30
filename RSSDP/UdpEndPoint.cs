using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rssdp.Infrastructure
{
	/// <summary>
	/// Cross platform representation of a UDP end point, being an IP address (either IPv4 or IPv6) and a port.
	/// </summary>
	public sealed class UdpEndPoint
	{

		/// <summary>
		/// The IP Address of the end point.
		/// </summary>
		/// <remarks>
		/// <para>Can be either IPv4 or IPv6, up to the code using this instance to determine which was provided.</para>
		/// </remarks>
		public string IPAddress { get; set; }

		/// <summary>
		/// The port of the end point.
		/// </summary>
		public int Port { get; set; }

		/// <summary>
		/// Returns the <see cref="IPAddress"/> and <see cref="Port"/> values separated by a colon.
		/// </summary>
		/// <returns>A string containing <see cref="IPAddress"/>:<see cref="Port"/>.</returns>
		public override string ToString()
		{
			return (this.IPAddress ?? String.Empty) + ":" + this.Port.ToString();
		}
	}
}
