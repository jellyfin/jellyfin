using MediaBrowser.Common.Net;
using System;
using System.Collections.Generic;

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
        /// Gets a value indicating whether [supports web sockets].
        /// </summary>
        /// <value><c>true</c> if [supports web sockets]; otherwise, <c>false</c>.</value>
        bool SupportsWebSockets { get; }

        /// <summary>
        /// Gets the local end points.
        /// </summary>
        /// <value>The local end points.</value>
        IEnumerable<string> LocalEndPoints { get; }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        void Stop();

        /// <summary>
        /// Gets or sets a value indicating whether [enable HTTP request logging].
        /// </summary>
        /// <value><c>true</c> if [enable HTTP request logging]; otherwise, <c>false</c>.</value>
        bool EnableHttpRequestLogging { get; set; }

        /// <summary>
        /// Occurs when [web socket connected].
        /// </summary>
        event EventHandler<WebSocketConnectEventArgs> WebSocketConnected;

        /// <summary>
        /// Inits this instance.
        /// </summary>
        void Init(IEnumerable<IRestfulService> services);
    }
}