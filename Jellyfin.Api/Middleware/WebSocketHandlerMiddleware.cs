using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Middleware;

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
    /// <returns>The async task.</returns>
    public async Task Invoke(HttpContext httpContext, IWebSocketManager webSocketManager)
    {
        if (!httpContext.WebSockets.IsWebSocketRequest)
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        await webSocketManager.WebSocketRequestHandler(httpContext).ConfigureAwait(false);
    }
}
