using System;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Interface IWebSocketServer
    /// </summary>
    public interface IWebSocketServer : IDisposable
    {
        /// <summary>
        /// Starts the specified port number.
        /// </summary>
        /// <param name="portNumber">The port number.</param>
        void Start(int portNumber);

        /// <summary>
        /// Stops this instance.
        /// </summary>
        void Stop();

        /// <summary>
        /// Occurs when [web socket connected].
        /// </summary>
        event EventHandler<WebSocketConnectEventArgs> WebSocketConnected;

        /// <summary>
        /// Gets the port.
        /// </summary>
        /// <value>The port.</value>
        int Port { get; }
    }
}
