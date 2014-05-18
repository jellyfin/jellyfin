using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using MediaBrowser.Common.Net;
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

        public SessionInfo Session { get; private set; }

        private readonly string _postUrl;

        public HttpSessionController(IHttpClient httpClient,
            IJsonSerializer json,
            SessionInfo session,
            string postUrl)
        {
            _httpClient = httpClient;
            _json = json;
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

        private Task SendMessage(string name, CancellationToken cancellationToken)
        {
            return SendMessage(name, new NameValueCollection(), cancellationToken);
        }

        private Task SendMessage(string name, NameValueCollection args, CancellationToken cancellationToken)
        {
            return SendMessage(new WebSocketMessage<string>
            {
                MessageType = name,
                Data = string.Empty

            }, cancellationToken);
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
            return Task.FromResult(true);
            //return SendMessage(new WebSocketMessage<PlayRequest>
            //{
            //    MessageType = "Play",
            //    Data = command

            //}, cancellationToken);
        }

        public Task SendPlaystateCommand(PlaystateRequest command, CancellationToken cancellationToken)
        {
            var args = new Dictionary<string, string>();

            if (command.Command == PlaystateCommand.Seek)
            {

            }

            return SendMessage(command.Command.ToString(), cancellationToken);
        }

        public Task SendLibraryUpdateInfo(LibraryUpdateInfo info, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendRestartRequiredNotification(SystemInfo info, CancellationToken cancellationToken)
        {
            return SendMessage("RestartRequired", cancellationToken);
        }

        public Task SendUserDataChangeInfo(UserDataChangeInfo info, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendServerShutdownNotification(CancellationToken cancellationToken)
        {
            return SendMessage("ServerShuttingDown", cancellationToken);
        }

        public Task SendServerRestartNotification(CancellationToken cancellationToken)
        {
            return SendMessage("ServerRestarting", cancellationToken);
        }

        public Task SendGeneralCommand(GeneralCommand command, CancellationToken cancellationToken)
        {
            return SendMessage(new WebSocketMessage<GeneralCommand>
            {
                MessageType = "GeneralCommand",
                Data = command

            }, cancellationToken);
        }

        private string ToQueryString(Dictionary<string, string> nvc)
        {
            var array = (from item in nvc
                         select string.Format("{0}={1}", WebUtility.UrlEncode(item.Key), WebUtility.UrlEncode(item.Value)))
                .ToArray();
            return "?" + string.Join("&", array);
        }
    }
}
