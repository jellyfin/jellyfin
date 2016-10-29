using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rssdp.Infrastructure
{
	/// <summary>
	/// Provides a common interface across platforms for UDP sockets used by this SSDP implementation.
	/// </summary>
	public interface IUdpSocket : IDisposable
	{
		/// <summary>
		/// Waits for and returns the next UDP message sent to this socket (uni or multicast).
		/// </summary>
		/// <returns></returns>
		System.Threading.Tasks.Task<ReceivedUdpData> ReceiveAsync();

        /// <summary>
        /// Sends a UDP message to a particular end point (uni or multicast).
        /// </summary>
        /// <param name="messageData">The data to send.</param>
        /// <param name="endPoint">The <see cref="UdpEndPoint"/> providing the address and port to send to.</param>
        Task SendTo(byte[] messageData, UdpEndPoint endPoint);

	}
}