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

        public Task SendPlayCommand(PlayRequest command, CancellationToken cancellationToken)
        {
            var socket = GetActiveSocket();

            return socket.SendAsync(new WebSocketMessage<PlayRequest>
            {
                MessageType = "Play",
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
                MessageType = "LibraryChanged",
                Data = info

            }, cancellationToken);
        }

        /// <summary>
        /// Sends the restart required message.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendRestartRequiredNotification(CancellationToken cancellationToken)
        {
            var socket = GetActiveSocket();

            return socket.SendAsync(new WebSocketMessage<SystemInfo>
            {
                MessageType = "RestartRequired",
                Data = _appHost.GetSystemInfo()

            }, cancellationToken);
        }


        /// <summary>
        /// Sends the user data change info.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendUserDataChangeInfo(UserDataChangeInfo info, CancellationToken cancellationToken)
        {
            var socket = GetActiveSocket();

            return socket.SendAsync(new WebSocketMessage<UserDataChangeInfo>
            {
                MessageType = "UserDataChanged",
                Data = info

            }, cancellationToken);
        }

        /// <summary>
        /// Sends the server shutdown notification.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendServerShutdownNotification(CancellationToken cancellationToken)
        {
            var socket = GetActiveSocket();

            return socket.SendAsync(new WebSocketMessage<string>
            {
                MessageType = "ServerShuttingDown",
                Data = string.Empty

            }, cancellationToken);
        }

        /// <summary>
        /// Sends the server restart notification.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendServerRestartNotification(CancellationToken cancellationToken)
        {
            var socket = GetActiveSocket();

            return socket.SendAsync(new WebSocketMessage<string>
            {
                MessageType = "ServerRestarting",
                Data = string.Empty

            }, cancellationToken);
        }

        public Task SendGeneralCommand(GeneralCommand command, CancellationToken cancellationToken)
        {
            var socket = GetActiveSocket();

            return socket.SendAsync(new WebSocketMessage<GeneralCommand>
            {
                MessageType = "GeneralCommand",
                Data = command

            }, cancellationToken);
        }

        public Task SendSessionEndedNotification(SessionInfoDto sessionInfo, CancellationToken cancellationToken)
        {
            var socket = GetActiveSocket();

            return socket.SendAsync(new WebSocketMessage<SessionInfoDto>
            {
                MessageType = "SessionEnded",
                Data = sessionInfo

            }, cancellationToken);
        }

        public Task SendPlaybackStartNotification(SessionInfoDto sessionInfo, CancellationToken cancellationToken)
        {
            var socket = GetActiveSocket();

            return socket.SendAsync(new WebSocketMessage<SessionInfoDto>
            {
                MessageType = "PlaybackStart",
                Data = sessionInfo

            }, cancellationToken);
        }

        public Task SendPlaybackStoppedNotification(SessionInfoDto sessionInfo, CancellationToken cancellationToken)
        {
            var socket = GetActiveSocket();

            return socket.SendAsync(new WebSocketMessage<SessionInfoDto>
            {
                MessageType = "PlaybackStopped",
                Data = sessionInfo

            }, cancellationToken);
        }
    }
}
