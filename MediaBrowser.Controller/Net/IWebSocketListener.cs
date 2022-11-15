using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    /// Interface for listening to messages coming through a web socket connection.
    /// </summary>
    public interface IWebSocketListener
    {
        /// <summary>
        /// Processes the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>Task.</returns>
        Task ProcessMessageAsync(WebSocketMessageInfo message);

        /// <summary>
        /// Processes a new web socket connection.
        /// </summary>
        /// <param name="connection">An instance of the <see cref="IWebSocketConnection"/> interface.</param>
        /// <param name="httpContext">The current http context.</param>
        /// <returns>Task.</returns>
        Task ProcessWebSocketConnectedAsync(IWebSocketConnection connection, HttpContext httpContext);
    }
}
