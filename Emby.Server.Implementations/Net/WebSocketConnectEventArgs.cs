using System;
using Microsoft.AspNetCore.Http;

namespace Emby.Server.Implementations.Net
{
    public class WebSocketConnectEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public string Url { get; set; }
        /// <summary>
        /// Gets or sets the query string.
        /// </summary>
        /// <value>The query string.</value>
        public IQueryCollection QueryString { get; set; }
        /// <summary>
        /// Gets or sets the web socket.
        /// </summary>
        /// <value>The web socket.</value>
        public IWebSocket WebSocket { get; set; }
        /// <summary>
        /// Gets or sets the endpoint.
        /// </summary>
        /// <value>The endpoint.</value>
        public string Endpoint { get; set; }
    }
}
