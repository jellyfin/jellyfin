using MediaBrowser.Common.Net;
using MediaBrowser.Model.Net;
using System;
using System.Threading;
using System.Threading.Tasks;
using IWebSocketConnection = Fleck.IWebSocketConnection;

namespace MediaBrowser.Server.Implementations.WebSocket
{
    public class FleckWebSocket : IWebSocket
    {
        private readonly IWebSocketConnection _connection;

        public FleckWebSocket(IWebSocketConnection connection)
        {
            _connection = connection;

            _connection.OnMessage = OnReceiveData;
        }

        public WebSocketState State
        {
            get { return _connection.IsAvailable ? WebSocketState.Open : WebSocketState.Closed; }
        }

        private void OnReceiveData(string data)
        {
            if (OnReceive != null)
            {
                OnReceive(data);
            }
        }

        public Task SendAsync(byte[] bytes, WebSocketMessageType type, bool endOfMessage, CancellationToken cancellationToken)
        {
            return Task.Run(() => _connection.Send(bytes));
        }

        public void Dispose()
        {
            _connection.Close();
        }

        public Action<byte[]> OnReceiveBytes { get; set; }
        public Action<string> OnReceive { get; set; }
    }
}
