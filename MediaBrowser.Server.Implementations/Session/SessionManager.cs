using MediaBrowser.Common.Events;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Session
{
    /// <summary>
    /// Class SessionManager
    /// </summary>
    public class SessionManager : ISessionManager
    {
        /// <summary>
        /// The _user data repository
        /// </summary>
        private readonly IUserDataManager _userDataRepository;

        /// <summary>
        /// The _user repository
        /// </summary>
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Gets or sets the configuration manager.
        /// </summary>
        /// <value>The configuration manager.</value>
        private readonly IServerConfigurationManager _configurationManager;

        /// <summary>
        /// The _active connections
        /// </summary>
        private readonly ConcurrentDictionary<string, SessionInfo> _activeConnections =
            new ConcurrentDictionary<string, SessionInfo>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Occurs when [playback start].
        /// </summary>
        public event EventHandler<PlaybackProgressEventArgs> PlaybackStart;
        /// <summary>
        /// Occurs when [playback progress].
        /// </summary>
        public event EventHandler<PlaybackProgressEventArgs> PlaybackProgress;
        /// <summary>
        /// Occurs when [playback stopped].
        /// </summary>
        public event EventHandler<PlaybackProgressEventArgs> PlaybackStopped;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionManager"/> class.
        /// </summary>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="userRepository">The user repository.</param>
        public SessionManager(IUserDataManager userDataRepository, IServerConfigurationManager configurationManager, ILogger logger, IUserRepository userRepository)
        {
            _userDataRepository = userDataRepository;
            _configurationManager = configurationManager;
            _logger = logger;
            _userRepository = userRepository;
        }

        /// <summary>
        /// Gets all connections.
        /// </summary>
        /// <value>All connections.</value>
        public IEnumerable<SessionInfo> Sessions
        {
            get { return _activeConnections.Values.OrderByDescending(c => c.LastActivityDate).ToList(); }
        }

        /// <summary>
        /// Logs the user activity.
        /// </summary>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="appVersion">The app version.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <param name="user">The user.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.UnauthorizedAccessException"></exception>
        /// <exception cref="System.ArgumentNullException">user</exception>
        public async Task<SessionInfo> LogSessionActivity(string clientType, string appVersion, string deviceId, string deviceName, User user)
        {
            if (string.IsNullOrEmpty(clientType))
            {
                throw new ArgumentNullException("clientType");
            }
            if (string.IsNullOrEmpty(appVersion))
            {
                throw new ArgumentNullException("appVersion");
            }
            if (string.IsNullOrEmpty(deviceId))
            {
                throw new ArgumentNullException("deviceId");
            }
            if (string.IsNullOrEmpty(deviceName))
            {
                throw new ArgumentNullException("deviceName");
            }

            if (user != null && user.Configuration.IsDisabled)
            {
                throw new UnauthorizedAccessException(string.Format("The {0} account is currently disabled. Please consult with your administrator.", user.Name));
            }

            var activityDate = DateTime.UtcNow;

            var session = GetSessionInfo(clientType, appVersion, deviceId, deviceName, user);

            session.LastActivityDate = activityDate;

            if (user == null)
            {
                return session;
            }

            var lastActivityDate = user.LastActivityDate;

            user.LastActivityDate = activityDate;

            // Don't log in the db anymore frequently than 10 seconds
            if (lastActivityDate.HasValue && (activityDate - lastActivityDate.Value).TotalSeconds < 10)
            {
                return session;
            }

            // Save this directly. No need to fire off all the events for this.
            await _userRepository.SaveUser(user, CancellationToken.None).ConfigureAwait(false);

            return session;
        }

        /// <summary>
        /// Updates the now playing item id.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="item">The item.</param>
        /// <param name="isPaused">if set to <c>true</c> [is paused].</param>
        /// <param name="currentPositionTicks">The current position ticks.</param>
        private void UpdateNowPlayingItem(SessionInfo session, BaseItem item, bool isPaused, bool isMuted, long? currentPositionTicks = null)
        {
            session.IsMuted = isMuted;
            session.IsPaused = isPaused;
            session.NowPlayingPositionTicks = currentPositionTicks;
            session.NowPlayingItem = item;
            session.LastActivityDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Removes the now playing item id.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="item">The item.</param>
        private void RemoveNowPlayingItem(SessionInfo session, BaseItem item)
        {
            if (session.NowPlayingItem != null && session.NowPlayingItem.Id == item.Id)
            {
                session.NowPlayingItem = null;
                session.NowPlayingPositionTicks = null;
                session.IsPaused = false;
            }
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="appVersion">The app version.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <param name="user">The user.</param>
        /// <returns>SessionInfo.</returns>
        private SessionInfo GetSessionInfo(string clientType, string appVersion, string deviceId, string deviceName, User user)
        {
            var key = clientType + deviceId + appVersion;

            var connection = _activeConnections.GetOrAdd(key, keyName => new SessionInfo
            {
                Client = clientType,
                DeviceId = deviceId,
                ApplicationVersion = appVersion,
                Id = Guid.NewGuid()
            });

            connection.DeviceName = deviceName;
            connection.User = user;

            return connection;
        }

        /// <summary>
        /// Used to report that playback has started for an item
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">info</exception>
        public async Task OnPlaybackStart(PlaybackInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            if (info.SessionId == Guid.Empty)
            {
                throw new ArgumentNullException("info");
            }

            var session = Sessions.First(i => i.Id.Equals(info.SessionId));

            var item = info.Item;

            UpdateNowPlayingItem(session, item, false, false);

            session.CanSeek = info.CanSeek;
            session.QueueableMediaTypes = info.QueueableMediaTypes;

            var key = item.GetUserDataKey();

            var user = session.User;

            var data = _userDataRepository.GetUserData(user.Id, key);

            data.PlayCount++;
            data.LastPlayedDate = DateTime.UtcNow;

            if (!(item is Video))
            {
                data.Played = true;
            }

            await _userDataRepository.SaveUserData(user.Id, key, data, UserDataSaveReason.PlaybackStart, CancellationToken.None).ConfigureAwait(false);

            // Nothing to save here
            // Fire events to inform plugins
            EventHelper.QueueEventIfNotNull(PlaybackStart, this, new PlaybackProgressEventArgs
            {
                Item = item,
                User = user
            }, _logger);
        }

        /// <summary>
        /// Used to report playback progress for an item
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException">positionTicks</exception>
        public async Task OnPlaybackProgress(PlaybackProgressInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            if (info.PositionTicks.HasValue && info.PositionTicks.Value < 0)
            {
                throw new ArgumentOutOfRangeException("positionTicks");
            }

            var session = Sessions.First(i => i.Id.Equals(info.SessionId));

            UpdateNowPlayingItem(session, info.Item, info.IsPaused, info.IsMuted, info.PositionTicks);

            var key = info.Item.GetUserDataKey();

            var user = session.User;

            if (info.PositionTicks.HasValue)
            {
                var data = _userDataRepository.GetUserData(user.Id, key);

                UpdatePlayState(info.Item, data, info.PositionTicks.Value);

                await _userDataRepository.SaveUserData(user.Id, key, data, UserDataSaveReason.PlaybackProgress, CancellationToken.None).ConfigureAwait(false);
            }

            EventHelper.QueueEventIfNotNull(PlaybackProgress, this, new PlaybackProgressEventArgs
            {
                Item = info.Item,
                User = user,
                PlaybackPositionTicks = info.PositionTicks

            }, _logger);
        }

        /// <summary>
        /// Used to report that playback has ended for an item
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">info</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">positionTicks</exception>
        public async Task OnPlaybackStopped(PlaybackStopInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            if (info.PositionTicks.HasValue && info.PositionTicks.Value < 0)
            {
                throw new ArgumentOutOfRangeException("positionTicks");
            }

            var session = Sessions.First(i => i.Id.Equals(info.SessionId));

            RemoveNowPlayingItem(session, info.Item);

            var key = info.Item.GetUserDataKey();

            var user = session.User;

            var data = _userDataRepository.GetUserData(user.Id, key);

            if (info.PositionTicks.HasValue)
            {
                UpdatePlayState(info.Item, data, info.PositionTicks.Value);
            }
            else
            {
                // If the client isn't able to report this, then we'll just have to make an assumption
                data.PlayCount++;
                data.Played = true;
                data.PlaybackPositionTicks = 0;
            }

            await _userDataRepository.SaveUserData(user.Id, key, data, UserDataSaveReason.PlaybackFinished, CancellationToken.None).ConfigureAwait(false);

            EventHelper.QueueEventIfNotNull(PlaybackStopped, this, new PlaybackProgressEventArgs
            {
                Item = info.Item,
                User = user,
                PlaybackPositionTicks = info.PositionTicks
            }, _logger);
        }

        /// <summary>
        /// Updates playstate position for an item but does not save
        /// </summary>
        /// <param name="item">The item</param>
        /// <param name="data">User data for the item</param>
        /// <param name="positionTicks">The current playback position</param>
        private void UpdatePlayState(BaseItem item, UserItemData data, long positionTicks)
        {
            var hasRuntime = item.RunTimeTicks.HasValue && item.RunTimeTicks > 0;

            // If a position has been reported, and if we know the duration
            if (positionTicks > 0 && hasRuntime)
            {
                var pctIn = Decimal.Divide(positionTicks, item.RunTimeTicks.Value) * 100;

                // Don't track in very beginning
                if (pctIn < _configurationManager.Configuration.MinResumePct)
                {
                    positionTicks = 0;
                }

                // If we're at the end, assume completed
                else if (pctIn > _configurationManager.Configuration.MaxResumePct || positionTicks >= item.RunTimeTicks.Value)
                {
                    positionTicks = 0;
                    data.Played = true;
                }

                else
                {
                    // Enforce MinResumeDuration
                    var durationSeconds = TimeSpan.FromTicks(item.RunTimeTicks.Value).TotalSeconds;

                    if (durationSeconds < _configurationManager.Configuration.MinResumeDurationSeconds)
                    {
                        positionTicks = 0;
                        data.Played = true;
                    }
                }
            }
            else if (!hasRuntime)
            {
                // If we don't know the runtime we'll just have to assume it was fully played
                data.Played = true;
                positionTicks = 0;
            }

            if (item is Audio)
            {
                positionTicks = 0;
            }

            data.PlaybackPositionTicks = positionTicks;
        }

        /// <summary>
        /// Gets the session for remote control.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <returns>SessionInfo.</returns>
        /// <exception cref="ResourceNotFoundException"></exception>
        private SessionInfo GetSessionForRemoteControl(Guid sessionId)
        {
            var session = Sessions.First(i => i.Id.Equals(sessionId));

            if (session == null)
            {
                throw new ResourceNotFoundException(string.Format("Session {0} not found.", sessionId));
            }

            if (!session.SupportsRemoteControl)
            {
                throw new ArgumentException(string.Format("Session {0} does not support remote control.", session.Id));
            }

            return session;
        }

        /// <summary>
        /// Sends the system command.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendSystemCommand(Guid sessionId, SystemCommand command, CancellationToken cancellationToken)
        {
            var session = GetSessionForRemoteControl(sessionId);

            return session.SessionController.SendSystemCommand(command, cancellationToken);
        }

        /// <summary>
        /// Sends the message command.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendMessageCommand(Guid sessionId, MessageCommand command, CancellationToken cancellationToken)
        {
            var session = GetSessionForRemoteControl(sessionId);

            return session.SessionController.SendMessageCommand(command, cancellationToken);
        }

        /// <summary>
        /// Sends the play command.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendPlayCommand(Guid sessionId, PlayRequest command, CancellationToken cancellationToken)
        {
            var session = GetSessionForRemoteControl(sessionId);

            return session.SessionController.SendPlayCommand(command, cancellationToken);
        }

        /// <summary>
        /// Sends the browse command.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendBrowseCommand(Guid sessionId, BrowseRequest command, CancellationToken cancellationToken)
        {
            var session = GetSessionForRemoteControl(sessionId);

            return session.SessionController.SendBrowseCommand(command, cancellationToken);
        }

        /// <summary>
        /// Sends the playstate command.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendPlaystateCommand(Guid sessionId, PlaystateRequest command, CancellationToken cancellationToken)
        {
            var session = GetSessionForRemoteControl(sessionId);

            return session.SessionController.SendPlaystateCommand(command, cancellationToken);
        }

        /// <summary>
        /// Sends the restart required message.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendRestartRequiredNotification(CancellationToken cancellationToken)
        {
            var sessions = Sessions.Where(i => i.IsActive && i.SessionController != null).ToList();

            var tasks = sessions.Select(session => Task.Run(async () =>
            {
                try
                {
                    await session.SessionController.SendRestartRequiredNotification(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in SendRestartRequiredNotification.", ex);
                }

            }));

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Sends the server shutdown notification.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendServerShutdownNotification(CancellationToken cancellationToken)
        {
            var sessions = Sessions.Where(i => i.IsActive && i.SessionController != null).ToList();

            var tasks = sessions.Select(session => Task.Run(async () =>
            {
                try
                {
                    await session.SessionController.SendServerShutdownNotification(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in SendServerShutdownNotification.", ex);
                }

            }));

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Sends the server restart notification.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendServerRestartNotification(CancellationToken cancellationToken)
        {
            var sessions = Sessions.Where(i => i.IsActive && i.SessionController != null).ToList();

            var tasks = sessions.Select(session => Task.Run(async () =>
            {
                try
                {
                    await session.SessionController.SendServerRestartNotification(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in SendServerRestartNotification.", ex);
                }

            }));

            return Task.WhenAll(tasks);
        }
    }
}
