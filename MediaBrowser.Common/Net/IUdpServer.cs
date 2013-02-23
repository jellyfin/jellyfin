using System;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Interface IUdpServer
    /// </summary>
    public interface IUdpServer : IDisposable
    {
        /// <summary>
        /// Occurs when [message received].
        /// </summary>
        event EventHandler<UdpMessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Starts the specified port.
        /// </summary>
        /// <param name="port">The port.</param>
        void Start(int port);

        /// <summary>
        /// Stops this instance.
        /// </summary>
        void Stop();

        /// <summary>
        /// Sends the async.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">data</exception>
        Task SendAsync(byte[] bytes, string remoteEndPoint);

        /// <summary>
        /// Sends the async.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="port">The port.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">bytes</exception>
        Task SendAsync(byte[] bytes, string ipAddress, int port);
    }
}
