using System.Globalization;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Model.Net;

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

        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionWebSocketListener" /> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userManager">The user manager.</param>
        public SessionWebSocketListener(ISessionManager sessionManager, ILogger logger, ILibraryManager libraryManager, IUserManager userManager)
        {
            _sessionManager = sessionManager;
            _logger = logger;
            _libraryManager = libraryManager;
            _userManager = userManager;
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
                var vals = message.Data.Split('|');

                var client = vals[0];
                var deviceId = vals[1];

                var session = _sessionManager.Sessions.FirstOrDefault(i => string.Equals(i.DeviceId, deviceId) && string.Equals(i.Client, client));

                if (session != null)
                {
                    var sockets = session.WebSockets.Where(i => i.State == WebSocketState.Open).ToList();
                    sockets.Add(message.Connection);

                    session.WebSockets = sockets;
                }
            }
            else if (string.Equals(message.MessageType, "Context", StringComparison.OrdinalIgnoreCase))
            {
                var session = _sessionManager.Sessions.FirstOrDefault(i => i.WebSockets.Contains(message.Connection));

                if (session != null)
                {
                    var vals = message.Data.Split('|');

                    session.NowViewingItemType = vals[0];
                    session.NowViewingItemIdentifier = vals[1];
                    session.NowViewingContext = vals.Length > 2 ? vals[2] : null;
                }
            }
            else if (string.Equals(message.MessageType, "PlaybackStart", StringComparison.OrdinalIgnoreCase))
            {
                var session = _sessionManager.Sessions.FirstOrDefault(i => i.WebSockets.Contains(message.Connection));

                if (session != null && session.UserId.HasValue)
                {
                    var item = DtoBuilder.GetItemByClientId(message.Data, _userManager, _libraryManager);

                    _sessionManager.OnPlaybackStart(_userManager.GetUserById(session.UserId.Value), item, session.Client, session.DeviceId, session.DeviceName);
                }
            }
            else if (string.Equals(message.MessageType, "PlaybackProgress", StringComparison.OrdinalIgnoreCase))
            {
                var session = _sessionManager.Sessions.FirstOrDefault(i => i.WebSockets.Contains(message.Connection));

                if (session != null && session.UserId.HasValue)
                {
                    var vals = message.Data.Split('|');

                    var item = DtoBuilder.GetItemByClientId(vals[0], _userManager, _libraryManager);

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

                    _sessionManager.OnPlaybackProgress(_userManager.GetUserById(session.UserId.Value), item, positionTicks, isPaused, session.Client, session.DeviceId, session.DeviceName);
                }
            }
            else if (string.Equals(message.MessageType, "PlaybackStopped", StringComparison.OrdinalIgnoreCase))
            {
                var session = _sessionManager.Sessions.FirstOrDefault(i => i.WebSockets.Contains(message.Connection));

                if (session != null && session.UserId.HasValue)
                {
                    var vals = message.Data.Split('|');

                    var item = DtoBuilder.GetItemByClientId(vals[0], _userManager, _libraryManager);

                    long? positionTicks = null;

                    if (vals.Length > 1)
                    {
                        long pos;

                        if (long.TryParse(vals[1], out pos))
                        {
                            positionTicks = pos;
                        }
                    }

                    _sessionManager.OnPlaybackStopped(_userManager.GetUserById(session.UserId.Value), item, positionTicks, session.Client, session.DeviceId, session.DeviceName);
                }
            }

            return _trueTaskResult;
        }
    }
}
