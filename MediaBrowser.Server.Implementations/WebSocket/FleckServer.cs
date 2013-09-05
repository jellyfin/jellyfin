using Fleck;
using MediaBrowser.Common.Net;
using System;
using IWebSocketServer = MediaBrowser.Common.Net.IWebSocketServer;

namespace MediaBrowser.Server.Implementations.WebSocket
{
    public class FleckServer : IWebSocketServer
    {
        private WebSocketServer _server;

        public void Start(int portNumber)
        {
            var server = new WebSocketServer("ws://localhost:" + portNumber);

            server.Start(socket =>
            {
                socket.OnOpen = () => OnClientConnected(socket);
            });

            _server = server;
        }

        public void Stop()
        {
            _server.Dispose();
        }

        private void OnClientConnected(Fleck.IWebSocketConnection context)
        {
            if (WebSocketConnected != null)
            {
                var socket = new FleckWebSocket(context);

                WebSocketConnected(this, new WebSocketConnectEventArgs
                {
                    WebSocket = socket,
                    Endpoint = context.ConnectionInfo.ClientIpAddress + ":" + context.ConnectionInfo.ClientPort
                });
            }
        }
        public event EventHandler<WebSocketConnectEventArgs> WebSocketConnected;

        public int Port
        {
            get { return _server.Port; }
        }

        public void Dispose()
        {
            _server.Dispose();
        }
    }
}
