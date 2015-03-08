using System;
using System.Collections.Specialized;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    /// Class WebSocketConnectEventArgs
    /// </summary>
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
        public NameValueCollection QueryString { get; set; }
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

    public class WebSocketConnectingEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public string Url { get; set; }
        /// <summary>
        /// Gets or sets the endpoint.
        /// </summary>
        /// <value>The endpoint.</value>
        public string Endpoint { get; set; }
        /// <summary>
        /// Gets or sets the query string.
        /// </summary>
        /// <value>The query string.</value>
        public NameValueCollection QueryString { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [allow connection].
        /// </summary>
        /// <value><c>true</c> if [allow connection]; otherwise, <c>false</c>.</value>
        public bool AllowConnection { get; set; }

        public WebSocketConnectingEventArgs()
        {
            QueryString = new NameValueCollection();
            AllowConnection = true;
        }
    }

}
