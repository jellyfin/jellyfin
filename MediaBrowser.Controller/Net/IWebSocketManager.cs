using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Data.Events;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    /// Interface IHttpServer.
    /// </summary>
    public interface IWebSocketManager
    {
        /// <summary>
        /// Occurs when [web socket connected].
        /// </summary>
        event EventHandler<GenericEventArgs<IWebSocketConnection>> WebSocketConnected;

        /// <summary>
        /// Inits this instance.
        /// </summary>
        /// <param name="listeners">The websocket listeners.</param>
        void Init(IEnumerable<IWebSocketListener> listeners);

        /// <summary>
        /// The HTTP request handler.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <returns>The task.</returns>
        Task WebSocketRequestHandler(HttpContext context);
    }
}
