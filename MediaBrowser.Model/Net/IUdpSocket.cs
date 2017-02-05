using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Model.Net
{
    /// <summary>
    /// Provides a common interface across platforms for UDP sockets used by this SSDP implementation.
    /// </summary>
    public interface IUdpSocket : IDisposable
    {
        IpAddressInfo LocalIPAddress { get; }

        /// <summary>
        /// Waits for and returns the next UDP message sent to this socket (uni or multicast).
        /// </summary>
        /// <returns></returns>
        Task<SocketReceiveResult> ReceiveAsync();

        /// <summary>
        /// Sends a UDP message to a particular end point (uni or multicast).
        /// </summary>
        Task SendAsync(byte[] buffer, int bytes, IpEndPointInfo endPoint, CancellationToken cancellationToken);
    }
}