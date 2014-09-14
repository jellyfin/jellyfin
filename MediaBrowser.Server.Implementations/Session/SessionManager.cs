using MediaBrowser.Common.Events;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Users;
using MediaBrowser.Server.Implementations.Security;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        private readonly IItemRepository _itemRepo;

        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IServerApplicationHost _appHost;

        private readonly IAuthenticationRepository _authRepo;

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

        public event EventHandler<GenericEventArgs<AuthenticationRequest>> AuthenticationFailed;

        public event EventHandler<GenericEventArgs<AuthenticationRequest>> AuthenticationSucceeded;
        
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
        public event EventHandler<SessionEventArgs> CapabilitiesChanged;
        public event EventHandler<SessionEventArgs> SessionEnded;
        public event EventHandler<SessionEventArgs> SessionActivity;

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
        public SessionManager(IUserDataManager userDataRepository, IServerConfigurationManager configurationManager, ILogger logger, IUserRepository userRepository, ILibraryManager libraryManager, IUserManager userManager, IMusicManager musicManager, IDtoService dtoService, IImageProcessor imageProcessor, IItemRepository itemRepo, IJsonSerializer jsonSerializer, IServerApplicationHost appHost, IHttpClient httpClient, IAuthenticationRepository authRepo)
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
            _itemRepo = itemRepo;
            _jsonSerializer = jsonSerializer;
            _appHost = appHost;
            _httpClient = httpClient;
            _authRepo = authRepo;
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

            if (!string.IsNullOrWhiteSpace(info.DeviceId))
            {
                var capabilities = GetSavedCapabilities(info.DeviceId);

                if (capabilities != null)
                {
                    ReportCapabilities(info, capabilities, false);
                }
            }
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
                _logger.Debug("Disposing session controller {0}", disposable.GetType().Name);

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
        public async Task<SessionInfo> LogSessionActivity(string clientType,
            string appVersion,
            string deviceId,
            string deviceName,
            string remoteEndPoint,
            User user)
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
                throw new AuthenticationException(string.Format("The {0} account is currently disabled. Please consult with your administrator.", user.Name));
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

            EventHelper.FireEventIfNotNull(SessionActivity, this, new SessionEventArgs
            {
                SessionInfo = session

            }, _logger);

            return session;
        }

        public async void ReportSessionEnded(string sessionId)
        {
            await _sessionLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                var session = GetSession(sessionId, false);

                if (session != null)
                {
                    var key = GetSessionKey(session.Client, session.ApplicationVersion, session.DeviceId);

                    SessionInfo removed;

                    if (_activeConnections.TryRemove(key, out removed))
                    {
                        OnSessionEnded(removed);
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
                var current = session.NowPlayingItem;

                if (current == null || !string.Equals(current.Id, info.ItemId, StringComparison.OrdinalIgnoreCase))
                {
                    info.Item = GetItemInfo(libraryItem, libraryItem, info.MediaSourceId);
                }
                else
                {
                    info.Item = current;
                }

                info.Item.RunTimeTicks = runtimeTicks;
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

            if (!string.IsNullOrEmpty(session.DeviceId))
            {
                ClearTranscodingInfo(session.DeviceId);
            }
        }

        private string GetSessionKey(string clientType, string appVersion, string deviceId)
        {
            return clientType + deviceId;
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

                    sessionInfo.DeviceName = deviceName;
                    sessionInfo.UserId = userId;
                    sessionInfo.UserName = username;
                    sessionInfo.RemoteEndPoint = remoteEndPoint;

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
                    .Select(i => _userManager.GetUserById(i.UserId))
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

            if (!string.IsNullOrEmpty(session.DeviceId) && info.PlayMethod != PlayMethod.Transcode)
            {
                ClearTranscodingInfo(session.DeviceId);
            }

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
                ClientName = session.Client,
                DeviceId = session.DeviceId

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

            EventHelper.FireEventIfNotNull(PlaybackProgress, this, new PlaybackProgressEventArgs
            {
                Item = libraryItem,
                Users = users,
                PlaybackPositionTicks = session.PlayState.PositionTicks,
                MediaSourceId = session.PlayState.MediaSourceId,
                MediaInfo = info.Item,
                DeviceName = session.DeviceName,
                ClientName = session.Client,
                DeviceId = session.DeviceId

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

            if (!string.IsNullOrWhiteSpace(info.ItemId) && libraryItem != null)
            {
                var current = session.NowPlayingItem;

                if (current == null || !string.Equals(current.Id, info.ItemId, StringComparison.OrdinalIgnoreCase))
                {
                    info.Item = GetItemInfo(libraryItem, libraryItem, info.MediaSourceId);
                }
                else
                {
                    info.Item = current;
                }
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
                ClientName = session.Client,
                DeviceId = session.DeviceId

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
        /// <param name="throwOnMissing">if set to <c>true</c> [throw on missing].</param>
        /// <returns>SessionInfo.</returns>
        /// <exception cref="ResourceNotFoundException"></exception>
        private SessionInfo GetSession(string sessionId, bool throwOnMissing = true)
        {
            var session = Sessions.FirstOrDefault(i => string.Equals(i.Id, sessionId));

            if (session == null && throwOnMissing)
            {
                throw new ResourceNotFoundException(string.Format("Session {0} not found.", sessionId));
            }

            return session;
        }

        public Task SendMessageCommand(string controllingSessionId, string sessionId, MessageCommand command, CancellationToken cancellationToken)
        {
            var generalCommand = new GeneralCommand
            {
                Name = GeneralCommandType.DisplayMessage.ToString()
            };

            generalCommand.Arguments["Header"] = command.Header;
            generalCommand.Arguments["Text"] = command.Text;

            if (command.TimeoutMs.HasValue)
            {
                generalCommand.Arguments["TimeoutMs"] = command.TimeoutMs.Value.ToString(CultureInfo.InvariantCulture);
            }

            return SendGeneralCommand(controllingSessionId, sessionId, generalCommand, cancellationToken);
        }

        public Task SendGeneralCommand(string controllingSessionId, string sessionId, GeneralCommand command, CancellationToken cancellationToken)
        {
            var session = GetSession(sessionId);

            var controllingSession = GetSession(controllingSessionId);
            AssertCanControl(session, controllingSession);

            return session.SessionController.SendGeneralCommand(command, cancellationToken);
        }

        public Task SendPlayCommand(string controllingSessionId, string sessionId, PlayRequest command, CancellationToken cancellationToken)
        {
            var session = GetSession(sessionId);

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
            var session = GetSession(sessionId);

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

            var info = _appHost.GetSystemInfo();

            var tasks = sessions.Select(session => Task.Run(async () =>
            {
                try
                {
                    await session.SessionController.SendRestartRequiredNotification(info, cancellationToken).ConfigureAwait(false);
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
            _logger.Debug("Beginning SendServerRestartNotification");

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

            if (session.UserId.HasValue && session.UserId.Value == userId)
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

            if (session.UserId.HasValue && session.UserId.Value == userId)
            {
                throw new ArgumentException("The requested user is already the primary user of the session.");
            }

            var user = session.AdditionalUsers.FirstOrDefault(i => new Guid(i.UserId) == userId);

            if (user != null)
            {
                session.AdditionalUsers.Remove(user);
            }
        }

        public void ValidateSecurityToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new AuthenticationException();
            }

            var result = _authRepo.Get(new AuthenticationInfoQuery
            {
                AccessToken = token
            });

            var info = result.Items.FirstOrDefault();

            if (info == null)
            {
                throw new AuthenticationException();
            }

            if (!info.IsActive)
            {
                throw new AuthenticationException("Access token has expired.");
            }

            if (!string.IsNullOrWhiteSpace(info.UserId))
            {
                var user = _userManager.GetUserById(info.UserId);

                if (user == null || user.Configuration.IsDisabled)
                {
                    throw new AuthenticationException("User account has been disabled.");
                }
            }
        }

        /// <summary>
        /// Authenticates the new session.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="isLocal">if set to <c>true</c> [is local].</param>
        /// <returns>Task{SessionInfo}.</returns>
        /// <exception cref="AuthenticationException">Invalid user or password entered.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Invalid user or password entered.</exception>
        /// <exception cref="UnauthorizedAccessException">Invalid user or password entered.</exception>
        public async Task<AuthenticationResult> AuthenticateNewSession(AuthenticationRequest request,
            bool isLocal)
        {
            var result = (isLocal && string.Equals(request.App, "Dashboard", StringComparison.OrdinalIgnoreCase)) ||
                await _userManager.AuthenticateUser(request.Username, request.Password, request.RemoteEndPoint).ConfigureAwait(false);

            if (!result)
            {
                EventHelper.FireEventIfNotNull(AuthenticationFailed, this, new GenericEventArgs<AuthenticationRequest>(request), _logger);

                throw new AuthenticationException("Invalid user or password entered.");
            }

            var user = _userManager.Users
                .First(i => string.Equals(request.Username, i.Name, StringComparison.OrdinalIgnoreCase));

            var token = await GetAuthorizationToken(user.Id.ToString("N"), request.DeviceId, request.App, request.DeviceName).ConfigureAwait(false);

            EventHelper.FireEventIfNotNull(AuthenticationSucceeded, this, new GenericEventArgs<AuthenticationRequest>(request), _logger);
            
            var session = await LogSessionActivity(request.App,
                request.AppVersion,
                request.DeviceId,
                request.DeviceName,
                request.RemoteEndPoint,
                user)
                .ConfigureAwait(false);
            
            return new AuthenticationResult
            {
                User = _userManager.GetUserDto(user, request.RemoteEndPoint),
                SessionInfo = GetSessionInfoDto(session),
                AccessToken = token,
                ServerId = _appHost.SystemId
            };
        }

        private async Task<string> GetAuthorizationToken(string userId, string deviceId, string app, string deviceName)
        {
            var existing = _authRepo.Get(new AuthenticationInfoQuery
            {
                DeviceId = deviceId,
                IsActive = true,
                UserId = userId,
                Limit = 1
            });

            if (existing.Items.Length > 0)
            {
                _logger.Debug("Reissuing access token");
                return existing.Items[0].AccessToken;
            }

            var newToken = new AuthenticationInfo
            {
                AppName = app,
                DateCreated = DateTime.UtcNow,
                DeviceId = deviceId,
                DeviceName = deviceName,
                UserId = userId,
                IsActive = true,
                AccessToken = Guid.NewGuid().ToString("N")
            };

            _logger.Debug("Creating new access token for user {0}", userId);
            await _authRepo.Create(newToken, CancellationToken.None).ConfigureAwait(false);

            return newToken.AccessToken;
        }

        public async Task Logout(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new ArgumentNullException("accessToken");
            }

            var existing = _authRepo.Get(new AuthenticationInfoQuery
            {
                Limit = 1,
                AccessToken = accessToken

            }).Items.FirstOrDefault();

            if (existing != null)
            {
                existing.IsActive = false;

                await _authRepo.Update(existing, CancellationToken.None).ConfigureAwait(false);

                var sessions = Sessions
                    .Where(i => string.Equals(i.DeviceId, existing.DeviceId, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var session in sessions)
                {
                    try
                    {
                        ReportSessionEnded(session.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error reporting session ended", ex);
                    }
                }
            }
        }

        public async Task RevokeUserTokens(string userId)
        {
            var existing = _authRepo.Get(new AuthenticationInfoQuery
            {
                IsActive = true,
                UserId = userId
            });

            foreach (var info in existing.Items)
            {
                await Logout(info.AccessToken).ConfigureAwait(false);
            }
        }

        public Task RevokeToken(string token)
        {
            return Logout(token);
        }

        /// <summary>
        /// Reports the capabilities.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="capabilities">The capabilities.</param>
        public void ReportCapabilities(string sessionId, SessionCapabilities capabilities)
        {
            var session = GetSession(sessionId);

            ReportCapabilities(session, capabilities, true);
        }

        private async void ReportCapabilities(SessionInfo session,
            SessionCapabilities capabilities,
            bool saveCapabilities)
        {
            session.PlayableMediaTypes = capabilities.PlayableMediaTypes;
            session.SupportedCommands = capabilities.SupportedCommands;

            if (!string.IsNullOrWhiteSpace(capabilities.MessageCallbackUrl))
            {
                var controller = session.SessionController as HttpSessionController;

                if (controller == null)
                {
                    session.SessionController = new HttpSessionController(_httpClient, _jsonSerializer, session, capabilities.MessageCallbackUrl, this);
                }
            }

            EventHelper.FireEventIfNotNull(CapabilitiesChanged, this, new SessionEventArgs
            {
                SessionInfo = session

            }, _logger);

            if (saveCapabilities)
            {
                await SaveCapabilities(session.DeviceId, capabilities).ConfigureAwait(false);
            }
        }

        private string GetCapabilitiesFilePath(string deviceId)
        {
            var filename = deviceId.GetMD5().ToString("N") + ".json";

            return Path.Combine(_configurationManager.ApplicationPaths.CachePath, "devices", filename);
        }

        private SessionCapabilities GetSavedCapabilities(string deviceId)
        {
            var path = GetCapabilitiesFilePath(deviceId);

            try
            {
                return _jsonSerializer.DeserializeFromFile<SessionCapabilities>(path);
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting saved capabilities", ex);
                return null;
            }
        }

        private readonly SemaphoreSlim _capabilitiesLock = new SemaphoreSlim(1, 1);
        private async Task SaveCapabilities(string deviceId, SessionCapabilities capabilities)
        {
            var path = GetCapabilitiesFilePath(deviceId);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            await _capabilitiesLock.WaitAsync().ConfigureAwait(false);

            try
            {
                _jsonSerializer.SerializeToFile(capabilities, path);
            }
            finally
            {
                _capabilitiesLock.Release();
            }
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
                NowViewingItem = session.NowViewingItem,
                ApplicationVersion = session.ApplicationVersion,
                QueueableMediaTypes = session.QueueableMediaTypes,
                PlayableMediaTypes = session.PlayableMediaTypes,
                AdditionalUsers = session.AdditionalUsers,
                SupportedCommands = session.SupportedCommands,
                UserName = session.UserName,
                NowPlayingItem = session.NowPlayingItem,
                SupportsRemoteControl = session.SupportsMediaControl,
                PlayState = session.PlayState,
                TranscodingInfo = session.NowPlayingItem == null ? null : session.TranscodingInfo
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
        /// <param name="chapterOwner">The chapter owner.</param>
        /// <param name="mediaSourceId">The media source identifier.</param>
        /// <returns>BaseItemInfo.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        private BaseItemInfo GetItemInfo(BaseItem item, BaseItem chapterOwner, string mediaSourceId)
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
                RunTimeTicks = item.RunTimeTicks,
                IndexNumber = item.IndexNumber,
                ParentIndexNumber = item.ParentIndexNumber,
                PremiereDate = item.PremiereDate,
                ProductionYear = item.ProductionYear
            };

            info.PrimaryImageTag = GetImageCacheTag(item, ImageType.Primary);
            if (info.PrimaryImageTag != null)
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

                if (info.PrimaryImageTag == null)
                {
                    var album = audio.Parents.OfType<MusicAlbum>().FirstOrDefault();

                    if (album != null && album.HasImage(ImageType.Primary))
                    {
                        info.PrimaryImageTag = GetImageCacheTag(album, ImageType.Primary);
                        if (info.PrimaryImageTag != null)
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

            if (chapterOwner != null)
            {
                info.ChapterImagesItemId = chapterOwner.Id.ToString("N");

                info.Chapters = _itemRepo.GetChapters(chapterOwner.Id).Select(i => _dtoService.GetChapterInfoDto(i, chapterOwner)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(mediaSourceId))
            {
                info.MediaStreams = _itemRepo.GetMediaStreams(new MediaStreamQuery
                {
                    ItemId = new Guid(mediaSourceId)

                }).ToList();
            }

            return info;
        }

        private string GetImageCacheTag(BaseItem item, ImageType type)
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

            var info = GetItemInfo(item, null, null);

            ReportNowViewingItem(sessionId, info);
        }

        public void ReportNowViewingItem(string sessionId, BaseItemInfo item)
        {
            var session = GetSession(sessionId);

            session.NowViewingItem = item;
        }

        public void ReportTranscodingInfo(string deviceId, TranscodingInfo info)
        {
            var session = Sessions.FirstOrDefault(i => string.Equals(i.DeviceId, deviceId));

            if (session != null)
            {
                session.TranscodingInfo = info;
            }
        }

        public void ClearTranscodingInfo(string deviceId)
        {
            ReportTranscodingInfo(deviceId, null);
        }

        public SessionInfo GetSession(string deviceId, string client, string version)
        {
            return Sessions.FirstOrDefault(i => string.Equals(i.DeviceId, deviceId) &&
                string.Equals(i.Client, client));
        }
    }
}