using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;

namespace Emby.Server.Implementations.Session
{
    public class WebSocketController : ISessionController, IDisposable
    {
        public SessionInfo Session { get; private set; }
        public IReadOnlyList<IWebSocketConnection> Sockets { get; private set; }

        private readonly ILogger _logger;

        private readonly ISessionManager _sessionManager;

        public WebSocketController(SessionInfo session, ILogger logger, ISessionManager sessionManager)
        {
            Session = session;
            _logger = logger;
            _sessionManager = sessionManager;
            Sockets = new List<IWebSocketConnection>();
        }

        private bool HasOpenSockets
        {
            get { return GetActiveSockets().Any(); }
        }

        public bool SupportsMediaControl
        {
            get { return HasOpenSockets; }
        }

        public bool IsSessionActive
        {
            get
            {
                return HasOpenSockets;
            }
        }

        private IEnumerable<IWebSocketConnection> GetActiveSockets()
        {
            return Sockets
                .OrderByDescending(i => i.LastActivityDate)
                .Where(i => i.State == WebSocketState.Open);
        }

        public void AddWebSocket(IWebSocketConnection connection)
        {
            var sockets = Sockets.ToList();
            sockets.Add(connection);

            Sockets = sockets;

            connection.Closed += connection_Closed;
        }

        void connection_Closed(object sender, EventArgs e)
        {
            var connection = (IWebSocketConnection)sender;
            var sockets = Sockets.ToList();
            sockets.Remove(connection);

            Sockets = sockets;

            _sessionManager.CloseIfNeeded(Session);
        }

        public Task SendMessage<T>(string name, string messageId, T data, ISessionController[] allControllers, CancellationToken cancellationToken)
        {
            var socket = GetActiveSockets()
                .FirstOrDefault();

            if (socket == null)
            {
                return Task.CompletedTask;
            }

            return socket.SendAsync(new WebSocketMessage<T>
            {
                Data = data,
                MessageType = name,
                MessageId = messageId

            }, cancellationToken);
        }

        public void Dispose()
        {
            foreach (var socket in Sockets.ToList())
            {
                socket.Closed -= connection_Closed;
            }
        }
    }
}
