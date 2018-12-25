using MediaBrowser.Model.Net;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Controller.Net
{
    public interface IWebSocketConnection : IDisposable
    {
        /// <summary>
        /// Occurs when [closed].
        /// </summary>
        event EventHandler<EventArgs> Closed;

        /// <summary>
        /// Gets the id.
        /// </summary>
        /// <value>The id.</value>
        Guid Id { get; }

        /// <summary>
        /// Gets the last activity date.
        /// </summary>
        /// <value>The last activity date.</value>
        DateTime LastActivityDate { get; }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        string Url { get; set; }
        /// <summary>
        /// Gets or sets the query string.
        /// </summary>
        /// <value>The query string.</value>
        QueryParamCollection QueryString { get; set; }

        /// <summary>
        /// Gets or sets the receive action.
        /// </summary>
        /// <value>The receive action.</value>
        Action<WebSocketMessageInfo> OnReceive { get; set; }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <value>The state.</value>
        WebSocketState State { get; }

        /// <summary>
        /// Gets the remote end point.
        /// </summary>
        /// <value>The remote end point.</value>
        string RemoteEndPoint { get; }

        /// <summary>
        /// Sends a message asynchronously.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">message</exception>
        Task SendAsync<T>(WebSocketMessage<T> message, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a message asynchronously.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendAsync(byte[] buffer, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a message asynchronously.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">buffer</exception>
        Task SendAsync(string text, CancellationToken cancellationToken);
    }
}