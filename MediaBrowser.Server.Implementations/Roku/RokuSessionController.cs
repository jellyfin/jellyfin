using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.System;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Roku
{
    public class RokuSessionController : ISessionController
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _json;
        private readonly IServerApplicationHost _appHost;

        public SessionInfo Session { get; private set; }

        public RokuSessionController(IHttpClient httpClient, IJsonSerializer json, IServerApplicationHost appHost, SessionInfo session)
        {
            _httpClient = httpClient;
            _json = json;
            _appHost = appHost;
            Session = session;
        }

        public bool SupportsMediaRemoteControl
        {
            get { return false; }
        }

        public bool IsSessionActive
        {
            get
            {
                return (DateTime.UtcNow - Session.LastActivityDate).TotalMinutes <= 10;
            }
        }

        public Task SendMessageCommand(MessageCommand command, CancellationToken cancellationToken)
        {
            return SendCommand(new WebSocketMessage<MessageCommand>
            {
                MessageType = "MessageCommand",
                Data = command

            }, cancellationToken);
        }

        public Task SendPlayCommand(PlayRequest command, CancellationToken cancellationToken)
        {
            return SendCommand(new WebSocketMessage<PlayRequest>
            {
                MessageType = "Play",
                Data = command

            }, cancellationToken);
        }

        public Task SendBrowseCommand(BrowseRequest command, CancellationToken cancellationToken)
        {
            return SendCommand(new WebSocketMessage<BrowseRequest>
            {
                MessageType = "Browse",
                Data = command

            }, cancellationToken);
        }

        public Task SendPlaystateCommand(PlaystateRequest command, CancellationToken cancellationToken)
        {
            return SendCommand(new WebSocketMessage<PlaystateRequest>
            {
                MessageType = "Playstate",
                Data = command

            }, cancellationToken);
        }

        private readonly Task _cachedTask = Task.FromResult(true);
        public Task SendLibraryUpdateInfo(LibraryUpdateInfo info, CancellationToken cancellationToken)
        {
            // Roku probably won't care about this
            return _cachedTask;
        }

        public Task SendRestartRequiredNotification(CancellationToken cancellationToken)
        {
            return SendCommand(new WebSocketMessage<SystemInfo>
            {
                MessageType = "RestartRequired",
                Data = _appHost.GetSystemInfo()

            }, cancellationToken);
        }

        public Task SendUserDataChangeInfo(UserDataChangeInfo info, CancellationToken cancellationToken)
        {
            // Roku probably won't care about this
            return _cachedTask;
        }

        public Task SendServerShutdownNotification(CancellationToken cancellationToken)
        {
            return SendCommand(new WebSocketMessage<string>
            {
                MessageType = "ServerShuttingDown",
                Data = string.Empty

            }, cancellationToken);
        }

        public Task SendServerRestartNotification(CancellationToken cancellationToken)
        {
            return SendCommand(new WebSocketMessage<string>
            {
                MessageType = "ServerRestarting",
                Data = string.Empty

            }, cancellationToken);
        }

        private Task SendCommand(object obj, CancellationToken cancellationToken)
        {
            var json = _json.SerializeToString(obj);

            return _httpClient.Post(new HttpRequestOptions
            {
                Url = "http://" + Session.RemoteEndPoint + "/mb/remotecontrol",
                CancellationToken = cancellationToken,
                RequestContent = json,
                RequestContentType = "application/json"
            });
        }


        public Task SendGeneralCommand(GeneralCommand command, CancellationToken cancellationToken)
        {
            return SendCommand(new WebSocketMessage<GeneralCommand>
            {
                MessageType = "Command",
                Data = command

            }, cancellationToken);
        }
    }
}
