#nullable disable

using MediaBrowser.Controller.Net.WebSocketMessages;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    /// Class WebSocketMessageInfo.
    /// </summary>
    public class WebSocketMessageInfo : InboundWebSocketMessage<string>
    {
        /// <summary>
        /// Gets or sets the connection.
        /// </summary>
        /// <value>The connection.</value>
        public IWebSocketConnection Connection { get; set; }
    }
}
