using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.Api.WebSockets
{
    /// <summary>
    /// Legacy web socket handler.
    /// </summary>
    public class LegacyWebSocketHandler : BaseWebSocketHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LegacyWebSocketHandler"/> class.
        /// </summary>
        /// <param name="webSocketConnectionManager">Instance of the <see cref="WebSocketConnectionManager"/> singleton.</param>
        public LegacyWebSocketHandler(WebSocketConnectionManager webSocketConnectionManager)
            : base(webSocketConnectionManager)
        {
        }

        /// <inheritdoc />
        public override async Task OnConnected(WebSocket socket)
        {
            await base.OnConnected(socket).ConfigureAwait(false);

            var socketId = WebSocketConnectionManager.GetId(socket);
            await SendMessageToAllAsync($"{socketId} is now connected").ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override async Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            var socketId = WebSocketConnectionManager.GetId(socket);
            var message = $"{socketId} said: {Encoding.UTF8.GetString(buffer, 0, result.Count)}";

            await SendMessageToAllAsync(message).ConfigureAwait(false);
        }
    }
}
