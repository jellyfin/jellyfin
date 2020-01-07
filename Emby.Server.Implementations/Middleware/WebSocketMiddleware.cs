using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WebSocketManager = Emby.Server.Implementations.WebSockets.WebSocketManager;

namespace Emby.Server.Implementations.Middleware
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WebSocketMiddleware> _logger;
        private readonly WebSocketManager _webSocketManager;

        public WebSocketMiddleware(RequestDelegate next, ILogger<WebSocketMiddleware> logger, WebSocketManager webSocketManager)
        {
            _next = next;
            _logger = logger;
            _webSocketManager = webSocketManager;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            _logger.LogInformation("Handling request: " + httpContext.Request.Path);

            if (httpContext.WebSockets.IsWebSocketRequest)
            {
                var webSocketContext = await httpContext.WebSockets.AcceptWebSocketAsync(null).ConfigureAwait(false);
                if (webSocketContext != null)
                {
                    await _webSocketManager.OnWebSocketConnected(webSocketContext).ConfigureAwait(false);
                }
            }
            else
            {
                await _next.Invoke(httpContext).ConfigureAwait(false);
            }
        }
    }
}
