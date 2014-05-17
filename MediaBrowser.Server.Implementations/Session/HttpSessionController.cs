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

namespace MediaBrowser.Server.Implementations.Session
{
    public class HttpSessionController : ISessionController
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _json;
        private readonly IServerApplicationHost _appHost;

        public SessionInfo Session { get; private set; }

        //var postUrl = string.Format("http://{0}/mediabrowser/message", session.RemoteEndPoint);
        
        private readonly string _postUrl;

        public HttpSessionController(IHttpClient httpClient, 
            IJsonSerializer json, 
            IServerApplicationHost appHost, 
            SessionInfo session, 
            string postUrl)
        {
            _httpClient = httpClient;
            _json = json;
            _appHost = appHost;
            Session = session;
            _postUrl = postUrl;
        }

        public bool IsSessionActive
        {
            get
            {
                return (DateTime.UtcNow - Session.LastActivityDate).TotalMinutes <= 10;
            }
        }

        public bool SupportsMediaControl
        {
            get { return true; }
        }

        private Task SendMessage(object obj, CancellationToken cancellationToken)
        {
            var json = _json.SerializeToString(obj);

            return _httpClient.Post(new HttpRequestOptions
            {
                Url = _postUrl,
                CancellationToken = cancellationToken,
                RequestContent = json,
                RequestContentType = "application/json"
            });
        }

        public Task SendSessionEndedNotification(SessionInfoDto sessionInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendPlaybackStartNotification(SessionInfoDto sessionInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendPlaybackStoppedNotification(SessionInfoDto sessionInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
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
            return Task.FromResult(true);
        }

        public Task SendRestartRequiredNotification(CancellationToken cancellationToken)
        {
            return SendMessage(new WebSocketMessage<SystemInfo>
            {
                MessageType = "RestartRequired",
                Data = _appHost.GetSystemInfo()

            }, cancellationToken);
        }

        public Task SendUserDataChangeInfo(UserDataChangeInfo info, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendServerShutdownNotification(CancellationToken cancellationToken)
        {
            return SendMessage(new WebSocketMessage<string>
            {
                MessageType = "ServerShuttingDown",
                Data = string.Empty

            }, cancellationToken);
        }

        public Task SendServerRestartNotification(CancellationToken cancellationToken)
        {
            return SendMessage(new WebSocketMessage<string>
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
    }
}
