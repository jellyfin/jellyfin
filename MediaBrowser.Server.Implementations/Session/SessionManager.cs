using MediaBrowser.Common.Events;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Library;
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

        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;

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
        public event EventHandler<PlaybackStopEventArgs> PlaybackStopped;

        private IEnumerable<ISessionControllerFactory> _sessionFactories = new List<ISessionControllerFactory>();

        private readonly SemaphoreSlim _sessionLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionManager" /> class.
        /// </summary>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="userRepository">The user repository.</param>
        /// <param name="libraryManager">The library manager.</param>
        public SessionManager(IUserDataManager userDataRepository, IServerConfigurationManager configurationManager, ILogger logger, IUserRepository userRepository, ILibraryManager libraryManager, IUserManager userManager)
        {
            _userDataRepository = userDataRepository;
            _configurationManager = configurationManager;
            _logger = logger;
            _userRepository = userRepository;
            _libraryManager = libraryManager;
            _userManager = userManager;
        }

        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="sessionFactories">The session factories.</param>
        public void AddParts(IEnumerable<ISessionControllerFactory> sessionFactories)
        {
            _sessionFactories = sessionFactories.ToList();
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
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <param name="user">The user.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        /// <exception cref="System.UnauthorizedAccessException"></exception>
        public async Task<SessionInfo> LogSessionActivity(string clientType, string appVersion, string deviceId, string deviceName, string remoteEndPoint, User user)
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

            var userId = user == null ? (Guid?)null : user.Id;
            var username = user == null ? null : user.Name;

            var session = await GetSessionInfo(clientType, appVersion, deviceId, deviceName, remoteEndPoint, userId, username).ConfigureAwait(false);

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

        public async Task ReportSessionEnded(Guid sessionId)
        {
            await _sessionLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                var session = GetSession(sessionId);

                if (session == null)
                {
                    throw new ArgumentException("Session not found");
                }

                var key = GetSessionKey(session.Client, session.ApplicationVersion, session.DeviceId);

                SessionInfo removed;

                if (_activeConnections.TryRemove(key, out removed))
                {
                    var disposable = removed.SessionController as IDisposable;

                    if (disposable != null)
                    {
                        try
                        {
                            disposable.Dispose();
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("Error disposing session controller", ex);
                        }
                    }
                }
            }
            finally
            {
                _sessionLock.Release();
            }
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
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (session.NowPlayingItem != null && session.NowPlayingItem.Id == item.Id)
            {
                session.NowPlayingItem = null;
                session.NowPlayingPositionTicks = null;
                session.IsPaused = false;
            }
        }

        private string GetSessionKey(string clientType, string appVersion, string deviceId)
        {
            return clientType + deviceId + appVersion;
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="appVersion">The app version.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="username">The username.</param>
        /// <returns>SessionInfo.</returns>
        private async Task<SessionInfo> GetSessionInfo(string clientType, string appVersion, string deviceId, string deviceName, string remoteEndPoint, Guid? userId, string username)
        {
            var key = GetSessionKey(clientType, appVersion, deviceId);

            await _sessionLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                var connection = _activeConnections.GetOrAdd(key, keyName => new SessionInfo
                {
                    Client = clientType,
                    DeviceId = deviceId,
                    ApplicationVersion = appVersion,
                    Id = Guid.NewGuid()
                });

                connection.DeviceName = deviceName;
                connection.UserId = userId;
                connection.UserName = username;
                connection.RemoteEndPoint = remoteEndPoint;

                if (!userId.HasValue)
                {
                    connection.AdditionalUsers.Clear();
                }

                if (connection.SessionController == null)
                {
                    connection.SessionController = _sessionFactories
                        .Select(i => i.GetSessionController(connection))
                        .FirstOrDefault(i => i != null);
                }

                return connection;
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        private List<User> GetUsers(SessionInfo session)
        {
            var users = new List<User>();

            if (session.UserId.HasValue)
            {
                var user = _userManager.GetUserById(session.UserId.Value);

                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                users.Add(user);

                var additionalUsers = session.AdditionalUsers
                    .Select(i => _userManager.GetUserById(new Guid(i.UserId)))
                    .Where(i => i != null);

                users.AddRange(additionalUsers);
            }

            return users;
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

            var users = GetUsers(session);

            foreach (var user in users)
            {
                await OnPlaybackStart(user.Id, key, item).ConfigureAwait(false);
            }

            // Nothing to save here
            // Fire events to inform plugins
            EventHelper.QueueEventIfNotNull(PlaybackStart, this, new PlaybackProgressEventArgs
            {
                Item = item,
                Users = users

            }, _logger);
        }

        /// <summary>
        /// Called when [playback start].
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="userDataKey">The user data key.</param>
        /// <param name="item">The item.</param>
        /// <returns>Task.</returns>
        private async Task OnPlaybackStart(Guid userId, string userDataKey, IHasUserData item)
        {
            var data = _userDataRepository.GetUserData(userId, userDataKey);

            data.PlayCount++;
            data.LastPlayedDate = DateTime.UtcNow;

            if (!(item is Video))
            {
                data.Played = true;
            }

            await _userDataRepository.SaveUserData(userId, item, data, UserDataSaveReason.PlaybackStart, CancellationToken.None).ConfigureAwait(false);
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

            var users = GetUsers(session);

            foreach (var user in users)
            {
                await OnPlaybackProgress(user.Id, key, info.Item, info.PositionTicks).ConfigureAwait(false);
            }

            EventHelper.QueueEventIfNotNull(PlaybackProgress, this, new PlaybackProgressEventArgs
            {
                Item = info.Item,
                Users = users,
                PlaybackPositionTicks = info.PositionTicks

            }, _logger);
        }

        private async Task OnPlaybackProgress(Guid userId, string userDataKey, BaseItem item, long? positionTicks)
        {
            var data = _userDataRepository.GetUserData(userId, userDataKey);

            if (positionTicks.HasValue)
            {
                UpdatePlayState(item, data, positionTicks.Value);

                await _userDataRepository.SaveUserData(userId, item, data, UserDataSaveReason.PlaybackProgress, CancellationToken.None).ConfigureAwait(false);
            }
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

            if (info.Item == null)
            {
                throw new ArgumentException("PlaybackStopInfo.Item cannot be null");
            }

            if (info.SessionId == Guid.Empty)
            {
                throw new ArgumentException("PlaybackStopInfo.SessionId cannot be Guid.Empty");
            }

            if (info.PositionTicks.HasValue && info.PositionTicks.Value < 0)
            {
                throw new ArgumentOutOfRangeException("positionTicks");
            }

            var session = Sessions.First(i => i.Id.Equals(info.SessionId));

            RemoveNowPlayingItem(session, info.Item);

            var key = info.Item.GetUserDataKey();

            var users = GetUsers(session);

            var playedToCompletion = false;
            foreach (var user in users)
            {
                playedToCompletion = await OnPlaybackStopped(user.Id, key, info.Item, info.PositionTicks).ConfigureAwait(false);
            }

            EventHelper.QueueEventIfNotNull(PlaybackStopped, this, new PlaybackStopEventArgs
            {
                Item = info.Item,
                Users = users,
                PlaybackPositionTicks = info.PositionTicks,
                PlayedToCompletion = playedToCompletion

            }, _logger);
        }

        private async Task<bool> OnPlaybackStopped(Guid userId, string userDataKey, BaseItem item, long? positionTicks)
        {
            var data = _userDataRepository.GetUserData(userId, userDataKey);
            bool playedToCompletion;

            if (positionTicks.HasValue)
            {
                playedToCompletion = UpdatePlayState(item, data, positionTicks.Value);
            }
            else
            {
                // If the client isn't able to report this, then we'll just have to make an assumption
                data.PlayCount++;
                data.Played = true;
                data.PlaybackPositionTicks = 0;
                playedToCompletion = true;
            }

            await _userDataRepository.SaveUserData(userId, item, data, UserDataSaveReason.PlaybackFinished, CancellationToken.None).ConfigureAwait(false);

            return playedToCompletion;
        }
        
        /// <summary>
        /// Updates playstate position for an item but does not save
        /// </summary>
        /// <param name="item">The item</param>
        /// <param name="data">User data for the item</param>
        /// <param name="positionTicks">The current playback position</param>
        private bool UpdatePlayState(BaseItem item, UserItemData data, long positionTicks)
        {
            var playedToCompletion = false;

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
                    data.Played = playedToCompletion = true;
                }

                else
                {
                    // Enforce MinResumeDuration
                    var durationSeconds = TimeSpan.FromTicks(item.RunTimeTicks.Value).TotalSeconds;

                    if (durationSeconds < _configurationManager.Configuration.MinResumeDurationSeconds)
                    {
                        positionTicks = 0;
                        data.Played = playedToCompletion = true;
                    }
                }
            }
            else if (!hasRuntime)
            {
                // If we don't know the runtime we'll just have to assume it was fully played
                data.Played = playedToCompletion = true;
                positionTicks = 0;
            }

            if (item is Audio)
            {
                positionTicks = 0;
            }

            data.PlaybackPositionTicks = positionTicks;

            return playedToCompletion;
        }

        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>SessionInfo.</returns>
        /// <exception cref="ResourceNotFoundException"></exception>
        private SessionInfo GetSession(Guid sessionId)
        {
            var session = Sessions.First(i => i.Id.Equals(sessionId));

            if (session == null)
            {
                throw new ResourceNotFoundException(string.Format("Session {0} not found.", sessionId));
            }

            return session;
        }

        /// <summary>
        /// Gets the session for remote control.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <returns>SessionInfo.</returns>
        /// <exception cref="ResourceNotFoundException"></exception>
        private SessionInfo GetSessionForRemoteControl(Guid sessionId)
        {
            var session = GetSession(sessionId);

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

            var items = command.ItemIds.Select(i => _libraryManager.GetItemById(new Guid(i)))
                .Where(i => i.LocationType != LocationType.Virtual)
                .ToList();

            if (session.UserId.HasValue)
            {
                var user = _userManager.GetUserById(session.UserId.Value);

                if (items.Any(i => i.GetPlayAccess(user) != PlayAccess.Full))
                {
                    throw new ArgumentException(string.Format("{0} is not allowed to play media.", user.Name));
                }
            }

            if (command.PlayCommand != PlayCommand.PlayNow)
            {
                if (items.Any(i => !session.QueueableMediaTypes.Contains(i.MediaType, StringComparer.OrdinalIgnoreCase)))
                {
                    throw new ArgumentException(string.Format("{0} is unable to queue the requested media type.", session.DeviceName ?? session.Id.ToString()));
                }
            }
            else
            {
                if (items.Any(i => !session.PlayableMediaTypes.Contains(i.MediaType, StringComparer.OrdinalIgnoreCase)))
                {
                    throw new ArgumentException(string.Format("{0} is unable to play the requested media type.", session.DeviceName ?? session.Id.ToString()));
                }
            }

            if (session.UserId.HasValue)
            {
                command.ControllingUserId = session.UserId.Value.ToString("N");
            }

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

            if (command.Command == PlaystateCommand.Seek && !session.CanSeek)
            {
                throw new ArgumentException(string.Format("Session {0} is unable to seek.", session.Id));
            }

            if (session.UserId.HasValue)
            {
                command.ControllingUserId = session.UserId.Value.ToString("N");
            }

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

            }, cancellationToken));

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

            }, cancellationToken));

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

            }, cancellationToken));

            return Task.WhenAll(tasks);
        }


        /// <summary>
        /// Adds the additional user.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <exception cref="System.UnauthorizedAccessException">Cannot modify additional users without authenticating first.</exception>
        /// <exception cref="System.ArgumentException">The requested user is already the primary user of the session.</exception>
        public void AddAdditionalUser(Guid sessionId, Guid userId)
        {
            var session = GetSession(sessionId);

            if (!session.UserId.HasValue)
            {
                throw new UnauthorizedAccessException("Cannot modify additional users without authenticating first.");
            }

            if (session.UserId.Value == userId)
            {
                throw new ArgumentException("The requested user is already the primary user of the session.");
            }

            if (session.AdditionalUsers.All(i => new Guid(i.UserId) != userId))
            {
                var user = _userManager.GetUserById(userId);

                session.AdditionalUsers.Add(new SessionUserInfo
                {
                    UserId = userId.ToString("N"),
                    UserName = user.Name
                });
            }
        }

        /// <summary>
        /// Removes the additional user.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <exception cref="System.UnauthorizedAccessException">Cannot modify additional users without authenticating first.</exception>
        /// <exception cref="System.ArgumentException">The requested user is already the primary user of the session.</exception>
        public void RemoveAdditionalUser(Guid sessionId, Guid userId)
        {
            var session = GetSession(sessionId);

            if (!session.UserId.HasValue)
            {
                throw new UnauthorizedAccessException("Cannot modify additional users without authenticating first.");
            }

            if (session.UserId.Value == userId)
            {
                throw new ArgumentException("The requested user is already the primary user of the session.");
            }

            var user = session.AdditionalUsers.FirstOrDefault(i => new Guid(i.UserId) == userId);

            if (user != null)
            {
                session.AdditionalUsers.Remove(user);
            }
        }

        /// <summary>
        /// Authenticates the new session.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="appVersion">The application version.</param>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <returns>Task{SessionInfo}.</returns>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public async Task<SessionInfo> AuthenticateNewSession(User user, string password, string clientType, string appVersion, string deviceId, string deviceName, string remoteEndPoint)
        {
            var result = await _userManager.AuthenticateUser(user, password).ConfigureAwait(false);

            if (!result)
            {
                throw new UnauthorizedAccessException("Invalid user or password entered.");
            }

            return await LogSessionActivity(clientType, appVersion, deviceId, deviceName, remoteEndPoint, user).ConfigureAwait(false);
        }
    }
}