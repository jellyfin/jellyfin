using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.System;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Session
{
    public class HttpSessionController : ISessionController, IDisposable
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _json;
        private readonly ISessionManager _sessionManager;

        public SessionInfo Session { get; private set; }

        private readonly string _postUrl;

        private Timer _pingTimer;
        private DateTime _lastPingTime;

        public HttpSessionController(IHttpClient httpClient,
            IJsonSerializer json,
            SessionInfo session,
            string postUrl, ISessionManager sessionManager)
        {
            _httpClient = httpClient;
            _json = json;
            Session = session;
            _postUrl = postUrl;
            _sessionManager = sessionManager;

            _pingTimer = new Timer(PingTimerCallback, null, Timeout.Infinite, Timeout.Infinite);

            ResetPingTimer();
        }

        private string PostUrl
        {
            get
            {
                return string.Format("http://{0}{1}", Session.RemoteEndPoint, _postUrl);
            }
        }

        public bool IsSessionActive
        {
            get
            {
                return (DateTime.UtcNow - Session.LastActivityDate).TotalMinutes <= 20;
            }
        }

        public bool SupportsMediaControl
        {
            get { return true; }
        }

        private async void PingTimerCallback(object state)
        {
            try
            {
                await SendMessage("Ping", CancellationToken.None).ConfigureAwait(false);

                _lastPingTime = DateTime.UtcNow;
            }
            catch
            {
                var lastActivityDate = new[] { _lastPingTime, Session.LastActivityDate }
                    .Max();

                var timeSinceLastPing = DateTime.UtcNow - lastActivityDate;

                // We don't want to stop the session due to one single request failure
                // At the same time, we don't want the timeout to be too long because it will
                // be sitting in active sessions available for remote control, when it's not
                if (timeSinceLastPing >= TimeSpan.FromMinutes(5))
                {
                    ReportSessionEnded();
                }
            }
        }

        private void ReportSessionEnded()
        {
            try
            {
                _sessionManager.ReportSessionEnded(Session.Id);
            }
            catch (Exception ex)
            {
            }
        }

        private void ResetPingTimer()
        {
            if (_pingTimer != null)
            {
                _lastPingTime = DateTime.UtcNow;

                var period = TimeSpan.FromSeconds(60);

                _pingTimer.Change(period, period);
            }
        }

        private Task SendMessage(string name, CancellationToken cancellationToken)
        {
            return SendMessage(name, new Dictionary<string, string>(), cancellationToken);
        }

        private async Task SendMessage(string name,
            Dictionary<string, string> args,
            CancellationToken cancellationToken)
        {
            var url = PostUrl + "/" + name + ToQueryString(args);

            await _httpClient.Post(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false);

            ResetPingTimer();
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
            var dict = new Dictionary<string, string>();

            dict["ItemIds"] = string.Join(",", command.ItemIds);

            if (command.StartPositionTicks.HasValue)
            {
                dict["StartPositionTicks"] = command.StartPositionTicks.Value.ToString(CultureInfo.InvariantCulture);
            }

            return SendMessage(command.PlayCommand.ToString(), dict, cancellationToken);
        }

        public Task SendPlaystateCommand(PlaystateRequest command, CancellationToken cancellationToken)
        {
            var args = new Dictionary<string, string>();

            if (command.Command == PlaystateCommand.Seek)
            {
                if (!command.SeekPositionTicks.HasValue)
                {
                    throw new ArgumentException("SeekPositionTicks cannot be null");
                }

                args["SeekPositionTicks"] = command.SeekPositionTicks.Value.ToString(CultureInfo.InvariantCulture);
            }

            return SendMessage(command.Command.ToString(), args, cancellationToken);
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
            return SendMessage(command.Name, command.Arguments, cancellationToken);
        }

        private string ToQueryString(Dictionary<string, string> nvc)
        {
            var array = (from item in nvc
                         select string.Format("{0}={1}", WebUtility.UrlEncode(item.Key), WebUtility.UrlEncode(item.Value)))
                .ToArray();

            var args = string.Join("&", array);

            if (string.IsNullOrEmpty(args))
            {
                return args;
            }

            return "?" + args;
        }

        public void Dispose()
        {
            DisposePingTimer();
        }

        private void DisposePingTimer()
        {
            if (_pingTimer != null)
            {
                _pingTimer.Dispose();
                _pingTimer = null;
            }
        }
    }
}
