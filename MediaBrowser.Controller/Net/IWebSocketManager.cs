using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    /// Interface IHttpServer.
    /// </summary>
    public interface IWebSocketManager
    {
        /// <summary>
        /// The HTTP request handler.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <returns>The task.</returns>
        Task WebSocketRequestHandler(HttpContext context);
    }
}
