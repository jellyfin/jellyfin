#pragma warning disable CS1591

using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Controller.Net
{
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
        /// Gets or sets the date of last Keeplive received.
        /// </summary>
        /// <value>The date of last Keeplive received.</value>
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
        /// Gets the remote end point.
        /// </summary>
        /// <value>The remote end point.</value>
        IPAddress? RemoteEndPoint { get; }

        /// <summary>
        /// Sends a message asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of websocket message data.</typeparam>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">The message is null.</exception>
        Task SendAsync<T>(WebSocketMessage<T> message, CancellationToken cancellationToken);

        Task ProcessAsync(CancellationToken cancellationToken = default);
    }
}
