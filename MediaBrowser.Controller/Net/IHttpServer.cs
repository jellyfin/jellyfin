using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Services;
using Microsoft.AspNetCore.Http;

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
        string[] UrlPrefixes { get; }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        void Stop();

        /// <summary>
        /// Occurs when [web socket connected].
        /// </summary>
        event EventHandler<GenericEventArgs<IWebSocketConnection>> WebSocketConnected;

        /// <summary>
        /// Inits this instance.
        /// </summary>
        void Init(IEnumerable<Type> serviceTypes, IEnumerable<IWebSocketListener> listener, IEnumerable<string> urlPrefixes);

        /// <summary>
        /// If set, all requests will respond with this message
        /// </summary>
        string GlobalResponse { get; set; }

        /// <summary>
        /// Sends the http context to the socket listener
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        Task ProcessWebSocketRequest(HttpContext ctx);

        /// <summary>
        /// The HTTP request handler
        /// </summary>
        /// <param name="httpReq"></param>
        /// <param name="urlString"></param>
        /// <param name="host"></param>
        /// <param name="localPath"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RequestHandler(IHttpRequest httpReq, string urlString, string host, string localPath,
            CancellationToken cancellationToken);
    }
}
