using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
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
        private readonly ILogger _logger;

        public WebSocketController(SessionInfo session, IServerApplicationHost appHost, ILogger logger)
        {
            Session = session;
            _appHost = appHost;
            _logger = logger;
            Sockets = new List<IWebSocketConnection>();
        }

        public bool IsSessionActive
        {
            get
            {
                return Sockets.Any(i => i.State == WebSocketState.Open);
            }
        }

        private IEnumerable<IWebSocketConnection> GetActiveSockets()
        {
            return Sockets
                .OrderByDescending(i => i.LastActivityDate)
                .Where(i => i.State == WebSocketState.Open);
        }

        private IWebSocketConnection GetActiveSocket()
        {
            var socket = GetActiveSockets()
                .FirstOrDefault();

            if (socket == null)
            {
                throw new InvalidOperationException("The requested session does not have an open web socket.");
            }

            return socket;
        }

        public Task SendPlayCommand(PlayRequest command, CancellationToken cancellationToken)
        {
            return SendMessage(new WebSocketMessage<PlayRequest>
            {
                MessageType = "Play",
                Data = command

            }, cancellationToken);
        }

        public Task SendPlaystateCommand(PlaystateRequest command, CancellationToken cancellationToken)
        {
            return SendMessage(new WebSocketMessage<PlaystateRequest>
            {
                MessageType = "Playstate",
                Data = command

            }, cancellationToken);
        }

        public Task SendLibraryUpdateInfo(LibraryUpdateInfo info, CancellationToken cancellationToken)
        {
            return SendMessages(new WebSocketMessage<LibraryUpdateInfo>
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
            return SendMessages(new WebSocketMessage<SystemInfo>
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
            return SendMessages(new WebSocketMessage<UserDataChangeInfo>
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
            return SendMessages(new WebSocketMessage<string>
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
            return SendMessages(new WebSocketMessage<string>
            {
                MessageType = "ServerRestarting",
                Data = string.Empty

            }, cancellationToken);
        }

        public Task SendGeneralCommand(GeneralCommand command, CancellationToken cancellationToken)
        {
            return SendMessage(new WebSocketMessage<GeneralCommand>
            {
                MessageType = "GeneralCommand",
                Data = command

            }, cancellationToken);
        }

        public Task SendSessionEndedNotification(SessionInfoDto sessionInfo, CancellationToken cancellationToken)
        {
            return SendMessages(new WebSocketMessage<SessionInfoDto>
            {
                MessageType = "SessionEnded",
                Data = sessionInfo

            }, cancellationToken);
        }

        public Task SendPlaybackStartNotification(SessionInfoDto sessionInfo, CancellationToken cancellationToken)
        {
            return SendMessages(new WebSocketMessage<SessionInfoDto>
            {
                MessageType = "PlaybackStart",
                Data = sessionInfo

            }, cancellationToken);
        }

        public Task SendPlaybackStoppedNotification(SessionInfoDto sessionInfo, CancellationToken cancellationToken)
        {
            return SendMessages(new WebSocketMessage<SessionInfoDto>
            {
                MessageType = "PlaybackStopped",
                Data = sessionInfo

            }, cancellationToken);
        }

        private Task SendMessage<T>(WebSocketMessage<T> message, CancellationToken cancellationToken)
        {
            var socket = GetActiveSocket();

            return socket.SendAsync(message, cancellationToken);
        }

        private Task SendMessages<T>(WebSocketMessage<T> message, CancellationToken cancellationToken)
        {
            var tasks = GetActiveSockets().Select(i => Task.Run(async () =>
            {
                try
                {
                    await i.SendAsync(message, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error sending web socket message", ex);
                }

            }, cancellationToken));

            return Task.WhenAll(tasks);
        }
    }
}
