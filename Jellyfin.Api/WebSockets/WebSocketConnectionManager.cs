using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Api.WebSockets
{
    /// <summary>
    /// Web socket connection manager.
    /// </summary>
    public class WebSocketConnectionManager
    {
        private readonly ConcurrentDictionary<Guid, WebSocket> _sockets = new ConcurrentDictionary<Guid, WebSocket>();

        /// <summary>
        /// Get socket by id.
        /// </summary>
        /// <param name="id">Socket id.</param>
        /// <returns>Websocket if exist.</returns>
        public WebSocket? GetSocketById(Guid id)
        {
            var (key, value) = _sockets.FirstOrDefault(p => p.Key == id);
            return key == Guid.Empty ? null : value;
        }

        /// <summary>
        /// Get all sockets.
        /// </summary>
        /// <returns>List of sockets.</returns>
        public IEnumerable<WebSocket> GetAll()
        {
            return _sockets.Values;
        }

        /// <summary>
        /// Get socket id.
        /// </summary>
        /// <param name="socket">Socket to get id for.</param>
        /// <returns>Socket id if exist.</returns>
        public Guid? GetId(WebSocket socket)
        {
            var socketId = _sockets.FirstOrDefault(p => p.Value == socket).Key;
            if (socketId == Guid.Empty)
            {
                return null;
            }

            return socketId;
        }

        /// <summary>
        /// Add socket to manager.
        /// </summary>
        /// <param name="socket">Socket to add.</param>
        public void AddSocket(WebSocket socket)
        {
            _sockets.TryAdd(CreateConnectionId(), socket);
        }

        /// <summary>
        /// Remove existing socket.
        /// </summary>
        /// <param name="id">Socket id.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        public async Task RemoveSocket(Guid id)
        {
            WebSocket? socket = null;
            try
            {
                _sockets.TryRemove(id, out socket);
                if (socket is null)
                {
                    return;
                }

                await socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closed by the ConnectionManager",
                        CancellationToken.None)
                    .ConfigureAwait(false);
            }
            finally
            {
                socket?.Dispose();
            }
        }

        private static Guid CreateConnectionId()
        {
            return Guid.NewGuid();
        }
    }
}
