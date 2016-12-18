using System;
using System.Collections.Generic;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    /// Interface IHttpServer
    /// </summary>
    public interface IHttpServer : IDisposable
    {
        /// <summary>
        /// Gets the URL prefix.
        /// </summary>
        /// <value>The URL prefix.</value>
        IEnumerable<string> UrlPrefixes { get; }

        /// <summary>
        /// Starts the specified server name.
        /// </summary>
        /// <param name="urlPrefixes">The URL prefixes.</param>
        void StartServer(IEnumerable<string> urlPrefixes);

        /// <summary>
        /// Stops this instance.
        /// </summary>
        void Stop();

        /// <summary>
        /// Occurs when [web socket connected].
        /// </summary>
        event EventHandler<WebSocketConnectEventArgs> WebSocketConnected;

        /// <summary>
        /// Occurs when [web socket connecting].
        /// </summary>
        event EventHandler<WebSocketConnectingEventArgs> WebSocketConnecting;

        /// <summary>
        /// Inits this instance.
        /// </summary>
        void Init(IEnumerable<IService> services);

        /// <summary>
        /// If set, all requests will respond with this message
        /// </summary>
        string GlobalResponse { get; set; }
    }
}