using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net.WebSocketMessages;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    /// Interface for WebSocket connections.
    /// </summary>
    public interface IWebSocketConnection : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Occurs when [closed].
        /// </summary>
        event EventHandler<EventArgs>? Closed;

        /// <summary>
        /// Gets the last activity date.
        /// </summary>
        /// <value>The last activity date.</value>
        DateTime LastActivityDate { get; }

        /// <summary>
        /// Gets or sets the date of last Keepalive received.
        /// </summary>
        /// <value>The date of last Keepalive received.</value>
        DateTime LastKeepAliveDate { get; set; }

        /// <summary>
        /// Gets or sets the receive action.
        /// </summary>
        /// <value>The receive action.</value>
        Func<WebSocketMessageInfo, Task>? OnReceive { get; set; }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <value>The state.</value>
        WebSocketState State { get; }

        /// <summary>
        /// Gets the authorization information.
        /// </summary>
        public AuthorizationInfo AuthorizationInfo { get; }

        /// <summary>
        /// Gets the remote end point.
        /// </summary>
        /// <value>The remote end point.</value>
        IPAddress? RemoteEndPoint { get; }

        /// <summary>
        /// Sends a message asynchronously.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">The message is null.</exception>
        Task SendAsync(OutboundWebSocketMessage message, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a message asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of websocket message data.</typeparam>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">The message is null.</exception>
        Task SendAsync<T>(OutboundWebSocketMessage<T> message, CancellationToken cancellationToken);

        /// <summary>
        /// Receives a message asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task ReceiveAsync(CancellationToken cancellationToken = default);
    }
}
