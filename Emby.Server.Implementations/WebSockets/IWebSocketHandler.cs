#pragma warning disable CS1591

using System.Threading.Tasks;
using MediaBrowser.Model.Net;

namespace Emby.Server.Implementations.WebSockets
{
    /// <summary>
    /// This interface defines a Websocket Handler.
    /// </summary>
    public interface IWebSocketHandler
    {
        Task ProcessMessage(WebSocketMessage<object> message, TaskCompletionSource<bool> taskCompletionSource);
    }
}
