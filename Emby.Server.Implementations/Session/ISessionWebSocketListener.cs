namespace Emby.Server.Implementations.Session
{
    using System.Threading.Tasks;
    using Jellyfin.Data.Events;
    using MediaBrowser.Controller.Net;

    /// <summary>
    /// Defines the <see cref="ISessionWebSocketListener" />.
    /// </summary>
    public interface ISessionWebSocketListener
    {
        /// <summary>
        /// Runs processes due to a WebSocket connection event.
        /// </summary>
        /// <param name="websocketConnection">The <see cref="IWebSocketConnection"/> instance.</param>
        void ProcessWebSocketConnected(IWebSocketConnection websocketConnection);

        /// <summary>
        /// Disposes the object.
        /// </summary>
        void Dispose();

        /// <summary>
        /// Processes a message.
        /// </summary>
        /// <param name="message">The <see cref="WebSocketMessageInfo"/>.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        Task ProcessMessageAsync(WebSocketMessageInfo message);
    }
}
