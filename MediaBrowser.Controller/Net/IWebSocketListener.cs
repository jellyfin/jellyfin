using System.Threading.Tasks;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    ///This is an interface for listening to messages coming through a web socket connection
    /// </summary>
    public interface IWebSocketListener
    {
        /// <summary>
        /// Processes the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>Task.</returns>
        Task ProcessMessage(WebSocketMessageInfo message);
    }
}
