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
        /// <param name="buffer">An array of type <see cref="byte" /> that contains the data to send.</param>
        /// <param name="offset">The zero-based position in buffer at which to begin sending data.</param>
        /// <param name="bytes">The number of bytes to send.</param>
        /// <param name="endPoint">An <see cref="IPEndPoint" /> that represents the remote device.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task SendToAsync(byte[] buffer, int offset, int bytes, IPEndPoint endPoint, CancellationToken cancellationToken);
    }
}
