using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Session
{
    /// <summary>
    /// Class SessionWebSocketListener
    /// </summary>
    public class SessionWebSocketListener : IWebSocketListener
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
        private readonly IDtoService _dtoService;
        private readonly IServerApplicationHost _appHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionWebSocketListener" /> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="dtoService">The dto service.</param>
        public SessionWebSocketListener(ISessionManager sessionManager, ILogManager logManager, IDtoService dtoService, IServerApplicationHost appHost)
        {
            _sessionManager = sessionManager;
            _logger = logManager.GetLogger(GetType().Name);
            _dtoService = dtoService;
            _appHost = appHost;
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
                ReportPlaybackStart(message);
            }
            else if (string.Equals(message.MessageType, "PlaybackProgress", StringComparison.OrdinalIgnoreCase))
            {
                ReportPlaybackProgress(message);
            }
            else if (string.Equals(message.MessageType, "PlaybackStopped", StringComparison.OrdinalIgnoreCase))
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

            var client = vals[0];
            var deviceId = vals[1];
            var version = vals[2];
            var deviceName = vals.Length > 3 ? vals[3] : string.Empty;

            var session = _sessionManager.Sessions
                .FirstOrDefault(i => string.Equals(i.DeviceId, deviceId) &&
                    string.Equals(i.Client, client) &&
                    string.Equals(i.ApplicationVersion, version));

            if (session == null && !string.IsNullOrEmpty(deviceName))
            {
                _logger.Debug("Logging session activity");

                await _sessionManager.LogSessionActivity(client, version, deviceId, deviceName, message.Connection.RemoteEndPoint, null).ConfigureAwait(false);

                session = _sessionManager.Sessions
                    .FirstOrDefault(i => string.Equals(i.DeviceId, deviceId) &&
                        string.Equals(i.Client, client) &&
                        string.Equals(i.ApplicationVersion, version));
            }

            if (session != null)
            {
                var controller = new WebSocketController(session, _appHost);
                controller.Sockets.Add(message.Connection);

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

                session.NowViewingItemType = vals[0];
                session.NowViewingItemId = vals[1];
                session.NowViewingItemName = vals[2];
                session.NowViewingContext = vals.Length > 3 ? vals[3] : null;
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

        /// <summary>
        /// Reports the playback start.
        /// </summary>
        /// <param name="message">The message.</param>
        private void ReportPlaybackStart(WebSocketMessageInfo message)
        {
            _logger.Debug("Received PlaybackStart message");

            var session = GetSessionFromMessage(message);

            if (session != null && session.User != null)
            {
                var vals = message.Data.Split('|');

                var item = _dtoService.GetItemByDtoId(vals[0]);

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

                var info = new PlaybackInfo
                {
                    CanSeek = canSeek,
                    Item = item,
                    SessionId = session.Id,
                    QueueableMediaTypes = queueableMediaTypes.Split(',').ToList()
                };

                _sessionManager.OnPlaybackStart(info);
            }
        }

        /// <summary>
        /// Reports the playback progress.
        /// </summary>
        /// <param name="message">The message.</param>
        private void ReportPlaybackProgress(WebSocketMessageInfo message)
        {
            var session = GetSessionFromMessage(message);

            if (session != null && session.User != null)
            {
                var vals = message.Data.Split('|');

                var item = _dtoService.GetItemByDtoId(vals[0]);

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
                    Item = item,
                    PositionTicks = positionTicks,
                    IsMuted = isMuted,
                    IsPaused = isPaused,
                    SessionId = session.Id
                };

                _sessionManager.OnPlaybackProgress(info);
            }
        }

        /// <summary>
        /// Reports the playback stopped.
        /// </summary>
        /// <param name="message">The message.</param>
        private void ReportPlaybackStopped(WebSocketMessageInfo message)
        {
            _logger.Debug("Received PlaybackStopped message");

            var session = GetSessionFromMessage(message);

            if (session != null && session.User != null)
            {
                var vals = message.Data.Split('|');

                var item = _dtoService.GetItemByDtoId(vals[0]);

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
                    Item = item,
                    PositionTicks = positionTicks,
                    SessionId = session.Id
                };

                _sessionManager.OnPlaybackStopped(info);
            }
        }
    }
}
