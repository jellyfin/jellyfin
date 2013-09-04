using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionWebSocketListener" /> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="dtoService">The dto service.</param>
        public SessionWebSocketListener(ISessionManager sessionManager, ILogManager logManager, IDtoService dtoService)
        {
            _sessionManager = sessionManager;
            _logger = logManager.GetLogger(GetType().Name);
            _dtoService = dtoService;
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
                _logger.Debug("Received Identity message");

                var vals = message.Data.Split('|');

                var client = vals[0];
                var deviceId = vals[1];
                var version = vals[2];

                var session = _sessionManager.Sessions
                    .FirstOrDefault(i => string.Equals(i.DeviceId, deviceId) &&
                        string.Equals(i.Client, client) &&
                        string.Equals(i.ApplicationVersion, version));

                if (session != null)
                {
                    var sockets = session.WebSockets.Where(i => i.State == WebSocketState.Open).ToList();
                    sockets.Add(message.Connection);

                    session.WebSockets = sockets;
                }
                else
                {
                    _logger.Warn("Unable to determine session based on identity message: {0}", message.Data);
                }
            }
            else if (string.Equals(message.MessageType, "Context", StringComparison.OrdinalIgnoreCase))
            {
                var session = _sessionManager.Sessions.FirstOrDefault(i => i.WebSockets.Contains(message.Connection));

                if (session != null)
                {
                    var vals = message.Data.Split('|');

                    session.NowViewingItemType = vals[0];
                    session.NowViewingItemId = vals[1];
                    session.NowViewingItemName = vals[2];
                    session.NowViewingContext = vals.Length > 3 ? vals[3] : null;
                }
                else
                {
                    _logger.Warn("Unable to determine session based on context message: {0}", message.Data);
                }
            }
            else if (string.Equals(message.MessageType, "PlaybackStart", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Debug("Received PlaybackStart message");
                
                var session = _sessionManager.Sessions.FirstOrDefault(i => i.WebSockets.Contains(message.Connection));

                if (session != null && session.User != null)
                {
                    var item = _dtoService.GetItemByDtoId(message.Data);

                    _sessionManager.OnPlaybackStart(item, session.Id);
                }
            }
            else if (string.Equals(message.MessageType, "PlaybackProgress", StringComparison.OrdinalIgnoreCase))
            {
                var session = _sessionManager.Sessions.FirstOrDefault(i => i.WebSockets.Contains(message.Connection));

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

                    _sessionManager.OnPlaybackProgress(item, positionTicks, isPaused, isMuted, session.Id);
                }
            }
            else if (string.Equals(message.MessageType, "PlaybackStopped", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Debug("Received PlaybackStopped message");
                
                var session = _sessionManager.Sessions.FirstOrDefault(i => i.WebSockets.Contains(message.Connection));

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

                    _sessionManager.OnPlaybackStopped(item, positionTicks, session.Id);
                }
            }

            return _trueTaskResult;
        }
    }
}
