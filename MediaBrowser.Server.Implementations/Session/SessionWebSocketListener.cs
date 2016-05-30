using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Session
{
    /// <summary>
    /// Class SessionWebSocketListener
    /// </summary>
    public class SessionWebSocketListener : IWebSocketListener, IDisposable
    {
        /// <summary>
        /// The _true task result
        /// </summary>
        private readonly Task _trueTaskResult = Task.FromResult(true);

        /// <summary>
        /// The _session manager
        /// </summary>
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The _dto service
        /// </summary>
        private readonly IJsonSerializer _json;

        private readonly IHttpServer _httpServer;
        private readonly IServerManager _serverManager;


        /// <summary>
        /// Initializes a new instance of the <see cref="SessionWebSocketListener" /> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="json">The json.</param>
        /// <param name="httpServer">The HTTP server.</param>
        /// <param name="serverManager">The server manager.</param>
        public SessionWebSocketListener(ISessionManager sessionManager, ILogManager logManager, IJsonSerializer json, IHttpServer httpServer, IServerManager serverManager)
        {
            _sessionManager = sessionManager;
            _logger = logManager.GetLogger(GetType().Name);
            _json = json;
            _httpServer = httpServer;
            _serverManager = serverManager;
            httpServer.WebSocketConnecting += _httpServer_WebSocketConnecting;
            serverManager.WebSocketConnected += _serverManager_WebSocketConnected;
        }

        async void _serverManager_WebSocketConnected(object sender, GenericEventArgs<IWebSocketConnection> e)
        {
            var session = await GetSession(e.Argument.QueryString, e.Argument.RemoteEndPoint).ConfigureAwait(false);

            if (session != null)
            {
                var controller = session.SessionController as WebSocketController;

                if (controller == null)
                {
                    controller = new WebSocketController(session, _logger, _sessionManager);
                }

                controller.AddWebSocket(e.Argument);

                session.SessionController = controller;
            }
            else
            {
                _logger.Warn("Unable to determine session based on url: {0}", e.Argument.Url);
            }
        }

        async void _httpServer_WebSocketConnecting(object sender, WebSocketConnectingEventArgs e)
        {
            //var token = e.QueryString["api_key"];
            //if (!string.IsNullOrWhiteSpace(token))
            //{
            //    try
            //    {
            //        var session = await GetSession(e.QueryString, e.Endpoint).ConfigureAwait(false);

            //        if (session == null)
            //        {
            //            e.AllowConnection = false;
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.ErrorException("Error getting session info", ex);
            //    }
            //}
        }

        private Task<SessionInfo> GetSession(NameValueCollection queryString, string remoteEndpoint)
        {
            if (queryString == null)
            {
                throw new ArgumentNullException("queryString");
            }

            var token = queryString["api_key"];
            if (string.IsNullOrWhiteSpace(token))
            {
                return Task.FromResult<SessionInfo>(null);
            }
            var deviceId = queryString["deviceId"];
            return _sessionManager.GetSessionByAuthenticationToken(token, deviceId, remoteEndpoint);
        }

        public void Dispose()
        {
            _httpServer.WebSocketConnecting -= _httpServer_WebSocketConnecting;
            _serverManager.WebSocketConnected -= _serverManager_WebSocketConnected;
        }

        /// <summary>
        /// Processes the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>Task.</returns>
        public Task ProcessMessage(WebSocketMessageInfo message)
        {
            if (string.Equals(message.MessageType, "Identity", StringComparison.OrdinalIgnoreCase))
            {
                ProcessIdentityMessage(message);
            }
            else if (string.Equals(message.MessageType, "Context", StringComparison.OrdinalIgnoreCase))
            {
                ProcessContextMessage(message);
            }
            else if (string.Equals(message.MessageType, "PlaybackStart", StringComparison.OrdinalIgnoreCase))
            {
                OnPlaybackStart(message);
            }
            else if (string.Equals(message.MessageType, "PlaybackProgress", StringComparison.OrdinalIgnoreCase))
            {
                OnPlaybackProgress(message);
            }
            else if (string.Equals(message.MessageType, "PlaybackStopped", StringComparison.OrdinalIgnoreCase))
            {
                OnPlaybackStopped(message);
            }
            else if (string.Equals(message.MessageType, "ReportPlaybackStart", StringComparison.OrdinalIgnoreCase))
            {
                ReportPlaybackStart(message);
            }
            else if (string.Equals(message.MessageType, "ReportPlaybackProgress", StringComparison.OrdinalIgnoreCase))
            {
                ReportPlaybackProgress(message);
            }
            else if (string.Equals(message.MessageType, "ReportPlaybackStopped", StringComparison.OrdinalIgnoreCase))
            {
                ReportPlaybackStopped(message);
            }

            return _trueTaskResult;
        }

        /// <summary>
        /// Processes the identity message.
        /// </summary>
        /// <param name="message">The message.</param>
        private async void ProcessIdentityMessage(WebSocketMessageInfo message)
        {
            _logger.Debug("Received Identity message: " + message.Data);

            var vals = message.Data.Split('|');

            if (vals.Length < 3)
            {
                _logger.Error("Client sent invalid identity message.");
                return;
            }

            var client = vals[0];
            var deviceId = vals[1];
            var version = vals[2];
            var deviceName = vals.Length > 3 ? vals[3] : string.Empty;

            var session = _sessionManager.GetSession(deviceId, client, version);

            if (session == null && !string.IsNullOrEmpty(deviceName))
            {
                _logger.Debug("Logging session activity");

                session = await _sessionManager.LogSessionActivity(client, version, deviceId, deviceName, message.Connection.RemoteEndPoint, null).ConfigureAwait(false);
            }

            if (session != null)
            {
                var controller = session.SessionController as WebSocketController;

                if (controller == null)
                {
                    controller = new WebSocketController(session, _logger, _sessionManager);
                }

                controller.AddWebSocket(message.Connection);

                session.SessionController = controller;
            }
            else
            {
                _logger.Warn("Unable to determine session based on identity message: {0}", message.Data);
            }
        }

        /// <summary>
        /// Processes the context message.
        /// </summary>
        /// <param name="message">The message.</param>
        private void ProcessContextMessage(WebSocketMessageInfo message)
        {
            var session = GetSessionFromMessage(message);

            if (session != null)
            {
                var vals = message.Data.Split('|');

                var itemId = vals[1];

                if (!string.IsNullOrWhiteSpace(itemId))
                {
                    _sessionManager.ReportNowViewingItem(session.Id, itemId);
                }
            }
        }

        /// <summary>
        /// Gets the session from message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>SessionInfo.</returns>
        private SessionInfo GetSessionFromMessage(WebSocketMessageInfo message)
        {
            var result = _sessionManager.Sessions.FirstOrDefault(i =>
            {
                var controller = i.SessionController as WebSocketController;

                if (controller != null)
                {
                    if (controller.Sockets.Any(s => s.Id == message.Connection.Id))
                    {
                        return true;
                    }
                }

                return false;

            });

            if (result == null)
            {
                _logger.Error("Unable to find session based on web socket message");
            }

            return result;
        }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        /// <summary>
        /// Reports the playback start.
        /// </summary>
        /// <param name="message">The message.</param>
        private void OnPlaybackStart(WebSocketMessageInfo message)
        {
            _logger.Debug("Received PlaybackStart message");

            var session = GetSessionFromMessage(message);

            if (session != null && session.UserId.HasValue)
            {
                var vals = message.Data.Split('|');

                var itemId = vals[0];

                var queueableMediaTypes = string.Empty;
                var canSeek = true;

                if (vals.Length > 1)
                {
                    canSeek = string.Equals(vals[1], "true", StringComparison.OrdinalIgnoreCase);
                }
                if (vals.Length > 2)
                {
                    queueableMediaTypes = vals[2];
                }

                var info = new PlaybackStartInfo
                {
                    CanSeek = canSeek,
                    ItemId = itemId,
                    SessionId = session.Id,
                    QueueableMediaTypes = queueableMediaTypes.Split(',').ToList()
                };

                if (vals.Length > 3)
                {
                    info.MediaSourceId = vals[3];
                }

                if (vals.Length > 4 && !string.IsNullOrWhiteSpace(vals[4]))
                {
                    info.AudioStreamIndex = int.Parse(vals[4], _usCulture);
                }

                if (vals.Length > 5 && !string.IsNullOrWhiteSpace(vals[5]))
                {
                    info.SubtitleStreamIndex = int.Parse(vals[5], _usCulture);
                }

                _sessionManager.OnPlaybackStart(info);
            }
        }

        private void ReportPlaybackStart(WebSocketMessageInfo message)
        {
            _logger.Debug("Received ReportPlaybackStart message");

            var session = GetSessionFromMessage(message);

            if (session != null && session.UserId.HasValue)
            {
                var info = _json.DeserializeFromString<PlaybackStartInfo>(message.Data);

                info.SessionId = session.Id;

                _sessionManager.OnPlaybackStart(info);
            }
        }

        private void ReportPlaybackProgress(WebSocketMessageInfo message)
        {
            //_logger.Debug("Received ReportPlaybackProgress message");

            var session = GetSessionFromMessage(message);

            if (session != null && session.UserId.HasValue)
            {
                var info = _json.DeserializeFromString<PlaybackProgressInfo>(message.Data);

                info.SessionId = session.Id;

                _sessionManager.OnPlaybackProgress(info);
            }
        }

        /// <summary>
        /// Reports the playback progress.
        /// </summary>
        /// <param name="message">The message.</param>
        private void OnPlaybackProgress(WebSocketMessageInfo message)
        {
            var session = GetSessionFromMessage(message);

            if (session != null && session.UserId.HasValue)
            {
                var vals = message.Data.Split('|');

                var itemId = vals[0];

                long? positionTicks = null;

                if (vals.Length > 1)
                {
                    long pos;

                    if (long.TryParse(vals[1], out pos))
                    {
                        positionTicks = pos;
                    }
                }

                var isPaused = vals.Length > 2 && string.Equals(vals[2], "true", StringComparison.OrdinalIgnoreCase);
                var isMuted = vals.Length > 3 && string.Equals(vals[3], "true", StringComparison.OrdinalIgnoreCase);

                var info = new PlaybackProgressInfo
                {
                    ItemId = itemId,
                    PositionTicks = positionTicks,
                    IsMuted = isMuted,
                    IsPaused = isPaused,
                    SessionId = session.Id
                };

                if (vals.Length > 4)
                {
                    info.MediaSourceId = vals[4];
                }

                if (vals.Length > 5 && !string.IsNullOrWhiteSpace(vals[5]))
                {
                    info.VolumeLevel = int.Parse(vals[5], _usCulture);
                }

                if (vals.Length > 5 && !string.IsNullOrWhiteSpace(vals[6]))
                {
                    info.AudioStreamIndex = int.Parse(vals[6], _usCulture);
                }

                if (vals.Length > 7 && !string.IsNullOrWhiteSpace(vals[7]))
                {
                    info.SubtitleStreamIndex = int.Parse(vals[7], _usCulture);
                }

                _sessionManager.OnPlaybackProgress(info);
            }
        }

        private void ReportPlaybackStopped(WebSocketMessageInfo message)
        {
            _logger.Debug("Received ReportPlaybackStopped message");

            var session = GetSessionFromMessage(message);

            if (session != null && session.UserId.HasValue)
            {
                var info = _json.DeserializeFromString<PlaybackStopInfo>(message.Data);

                info.SessionId = session.Id;

                _sessionManager.OnPlaybackStopped(info);
            }
        }

        /// <summary>
        /// Reports the playback stopped.
        /// </summary>
        /// <param name="message">The message.</param>
        private void OnPlaybackStopped(WebSocketMessageInfo message)
        {
            _logger.Debug("Received PlaybackStopped message");

            var session = GetSessionFromMessage(message);

            if (session != null && session.UserId.HasValue)
            {
                var vals = message.Data.Split('|');

                var itemId = vals[0];

                long? positionTicks = null;

                if (vals.Length > 1)
                {
                    long pos;

                    if (long.TryParse(vals[1], out pos))
                    {
                        positionTicks = pos;
                    }
                }

                var info = new PlaybackStopInfo
                {
                    ItemId = itemId,
                    PositionTicks = positionTicks,
                    SessionId = session.Id
                };

                if (vals.Length > 2)
                {
                    info.MediaSourceId = vals[2];
                }

                _sessionManager.OnPlaybackStopped(info);
            }
        }
    }
}
