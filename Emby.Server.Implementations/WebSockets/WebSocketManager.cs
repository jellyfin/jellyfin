using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using UtfUnknown;

namespace Emby.Server.Implementations.WebSockets
{
    public class WebSocketManager
    {
        private readonly IWebSocketHandler[] _webSocketHandlers;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger<WebSocketManager> _logger;
        private const int BufferSize = 4096;

        public WebSocketManager(IWebSocketHandler[] webSocketHandlers, IJsonSerializer jsonSerializer, ILogger<WebSocketManager> logger)
        {
            _webSocketHandlers = webSocketHandlers;
            _jsonSerializer = jsonSerializer;
            _logger = logger;
        }

        public async Task OnWebSocketConnected(WebSocket webSocket)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            var cancellationToken = new CancellationTokenSource().Token;
            WebSocketReceiveResult result;
            var message = new List<byte>();

            // Keep listening for incoming messages, otherwise the socket closes automatically
            do
            {
                var buffer = WebSocket.CreateServerBuffer(BufferSize);
                result = await webSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                message.AddRange(buffer.Array.Take(result.Count));

                if (result.EndOfMessage)
                {
                    await ProcessMessage(message.ToArray(), taskCompletionSource).ConfigureAwait(false);
                    message.Clear();
                }
            } while (!taskCompletionSource.Task.IsCompleted &&
                     webSocket.State == WebSocketState.Open &&
                     result.MessageType != WebSocketMessageType.Close);

            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(
                    result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                    result.CloseStatusDescription,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ProcessMessage(byte[] messageBytes, TaskCompletionSource<bool> taskCompletionSource)
        {
            var charset = CharsetDetector.DetectFromBytes(messageBytes).Detected?.EncodingName;
            var message = string.Equals(charset, "utf-8", StringComparison.OrdinalIgnoreCase)
                ? Encoding.UTF8.GetString(messageBytes, 0, messageBytes.Length)
                : Encoding.ASCII.GetString(messageBytes, 0, messageBytes.Length);

            // All messages are expected to be valid JSON objects
            if (!message.StartsWith("{", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Received web socket message that is not a json structure: {Message}", message);
                return;
            }

            try
            {
                var info = _jsonSerializer.DeserializeFromString<WebSocketMessage<object>>(message);

                _logger.LogDebug("Websocket message received: {0}", info.MessageType);

                var tasks = _webSocketHandlers.Select(handler => Task.Run(() =>
                {
                    try
                    {
                        handler.ProcessMessage(info, taskCompletionSource).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "{HandlerType} failed processing WebSocket message {MessageType}",
                            handler.GetType().Name, info.MessageType ?? string.Empty);
                    }
                }));

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing web socket message");
            }
        }
    }
}
