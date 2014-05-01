using MediaBrowser.Common.Events;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
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
        private readonly IMusicManager _musicManager;
        private readonly IDtoService _dtoService;
        private readonly IImageProcessor _imageProcessor;

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

        public event EventHandler<SessionEventArgs> SessionStarted;

        public event EventHandler<SessionEventArgs> SessionEnded;

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
        public SessionManager(IUserDataManager userDataRepository, IServerConfigurationManager configurationManager, ILogger logger, IUserRepository userRepository, ILibraryManager libraryManager, IUserManager userManager, IMusicManager musicManager, IDtoService dtoService, IImageProcessor imageProcessor)
        {
            _userDataRepository = userDataRepository;
            _configurationManager = configurationManager;
            _logger = logger;
            _userRepository = userRepository;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _musicManager = musicManager;
            _dtoService = dtoService;
            _imageProcessor = imageProcessor;
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

        private void OnSessionStarted(SessionInfo info)
        {
            EventHelper.QueueEventIfNotNull(SessionStarted, this, new SessionEventArgs
            {
                SessionInfo = info

            }, _logger);
        }

        private async void OnSessionEnded(SessionInfo info)
        {
            try
            {
                await SendSessionEndedNotification(info, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error in SendSessionEndedNotification", ex);
            }

            EventHelper.QueueEventIfNotNull(SessionEnded, this, new SessionEventArgs
            {
                SessionInfo = info

            }, _logger);

            var disposable = info.SessionController as IDisposable;

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

        public async Task ReportSessionEnded(string sessionId)
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
                    OnSessionEnded(removed);
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
        /// <param name="info">The information.</param>
        /// <param name="libraryItem">The library item.</param>
        private void UpdateNowPlayingItem(SessionInfo session, PlaybackProgressInfo info, BaseItem libraryItem)
        {
            var runtimeTicks = libraryItem == null ? null : libraryItem.RunTimeTicks;

            if (string.IsNullOrWhiteSpace(info.MediaSourceId))
            {
                info.MediaSourceId = info.ItemId;
            }

            if (!string.Equals(info.ItemId, info.MediaSourceId) &&
                !string.IsNullOrWhiteSpace(info.MediaSourceId))
            {
                runtimeTicks = _libraryManager.GetItemById(new Guid(info.MediaSourceId)).RunTimeTicks;
            }

            if (!string.IsNullOrWhiteSpace(info.ItemId) && libraryItem != null)
            {
                info.Item = GetItemInfo(libraryItem, runtimeTicks);
            }

            session.NowPlayingItem = info.Item;
            session.LastActivityDate = DateTime.UtcNow;

            session.PlayState.IsPaused = info.IsPaused;
            session.PlayState.PositionTicks = info.PositionTicks;
            session.PlayState.MediaSourceId = info.MediaSourceId;
            session.PlayState.CanSeek = info.CanSeek;
            session.PlayState.IsMuted = info.IsMuted;
            session.PlayState.VolumeLevel = info.VolumeLevel;
            session.PlayState.AudioStreamIndex = info.AudioStreamIndex;
            session.PlayState.SubtitleStreamIndex = info.SubtitleStreamIndex;
            session.PlayState.PlayMethod = info.PlayMethod;
        }

        /// <summary>
        /// Removes the now playing item id.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <exception cref="System.ArgumentNullException">item</exception>
        private void RemoveNowPlayingItem(SessionInfo session)
        {
            session.NowPlayingItem = null;
            session.PlayState = new PlayerStateInfo();
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
                var connection = _activeConnections.GetOrAdd(key, keyName =>
                {
                    var sessionInfo = new SessionInfo
                    {
                        Client = clientType,
                        DeviceId = deviceId,
                        ApplicationVersion = appVersion,
                        Id = Guid.NewGuid().ToString("N")
                    };

                    OnSessionStarted(sessionInfo);

                    return sessionInfo;
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
        public async Task OnPlaybackStart(PlaybackStartInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            var session = GetSession(info.SessionId);

            var libraryItem = string.IsNullOrWhiteSpace(info.ItemId)
                ? null
                : _libraryManager.GetItemById(new Guid(info.ItemId));

            UpdateNowPlayingItem(session, info, libraryItem);

            session.QueueableMediaTypes = info.QueueableMediaTypes;

            var users = GetUsers(session);

            if (libraryItem != null)
            {
                var key = libraryItem.GetUserDataKey();

                foreach (var user in users)
                {
                    await OnPlaybackStart(user.Id, key, libraryItem).ConfigureAwait(false);
                }
            }

            // Nothing to save here
            // Fire events to inform plugins
            EventHelper.QueueEventIfNotNull(PlaybackStart, this, new PlaybackProgressEventArgs
            {
                Item = libraryItem,
                Users = users,
                MediaSourceId = info.MediaSourceId,
                MediaInfo = info.Item,
                DeviceName = session.DeviceName,
                ClientName = session.Client

            }, _logger);

            await SendPlaybackStartNotification(session, CancellationToken.None).ConfigureAwait(false);
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

            var session = GetSession(info.SessionId);

            var libraryItem = string.IsNullOrWhiteSpace(info.ItemId)
                ? null
                : _libraryManager.GetItemById(new Guid(info.ItemId));

            UpdateNowPlayingItem(session, info, libraryItem);

            var users = GetUsers(session);

            if (libraryItem != null)
            {
                var key = libraryItem.GetUserDataKey();

                foreach (var user in users)
                {
                    await OnPlaybackProgress(user.Id, key, libraryItem, info.PositionTicks).ConfigureAwait(false);
                }
            }

            EventHelper.QueueEventIfNotNull(PlaybackProgress, this, new PlaybackProgressEventArgs
            {
                Item = libraryItem,
                Users = users,
                PlaybackPositionTicks = session.PlayState.PositionTicks,
                MediaSourceId = session.PlayState.MediaSourceId,
                MediaInfo = info.Item,
                DeviceName = session.DeviceName,
                ClientName = session.Client

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

            if (info.PositionTicks.HasValue && info.PositionTicks.Value < 0)
            {
                throw new ArgumentOutOfRangeException("positionTicks");
            }

            var session = GetSession(info.SessionId);

            var libraryItem = string.IsNullOrWhiteSpace(info.ItemId)
                ? null
                : _libraryManager.GetItemById(new Guid(info.ItemId));

            // Normalize
            if (string.IsNullOrWhiteSpace(info.MediaSourceId))
            {
                info.MediaSourceId = info.ItemId;
            }

            RemoveNowPlayingItem(session);

            var users = GetUsers(session);
            var playedToCompletion = false;

            if (libraryItem != null)
            {
                var key = libraryItem.GetUserDataKey();

                foreach (var user in users)
                {
                    playedToCompletion = await OnPlaybackStopped(user.Id, key, libraryItem, info.PositionTicks).ConfigureAwait(false);
                }
            }

            EventHelper.QueueEventIfNotNull(PlaybackStopped, this, new PlaybackStopEventArgs
            {
                Item = libraryItem,
                Users = users,
                PlaybackPositionTicks = info.PositionTicks,
                PlayedToCompletion = playedToCompletion,
                MediaSourceId = info.MediaSourceId,
                MediaInfo = info.Item,
                DeviceName = session.DeviceName,
                ClientName = session.Client

            }, _logger);

            await SendPlaybackStoppedNotification(session, CancellationToken.None).ConfigureAwait(false);
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
        private SessionInfo GetSession(string sessionId)
        {
            var session = Sessions.First(i => string.Equals(i.Id, sessionId));

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
        private SessionInfo GetSessionForRemoteControl(string sessionId)
        {
            var session = GetSession(sessionId);

            if (!session.SupportsRemoteControl)
            {
                throw new ArgumentException(string.Format("Session {0} does not support remote control.", session.Id));
            }

            return session;
        }

        public Task SendMessageCommand(string controllingSessionId, string sessionId, MessageCommand command, CancellationToken cancellationToken)
        {
            var session = GetSessionForRemoteControl(sessionId);

            var controllingSession = GetSession(controllingSessionId);
            AssertCanControl(session, controllingSession);

            return session.SessionController.SendMessageCommand(command, cancellationToken);
        }

        public Task SendGeneralCommand(string controllingSessionId, string sessionId, GeneralCommand command, CancellationToken cancellationToken)
        {
            var session = GetSessionForRemoteControl(sessionId);

            var controllingSession = GetSession(controllingSessionId);
            AssertCanControl(session, controllingSession);

            return session.SessionController.SendGeneralCommand(command, cancellationToken);
        }

        public Task SendPlayCommand(string controllingSessionId, string sessionId, PlayRequest command, CancellationToken cancellationToken)
        {
            var session = GetSessionForRemoteControl(sessionId);

            var user = session.UserId.HasValue ? _userManager.GetUserById(session.UserId.Value) : null;

            List<BaseItem> items;

            if (command.PlayCommand == PlayCommand.PlayInstantMix)
            {
                items = command.ItemIds.SelectMany(i => TranslateItemForInstantMix(i, user))
                    .Where(i => i.LocationType != LocationType.Virtual)
                    .ToList();

                command.PlayCommand = PlayCommand.PlayNow;
            }
            else
            {
                items = command.ItemIds.SelectMany(i => TranslateItemForPlayback(i, user))
                   .Where(i => i.LocationType != LocationType.Virtual)
                   .ToList();
            }

            if (command.PlayCommand == PlayCommand.PlayShuffle)
            {
                items = items.OrderBy(i => Guid.NewGuid()).ToList();
                command.PlayCommand = PlayCommand.PlayNow;
            }

            command.ItemIds = items.Select(i => i.Id.ToString("N")).ToArray();

            if (user != null)
            {
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

            var controllingSession = GetSession(controllingSessionId);
            AssertCanControl(session, controllingSession);
            if (controllingSession.UserId.HasValue)
            {
                command.ControllingUserId = controllingSession.UserId.Value.ToString("N");
            }

            return session.SessionController.SendPlayCommand(command, cancellationToken);
        }

        private IEnumerable<BaseItem> TranslateItemForPlayback(string id, User user)
        {
            var item = _libraryManager.GetItemById(new Guid(id));

            if (item.IsFolder)
            {
                var folder = (Folder)item;

                var items = user == null ? folder.RecursiveChildren :
                    folder.GetRecursiveChildren(user);

                items = items.Where(i => !i.IsFolder);

                items = items.OrderBy(i => i.SortName);

                return items;
            }

            return new[] { item };
        }

        private IEnumerable<BaseItem> TranslateItemForInstantMix(string id, User user)
        {
            var item = _libraryManager.GetItemById(new Guid(id));

            var audio = item as Audio;

            if (audio != null)
            {
                return _musicManager.GetInstantMixFromSong(audio, user);
            }

            var artist = item as MusicArtist;

            if (artist != null)
            {
                return _musicManager.GetInstantMixFromArtist(artist.Name, user);
            }

            var album = item as MusicAlbum;

            if (album != null)
            {
                return _musicManager.GetInstantMixFromAlbum(album, user);
            }

            var genre = item as MusicGenre;

            if (genre != null)
            {
                return _musicManager.GetInstantMixFromGenres(new[] { genre.Name }, user);
            }

            return new BaseItem[] { };
        }

        public Task SendBrowseCommand(string controllingSessionId, string sessionId, BrowseRequest command, CancellationToken cancellationToken)
        {
            var generalCommand = new GeneralCommand
            {
                Name = GeneralCommandType.DisplayContent.ToString()
            };

            generalCommand.Arguments["ItemId"] = command.ItemId;
            generalCommand.Arguments["ItemName"] = command.ItemName;
            generalCommand.Arguments["ItemType"] = command.ItemType;

            return SendGeneralCommand(controllingSessionId, sessionId, generalCommand, cancellationToken);
        }

        public Task SendPlaystateCommand(string controllingSessionId, string sessionId, PlaystateRequest command, CancellationToken cancellationToken)
        {
            var session = GetSessionForRemoteControl(sessionId);

            var controllingSession = GetSession(controllingSessionId);
            AssertCanControl(session, controllingSession);
            if (controllingSession.UserId.HasValue)
            {
                command.ControllingUserId = controllingSession.UserId.Value.ToString("N");
            }

            return session.SessionController.SendPlaystateCommand(command, cancellationToken);
        }

        private void AssertCanControl(SessionInfo session, SessionInfo controllingSession)
        {
            if (session == null)
            {
                throw new ArgumentNullException("session");
            }
            if (controllingSession == null)
            {
                throw new ArgumentNullException("controllingSession");
            }
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

        public Task SendSessionEndedNotification(SessionInfo sessionInfo, CancellationToken cancellationToken)
        {
            var sessions = Sessions.Where(i => i.IsActive && i.SessionController != null).ToList();
            var dto = GetSessionInfoDto(sessionInfo);

            var tasks = sessions.Select(session => Task.Run(async () =>
            {
                try
                {
                    await session.SessionController.SendSessionEndedNotification(dto, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in SendSessionEndedNotification.", ex);
                }

            }, cancellationToken));

            return Task.WhenAll(tasks);
        }

        public Task SendPlaybackStartNotification(SessionInfo sessionInfo, CancellationToken cancellationToken)
        {
            var sessions = Sessions.Where(i => i.IsActive && i.SessionController != null).ToList();
            var dto = GetSessionInfoDto(sessionInfo);

            var tasks = sessions.Select(session => Task.Run(async () =>
            {
                try
                {
                    await session.SessionController.SendPlaybackStartNotification(dto, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in SendPlaybackStartNotification.", ex);
                }

            }, cancellationToken));

            return Task.WhenAll(tasks);
        }

        public Task SendPlaybackStoppedNotification(SessionInfo sessionInfo, CancellationToken cancellationToken)
        {
            var sessions = Sessions.Where(i => i.IsActive && i.SessionController != null).ToList();
            var dto = GetSessionInfoDto(sessionInfo);

            var tasks = sessions.Select(session => Task.Run(async () =>
            {
                try
                {
                    await session.SessionController.SendPlaybackStoppedNotification(dto, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in SendPlaybackStoppedNotification.", ex);
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
        public void AddAdditionalUser(string sessionId, Guid userId)
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
        public void RemoveAdditionalUser(string sessionId, Guid userId)
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

        /// <summary>
        /// Reports the capabilities.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="capabilities">The capabilities.</param>
        public void ReportCapabilities(string sessionId, SessionCapabilities capabilities)
        {
            var session = GetSession(sessionId);

            session.PlayableMediaTypes = capabilities.PlayableMediaTypes;
            session.SupportedCommands = capabilities.SupportedCommands;
        }

        public SessionInfoDto GetSessionInfoDto(SessionInfo session)
        {
            var dto = new SessionInfoDto
            {
                Client = session.Client,
                DeviceId = session.DeviceId,
                DeviceName = session.DeviceName,
                Id = session.Id,
                LastActivityDate = session.LastActivityDate,
                NowPlayingPositionTicks = session.PlayState.PositionTicks,
                SupportsRemoteControl = session.SupportsRemoteControl,
                IsPaused = session.PlayState.IsPaused,
                IsMuted = session.PlayState.IsMuted,
                NowViewingItem = session.NowViewingItem,
                ApplicationVersion = session.ApplicationVersion,
                CanSeek = session.PlayState.CanSeek,
                QueueableMediaTypes = session.QueueableMediaTypes,
                PlayableMediaTypes = session.PlayableMediaTypes,
                RemoteEndPoint = session.RemoteEndPoint,
                AdditionalUsers = session.AdditionalUsers,
                SupportedCommands = session.SupportedCommands,
                UserName = session.UserName,
                NowPlayingItem = session.NowPlayingItem,

                PlayState = session.PlayState
            };

            if (session.UserId.HasValue)
            {
                dto.UserId = session.UserId.Value.ToString("N");

                var user = _userManager.GetUserById(session.UserId.Value);

                if (user != null)
                {
                    dto.UserPrimaryImageTag = GetImageCacheTag(user, ImageType.Primary);
                }
            }

            return dto;
        }

        /// <summary>
        /// Converts a BaseItem to a BaseItemInfo
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="runtimeTicks">The now playing runtime ticks.</param>
        /// <returns>BaseItemInfo.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        private BaseItemInfo GetItemInfo(BaseItem item, long? runtimeTicks)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var info = new BaseItemInfo
            {
                Id = GetDtoId(item),
                Name = item.Name,
                MediaType = item.MediaType,
                Type = item.GetClientTypeName(),
                RunTimeTicks = runtimeTicks,
                IndexNumber = item.IndexNumber,
                ParentIndexNumber = item.ParentIndexNumber,
                PremiereDate = item.PremiereDate,
                ProductionYear = item.ProductionYear
            };

            info.PrimaryImageTag = GetImageCacheTag(item, ImageType.Primary);
            if (info.PrimaryImageTag.HasValue)
            {
                info.PrimaryImageItemId = GetDtoId(item);
            }

            var episode = item as Episode;
            if (episode != null)
            {
                info.IndexNumberEnd = episode.IndexNumberEnd;
            }

            var hasSeries = item as IHasSeries;
            if (hasSeries != null)
            {
                info.SeriesName = hasSeries.SeriesName;
            }

            var recording = item as ILiveTvRecording;
            if (recording != null && recording.RecordingInfo != null)
            {
                if (recording.RecordingInfo.IsSeries)
                {
                    info.Name = recording.RecordingInfo.EpisodeTitle;
                    info.SeriesName = recording.RecordingInfo.Name;

                    if (string.IsNullOrWhiteSpace(info.Name))
                    {
                        info.Name = recording.RecordingInfo.Name;
                    }
                }
            }

            var audio = item as Audio;
            if (audio != null)
            {
                info.Album = audio.Album;
                info.Artists = audio.Artists;

                if (!info.PrimaryImageTag.HasValue)
                {
                    var album = audio.Parents.OfType<MusicAlbum>().FirstOrDefault();

                    if (album != null && album.HasImage(ImageType.Primary))
                    {
                        info.PrimaryImageTag = GetImageCacheTag(album, ImageType.Primary);
                        if (info.PrimaryImageTag.HasValue)
                        {
                            info.PrimaryImageItemId = GetDtoId(album);
                        }
                    }
                }
            }

            var musicVideo = item as MusicVideo;
            if (musicVideo != null)
            {
                info.Album = musicVideo.Album;

                if (!string.IsNullOrWhiteSpace(musicVideo.Artist))
                {
                    info.Artists.Add(musicVideo.Artist);
                }
            }

            var backropItem = item.HasImage(ImageType.Backdrop) ? item : null;
            var thumbItem = item.HasImage(ImageType.Thumb) ? item : null;
            var logoItem = item.HasImage(ImageType.Logo) ? item : null;

            if (thumbItem == null)
            {
                if (episode != null)
                {
                    var series = episode.Series;

                    if (series != null && series.HasImage(ImageType.Thumb))
                    {
                        thumbItem = series;
                    }
                }
            }

            if (backropItem == null)
            {
                if (episode != null)
                {
                    var series = episode.Series;

                    if (series != null && series.HasImage(ImageType.Backdrop))
                    {
                        backropItem = series;
                    }
                }
            }

            if (backropItem == null)
            {
                backropItem = item.Parents.FirstOrDefault(i => i.HasImage(ImageType.Backdrop));
            }

            if (thumbItem == null)
            {
                thumbItem = item.Parents.FirstOrDefault(i => i.HasImage(ImageType.Thumb));
            }

            if (logoItem == null)
            {
                logoItem = item.Parents.FirstOrDefault(i => i.HasImage(ImageType.Logo));
            }

            if (thumbItem != null)
            {
                info.ThumbImageTag = GetImageCacheTag(thumbItem, ImageType.Thumb);
                info.ThumbItemId = GetDtoId(thumbItem);
            }

            if (backropItem != null)
            {
                info.BackdropImageTag = GetImageCacheTag(backropItem, ImageType.Backdrop);
                info.BackdropItemId = GetDtoId(backropItem);
            }

            if (logoItem != null)
            {
                info.LogoImageTag = GetImageCacheTag(logoItem, ImageType.Logo);
                info.LogoItemId = GetDtoId(logoItem);
            }

            return info;
        }

        private Guid? GetImageCacheTag(BaseItem item, ImageType type)
        {
            try
            {
                return _imageProcessor.GetImageCacheTag(item, type);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting {0} image info", ex, type);
                return null;
            }
        }

        private string GetDtoId(BaseItem item)
        {
            return _dtoService.GetDtoId(item);
        }

        public void ReportNowViewingItem(string sessionId, string itemId)
        {
            var item = _libraryManager.GetItemById(new Guid(itemId));

            var info = GetItemInfo(item, item.RunTimeTicks);

            ReportNowViewingItem(sessionId, info);
        }

        public void ReportNowViewingItem(string sessionId, BaseItemInfo item)
        {
            var session = GetSession(sessionId);

            session.NowViewingItem = item;
        }
    }
}