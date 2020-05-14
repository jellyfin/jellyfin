using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Services;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    /// Interface IHttpServer.
    /// </summary>
    public interface IHttpServer
    {
        /// <summary>
        /// Gets the URL prefix.
        /// </summary>
        /// <value>The URL prefix.</value>
        string[] UrlPrefixes { get; }

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
        /// The HTTP request handler
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        Task RequestHandler(HttpContext context);

        /// <summary>
        /// Get the default CORS headers
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        IDictionary<string, string> GetDefaultCorsHeaders(IRequest req);
    }
}
