using MediaBrowser.Model.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    /// Interface IServerManager
    /// </summary>
    public interface IServerManager : IDisposable
    {
        /// <summary>
        /// Starts this instance.
        /// </summary>
        /// <param name="urlPrefixes">The URL prefixes.</param>
        void Start(IEnumerable<string> urlPrefixes);

        /// <summary>
        /// Sends a message to all clients currently connected via a web socket
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="data">The data.</param>
        /// <returns>Task.</returns>
        void SendWebSocketMessage<T>(string messageType, T data);

        /// <summary>
        /// Sends a message to all clients currently connected via a web socket
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="dataFunction">The function that generates the data to send, if there are any connected clients</param>
        void SendWebSocketMessage<T>(string messageType, Func<T> dataFunction);

        /// <summary>
        /// Sends a message to all clients currently connected via a web socket
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="dataFunction">The function that generates the data to send, if there are any connected clients</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">messageType</exception>
        Task SendWebSocketMessageAsync<T>(string messageType, Func<T> dataFunction, CancellationToken cancellationToken);

        /// <summary>
        /// Adds the web socket listeners.
        /// </summary>
        /// <param name="listeners">The listeners.</param>
        void AddWebSocketListeners(IEnumerable<IWebSocketListener> listeners);

        /// <summary>
        /// Gets the web socket connections.
        /// </summary>
        /// <value>The web socket connections.</value>
        IEnumerable<IWebSocketConnection> WebSocketConnections { get; }

        /// <summary>
        /// Occurs when [web socket connected].
        /// </summary>
        event EventHandler<GenericEventArgs<IWebSocketConnection>> WebSocketConnected;
    }
}