using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Emby.Server.Implementations.WebSockets
{
    public class WebSocketManager
    {
        private readonly ConcurrentDictionary<Guid, WebSocket> _activeWebSockets;

        public WebSocketManager()
        {
            _activeWebSockets = new ConcurrentDictionary<Guid, WebSocket>();
        }

        public void AddSocket(WebSocket webSocket)
        {
            var guid = Guid.NewGuid();
            _activeWebSockets.TryAdd(guid, webSocket);
        }
    }
}
