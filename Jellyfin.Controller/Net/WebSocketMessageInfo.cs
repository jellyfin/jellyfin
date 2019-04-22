using Jellyfin.Model.Net;

namespace Jellyfin.Controller.Net
{
    /// <summary>
    /// Class WebSocketMessageInfo
    /// </summary>
    public class WebSocketMessageInfo : WebSocketMessage<string>
    {
        /// <summary>
        /// Gets or sets the connection.
        /// </summary>
        /// <value>The connection.</value>
        public IWebSocketConnection Connection { get; set; }
    }
}
