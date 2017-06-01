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
    public interface ISocket : IDisposable
    {
        IpAddressInfo LocalIPAddress { get; }

        Task<SocketReceiveResult> ReceiveAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

        int Receive(byte[] buffer, int offset, int count);

        IAsyncResult BeginReceive(byte[] buffer, int offset, int count, AsyncCallback callback);
        SocketReceiveResult EndReceive(IAsyncResult result);

        /// <summary>
        /// Sends a UDP message to a particular end point (uni or multicast).
        /// </summary>
        Task SendToAsync(byte[] buffer, int offset, int bytes, IpEndPointInfo endPoint, CancellationToken cancellationToken);

        IAsyncResult BeginSendTo(byte[] buffer, int offset, int size, IpEndPointInfo endPoint, AsyncCallback callback, object state);
        int EndSendTo(IAsyncResult result);
    }
}