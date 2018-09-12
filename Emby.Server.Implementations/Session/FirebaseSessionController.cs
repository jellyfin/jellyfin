using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Net;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using MediaBrowser.Common;

namespace Emby.Server.Implementations.Session
{
    public class FirebaseSessionController : ISessionController
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _json;
        private readonly ISessionManager _sessionManager;

        public SessionInfo Session { get; private set; }

        private readonly string _token;

        private IApplicationHost _appHost;
        private string _senderId;
        private string _applicationId;

        public FirebaseSessionController(IHttpClient httpClient,
            IApplicationHost appHost,
            IJsonSerializer json,
            SessionInfo session,
            string token, ISessionManager sessionManager)
        {
            _httpClient = httpClient;
            _json = json;
            _appHost = appHost;
            Session = session;
            _token = token;
            _sessionManager = sessionManager;

            _applicationId = _appHost.GetValue("firebase_applicationid");
            _senderId = _appHost.GetValue("firebase_senderid");
        }

        public static bool IsSupported(IApplicationHost appHost)
        {
            return !string.IsNullOrEmpty(appHost.GetValue("firebase_applicationid")) && !string.IsNullOrEmpty(appHost.GetValue("firebase_senderid"));
        }

        public bool IsSessionActive
        {
            get
            {
                return (DateTime.UtcNow - Session.LastActivityDate).TotalDays <= 3;
            }
        }

        public bool SupportsMediaControl
        {
            get { return true; }
        }

        public async Task SendMessage<T>(string name, string messageId, T data, ISessionController[] allControllers, CancellationToken cancellationToken)
        {
            if (!IsSessionActive)
            {
                return;
            }

            if (string.IsNullOrEmpty(_senderId) || string.IsNullOrEmpty(_applicationId))
            {
                return;
            }

            foreach (var controller in allControllers)
            {
                // Don't send if there's an active web socket connection
                if ((controller is WebSocketController) && controller.IsSessionActive)
                {
                    return;
                }
            }

            var msg = new WebSocketMessage<T>
            {
                Data = data,
                MessageType = name,
                MessageId = messageId,
                ServerId = _appHost.SystemId
            };

            var req = new FirebaseBody<T>
            {
                to = _token,
                data = msg
            };

            var byteArray = Encoding.UTF8.GetBytes(_json.SerializeToString(req));

            var enableLogging = false;

#if DEBUG
            enableLogging = true;
#endif

            var options = new HttpRequestOptions
            {
                Url = "https://fcm.googleapis.com/fcm/send",
                RequestContentType = "application/json",
                RequestContentBytes = byteArray,
                CancellationToken = cancellationToken,
                LogRequest = enableLogging,
                LogResponse = enableLogging,
                LogErrors = enableLogging
            };

            options.RequestHeaders["Authorization"] = string.Format("key={0}", _applicationId);
            options.RequestHeaders["Sender"] = string.Format("id={0}", _senderId);

            using (var response = await _httpClient.Post(options).ConfigureAwait(false))
            {

            }
        }
    }

    internal class FirebaseBody<T>
    {
        public string to { get; set; }
        public WebSocketMessage<T> data { get; set; }
    }
}
