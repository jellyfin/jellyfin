using System.Threading.Tasks;
using MediaBrowser.Model.Net;

namespace Emby.Server.Implementations.WebSockets
{
    public interface IWebSocketHandler
    {
        Task ProcessMessage(WebSocketMessage<object> message, TaskCompletionSource<bool> taskCompletionSource);
    }
}
