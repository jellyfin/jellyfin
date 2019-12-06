using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Session;

namespace Emby.Server.Implementations.Session
{
    public class HttpSessionController : ISessionController
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _json;
        private readonly ISessionManager _sessionManager;

        public SessionInfo Session { get; private set; }

        private readonly string _postUrl;

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
        }

        private string PostUrl => string.Format("http://{0}{1}", Session.RemoteEndPoint, _postUrl);

        public bool IsSessionActive => (DateTime.UtcNow - Session.LastActivityDate).TotalMinutes <= 5;

        public bool SupportsMediaControl => true;

        private Task SendMessage(string name, string messageId, CancellationToken cancellationToken)
        {
            return SendMessage(name, messageId, new Dictionary<string, string>(), cancellationToken);
        }

        private Task SendMessage(string name, string messageId, Dictionary<string, string> args, CancellationToken cancellationToken)
        {
            args["messageId"] = messageId;
            var url = PostUrl + "/" + name + ToQueryString(args);

            return SendRequest(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                BufferContent = false
            });
        }

        private Task SendPlayCommand(PlayRequest command, string messageId, CancellationToken cancellationToken)
        {
            var dict = new Dictionary<string, string>();

            dict["ItemIds"] = string.Join(",", command.ItemIds.Select(i => i.ToString("N", CultureInfo.InvariantCulture)).ToArray());

            if (command.StartPositionTicks.HasValue)
            {
                dict["StartPositionTicks"] = command.StartPositionTicks.Value.ToString(CultureInfo.InvariantCulture);
            }
            if (command.AudioStreamIndex.HasValue)
            {
                dict["AudioStreamIndex"] = command.AudioStreamIndex.Value.ToString(CultureInfo.InvariantCulture);
            }
            if (command.SubtitleStreamIndex.HasValue)
            {
                dict["SubtitleStreamIndex"] = command.SubtitleStreamIndex.Value.ToString(CultureInfo.InvariantCulture);
            }
            if (command.StartIndex.HasValue)
            {
                dict["StartIndex"] = command.StartIndex.Value.ToString(CultureInfo.InvariantCulture);
            }
            if (!string.IsNullOrEmpty(command.MediaSourceId))
            {
                dict["MediaSourceId"] = command.MediaSourceId;
            }

            return SendMessage(command.PlayCommand.ToString(), messageId, dict, cancellationToken);
        }

        private Task SendPlaystateCommand(PlaystateRequest command, string messageId, CancellationToken cancellationToken)
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

            return SendMessage(command.Command.ToString(), messageId, args, cancellationToken);
        }

        private string[] _supportedMessages = Array.Empty<string>();
        public Task SendMessage<T>(string name, string messageId, T data, ISessionController[] allControllers, CancellationToken cancellationToken)
        {
            if (!IsSessionActive)
            {
                return Task.CompletedTask;
            }

            if (string.Equals(name, "Play", StringComparison.OrdinalIgnoreCase))
            {
                return SendPlayCommand(data as PlayRequest, messageId, cancellationToken);
            }
            if (string.Equals(name, "PlayState", StringComparison.OrdinalIgnoreCase))
            {
                return SendPlaystateCommand(data as PlaystateRequest, messageId, cancellationToken);
            }
            if (string.Equals(name, "GeneralCommand", StringComparison.OrdinalIgnoreCase))
            {
                var command = data as GeneralCommand;
                return SendMessage(command.Name, messageId, command.Arguments, cancellationToken);
            }

            if (!_supportedMessages.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            var url = PostUrl + "/" + name;

            url += "?messageId=" + messageId;

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                BufferContent = false
            };

            if (data != null)
            {
                if (typeof(T) == typeof(string))
                {
                    var str = data as string;
                    if (!string.IsNullOrEmpty(str))
                    {
                        options.RequestContent = str;
                        options.RequestContentType = "application/json";
                    }
                }
                else
                {
                    options.RequestContent = _json.SerializeToString(data);
                    options.RequestContentType = "application/json";
                }
            }

            return SendRequest(options);
        }

        private async Task SendRequest(HttpRequestOptions options)
        {
            using (var response = await _httpClient.Post(options).ConfigureAwait(false))
            {

            }
        }

        private static string ToQueryString(Dictionary<string, string> nvc)
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
    }
}
