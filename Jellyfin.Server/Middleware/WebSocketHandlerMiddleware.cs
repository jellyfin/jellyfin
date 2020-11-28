using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Server.Middleware
{
    /// <summary>
    /// Handles WebSocket requests.
    /// </summary>
    public class WebSocketHandlerMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketHandlerMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next delegate in the pipeline.</param>
        public WebSocketHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Executes the middleware action.
        /// </summary>
        /// <param name="httpContext">The current HTTP context.</param>
        /// <param name="webSocketManager">The WebSocket connection manager.</param>
        /// <param name="websocketListener">Session manager instance.</param>
        /// <returns>The async task.</returns>
        public async Task Invoke(
            HttpContext httpContext,
            IWebSocketManager webSocketManager,
#pragma warning disable CA1801
#pragma warning disable IDE0060
            // TODO: Workaround. see https://github.com/jellyfin/jellyfin/pull/3194
            // Do not remove this parameter. It uses DI to create a SessionWebSocketListener which is
            // required for webSocketManager events.
            IWebSocketListener websocketListener)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore CA1801
        {
            if (!httpContext.WebSockets.IsWebSocketRequest)
            {
                await _next(httpContext).ConfigureAwait(false);
                return;
            }

            await webSocketManager.WebSocketRequestHandler(httpContext).ConfigureAwait(false);
        }
    }
}
