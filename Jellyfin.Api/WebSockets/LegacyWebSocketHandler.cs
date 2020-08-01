using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.WebSockets
{
    /// <summary>
    /// Legacy web socket handler.
    /// </summary>
    public class LegacyWebSocketHandler : BaseWebSocketHandler
    {
        private readonly ILogger<LegacyWebSocketHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LegacyWebSocketHandler"/> class.
        /// </summary>
        /// <param name="webSocketConnectionManager">Instance of the <see cref="WebSocketConnectionManager"/> singleton.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{LegacyWebSocketHandler}"/> interface.</param>
        public LegacyWebSocketHandler(WebSocketConnectionManager webSocketConnectionManager, ILogger<LegacyWebSocketHandler> logger)
            : base(webSocketConnectionManager)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public override async Task OnConnected(WebSocket socket)
        {
            await base.OnConnected(socket).ConfigureAwait(false);

            var socketId = WebSocketConnectionManager.GetId(socket);
            _logger.LogInformation("{socketId} is now connected", socketId);
            await SendMessageToAllAsync($"{socketId} is now connected").ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override async Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            var socketId = WebSocketConnectionManager.GetId(socket);
            var messageStr = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var message = $"{socketId} said: {messageStr}";
            _logger.LogInformation("{socketId} said {message}", messageStr);

            await SendMessageToAllAsync(message).ConfigureAwait(false);
        }
    }
}
