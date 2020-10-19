#pragma warning disable CS1591

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Model.Net
{
    /// <summary>
    /// Provides a common interface across platforms for UDP sockets used by this SSDP implementation.
    /// </summary>
    public interface ISocket : IDisposable
    {
        IPAddress LocalIPAddress { get; }

        Task<SocketReceiveResult> ReceiveAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

        IAsyncResult BeginReceive(byte[] buffer, int offset, int count, AsyncCallback callback);

        SocketReceiveResult EndReceive(IAsyncResult result);

        /// <summary>
        /// Sends a UDP message to a particular end point (uni or multicast).
        /// </summary>
        Task SendToAsync(byte[] buffer, int offset, int bytes, IPEndPoint endPoint, CancellationToken cancellationToken);
    }
}
