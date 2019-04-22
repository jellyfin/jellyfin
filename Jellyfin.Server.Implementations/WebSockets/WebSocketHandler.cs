using System.Threading.Tasks;
using Jellyfin.Model.Net;

namespace Jellyfin.Server.Implementations.WebSockets
{
    public interface IWebSocketHandler
    {
        Task ProcessMessage(WebSocketMessage<object> message, TaskCompletionSource<bool> taskCompletionSource);
    }
}
