#pragma warning disable CS1591
#pragma warning disable SA1600

using System;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Net;
using MediaBrowser.Model.Services;
using Microsoft.AspNetCore.Http;

namespace Emby.Server.Implementations.HttpServer
{
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
        Task Stop();

        Task ProcessWebSocketRequest(HttpContext ctx);
    }
}
