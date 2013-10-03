using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Session
{
    public class WebSocketController : ISessionController
    {
        public SessionInfo Session { get; private set; }
        public List<IWebSocketConnection> Sockets { get; private set; }

        private readonly IServerApplicationHost _appHost;

        public WebSocketController(SessionInfo session, IServerApplicationHost appHost)
        {
            Session = session;
            _appHost = appHost;
            Sockets = new List<IWebSocketConnection>();
        }

        public bool SupportsMediaRemoteControl
        {
            get
            {
                return Sockets.Any(i => i.State == WebSocketState.Open);
            }
        }

        public bool IsSessionActive
        {
            get
            {
                return Sockets.Any(i => i.State == WebSocketState.Open);
            }
        }

        private IWebSocketConnection GetActiveSocket()
        {
            var socket = Sockets
                .OrderByDescending(i => i.LastActivityDate)
                .FirstOrDefault(i => i.State == WebSocketState.Open);

            if (socket == null)
            {
                throw new InvalidOperationException("The requested session does not have an open web socket.");
            }

            return socket;
        }

        public Task SendSystemCommand(SystemCommand command, CancellationToken cancellationToken)
        {
            var socket = GetActiveSocket();

            return socket.SendAsync(new WebSocketMessage<string>
            {
                MessageType = "SystemCommand",
                Data = command.ToString()

            }, cancellationToken);
        }

        public Task SendMessageCommand(MessageCommand command, CancellationToken cancellationToken)
        {
            var socket = GetActiveSocket();

            return socket.SendAsync(new WebSocketMessage<MessageCommand>
            {
                MessageType = "MessageCommand",
                Data = command

            }, cancellationToken);
        }

        public Task SendPlayCommand(PlayRequest command, CancellationToken cancellationToken)
        {
            var socket = GetActiveSocket();

            return socket.SendAsync(new WebSocketMessage<PlayRequest>
            {
                MessageType = "Play",
                Data = command

            }, cancellationToken);
        }

        public Task SendBrowseCommand(BrowseRequest command, CancellationToken cancellationToken)
        {
            var socket = GetActiveSocket();

            return socket.SendAsync(new WebSocketMessage<BrowseRequest>
            {
                MessageType = "Browse",
                Data = command

            }, cancellationToken);
        }

        public Task SendPlaystateCommand(PlaystateRequest command, CancellationToken cancellationToken)
        {
            var socket = GetActiveSocket();

            return socket.SendAsync(new WebSocketMessage<PlaystateRequest>
            {
                MessageType = "Playstate",
                Data = command

            }, cancellationToken);
        }

        public Task SendLibraryUpdateInfo(LibraryUpdateInfo info, CancellationToken cancellationToken)
        {
            var socket = GetActiveSocket();
            
            return socket.SendAsync(new WebSocketMessage<LibraryUpdateInfo>
            {
                MessageType = "Playstate",
                Data = info

            }, cancellationToken);
        }

        /// <summary>
        /// Sends the restart required message.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendRestartRequiredMessage(CancellationToken cancellationToken)
        {
            var socket = GetActiveSocket();

            return socket.SendAsync(new WebSocketMessage<SystemInfo>
            {
                MessageType = "RestartRequired",
                Data = _appHost.GetSystemInfo()

            }, cancellationToken);
        }
    }
}
