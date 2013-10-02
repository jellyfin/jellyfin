using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Session;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Session
{
    public class WebSocketController : ISessionController
    {
        public bool Supports(SessionInfo session)
        {
            return session.WebSockets.Any(i => i.State == WebSocketState.Open);
        }

        private IWebSocketConnection GetSocket(SessionInfo session)
        {
            var socket = session.WebSockets.OrderByDescending(i => i.LastActivityDate).FirstOrDefault(i => i.State == WebSocketState.Open);


            if (socket == null)
            {
                throw new InvalidOperationException("The requested session does not have an open web socket.");
            }

            return socket;
        }

        public Task SendSystemCommand(SessionInfo session, SystemCommand command, CancellationToken cancellationToken)
        {
            var socket = GetSocket(session);

            return socket.SendAsync(new WebSocketMessage<string>
            {
                MessageType = "SystemCommand",
                Data = command.ToString()

            }, cancellationToken);
        }

        public Task SendMessageCommand(SessionInfo session, MessageCommand command, CancellationToken cancellationToken)
        {
            var socket = GetSocket(session);

            return socket.SendAsync(new WebSocketMessage<MessageCommand>
            {
                MessageType = "MessageCommand",
                Data = command

            }, cancellationToken);
        }

        public Task SendPlayCommand(SessionInfo session, PlayRequest command, CancellationToken cancellationToken)
        {
            var socket = GetSocket(session);

            return socket.SendAsync(new WebSocketMessage<PlayRequest>
            {
                MessageType = "Play",
                Data = command

            }, cancellationToken);
        }

        public Task SendBrowseCommand(SessionInfo session, BrowseRequest command, CancellationToken cancellationToken)
        {
            var socket = GetSocket(session);

            return socket.SendAsync(new WebSocketMessage<BrowseRequest>
            {
                MessageType = "Browse",
                Data = command

            }, cancellationToken);
        }

        public Task SendPlaystateCommand(SessionInfo session, PlaystateRequest command, CancellationToken cancellationToken)
        {
            var socket = GetSocket(session);

            return socket.SendAsync(new WebSocketMessage<PlaystateRequest>
            {
                MessageType = "Playstate",
                Data = command

            }, cancellationToken);
        }
    }
}
