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
        /// <param name="certificatePath">If an https prefix is specified, 
        /// the ssl certificate localtion on the file system.</param>
        void StartServer(IEnumerable<string> urlPrefixes, string certificatePath);

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
        /// Occurs when [web socket connected].
        /// </summary>
        event EventHandler<WebSocketConnectEventArgs> WebSocketConnected;

        /// <summary>
        /// Inits this instance.
        /// </summary>
        void Init(IEnumerable<IRestfulService> services);
    }
}