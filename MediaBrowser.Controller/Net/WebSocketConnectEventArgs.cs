using System;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    /// Class WebSocketConnectEventArgs
    /// </summary>
    public class WebSocketConnectEventArgs : EventArgs
    {
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
