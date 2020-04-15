using System;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Net;
using MediaBrowser.Model.Services;
using Microsoft.AspNetCore.Http;

namespace Emby.Server.Implementations.HttpServer
{
    /// <summary>
    /// An HTTP listener.
    /// </summary>
    public interface IHttpListener : IDisposable
    {
        /// <summary>
        /// Gets or sets the error handler.
        /// </summary>
        /// <value>The error handler.</value>
        Func<Exception, IRequest, bool, bool, Task> ErrorHandler { get; set; }

        /// <summary>
        /// Gets or sets the request handler.
        /// </summary>
        /// <value>The request handler.</value>
        Func<IHttpRequest, string, string, string, CancellationToken, Task> RequestHandler { get; set; }

        /// <summary>
        /// Gets or sets the web socket handler.
        /// </summary>
        /// <value>The web socket handler.</value>
        Action<WebSocketConnectEventArgs> WebSocketConnected { get; set; }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        /// <returns>The task.</returns>
        Task Stop();

        /// <summary>
        /// Process the web socket request.
        /// </summary>
        /// <param name="ctx">The HTTP context.</param>
        /// <returns>The task.</returns>
        Task ProcessWebSocketRequest(HttpContext ctx);
    }
}
