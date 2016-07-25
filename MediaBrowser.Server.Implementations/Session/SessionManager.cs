using MediaBrowser.Common.Events;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Devices;
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
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Users;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;

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
        private readonly IMediaSourceManager _mediaSourceManager;

        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IServerApplicationHost _appHost;

        private readonly IAuthenticationRepository _authRepo;
        private readonly IDeviceManager _deviceManager;

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

        public SessionManager(IUserDataManager userDataRepository, ILogger logger, IUserRepository userRepository, ILibraryManager libraryManager, IUserManager userManager, IMusicManager musicManager, IDtoService dtoService, IImageProcessor imageProcessor, IJsonSerializer jsonSerializer, IServerApplicationHost appHost, IHttpClient httpClient, IAuthenticationRepository authRepo, IDeviceManager deviceManager, IMediaSourceManager mediaSourceManager)
        {
            _userDataRepository = userDataRepository;
            _logger = logger;
            _userRepository = userRepository;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _musicManager = musicManager;
            _dtoService = dtoService;
            _imageProcessor = imageProcessor;
            _jsonSerializer = jsonSerializer;
            _appHost = appHost;
            _httpClient = httpClient;
            _authRepo = authRepo;
            _deviceManager = deviceManager;
            _mediaSourceManager = mediaSourceManager;

            _deviceManager.DeviceOptionsUpdated += _deviceManager_DeviceOptionsUpdated;
        }

        void _deviceManager_DeviceOptionsUpdated(object sender, GenericEventArgs<DeviceInfo> e)
        {
            foreach (var session in Sessions)
            {
                if (string.Equals(session.DeviceId, e.Argument.Id))
                {
                    session.DeviceName = e.Argument.Name;
                }
            }
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
                    info.AppIconUrl = capabilities.IconUrl;
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
        /// <param name="appName">Type of the client.</param>
        /// <param name="appVersion">The app version.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <param name="user">The user.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        /// <exception cref="System.UnauthorizedAccessException"></exception>
        public async Task<SessionInfo> LogSessionActivity(string appName,
            string appVersion,
            string deviceId,
            string deviceName,
            string remoteEndPoint,
            User user)
        {
            if (string.IsNullOrEmpty(appName))
            {
                throw new ArgumentNullException("appName");
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

            var activityDate = DateTime.UtcNow;
            var session = await GetSessionInfo(appName, appVersion, deviceId, deviceName, remoteEndPoint, user).ConfigureAwait(false);
            var lastActivityDate = session.LastActivityDate;
            session.LastActivityDate = activityDate;

            if (user != null)
            {
                var userLastActivityDate = user.LastActivityDate ?? DateTime.MinValue;
                user.LastActivityDate = activityDate;

                // Don't log in the db anymore frequently than 10 seconds
                if ((activityDate - userLastActivityDate).TotalSeconds > 10)
                {
                    try
                    {
                        // Save this directly. No need to fire off all the events for this.
                        await _userRepository.SaveUser(user, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error updating user", ex);
                    }
                }
            }

            if ((activityDate - lastActivityDate).TotalSeconds > 10)
            {
                EventHelper.FireEventIfNotNull(SessionActivity, this, new SessionEventArgs
                {
                    SessionInfo = session

                }, _logger);
            }

            var controller = session.SessionController;
            if (controller != null)
            {
                controller.OnActivity();
            }

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
                    var key = GetSessionKey(session.Client, session.DeviceId);

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

        private Task<MediaSourceInfo> GetMediaSource(IHasMediaSources item, string mediaSourceId)
        {
            return _mediaSourceManager.GetMediaSource(item, mediaSourceId, false);
        }

        /// <summary>
        /// Updates the now playing item id.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="info">The information.</param>
        /// <param name="libraryItem">The library item.</param>
        private async Task UpdateNowPlayingItem(SessionInfo session, PlaybackProgressInfo info, BaseItem libraryItem)
        {
            if (string.IsNullOrWhiteSpace(info.MediaSourceId))
            {
                info.MediaSourceId = info.ItemId;
            }

            if (!string.IsNullOrWhiteSpace(info.ItemId) && info.Item == null && libraryItem != null)
            {
                var current = session.NowPlayingItem;

                if (current == null || !string.Equals(current.Id, info.ItemId, StringComparison.OrdinalIgnoreCase))
                {
                    var runtimeTicks = libraryItem.RunTimeTicks;

                    MediaSourceInfo mediaSource = null;
                    var hasMediaSources = libraryItem as IHasMediaSources;
                    if (hasMediaSources != null)
                    {
                        mediaSource = await GetMediaSource(hasMediaSources, info.MediaSourceId).ConfigureAwait(false);

                        if (mediaSource != null)
                        {
                            runtimeTicks = mediaSource.RunTimeTicks;
                        }
                    }

                    info.Item = GetItemInfo(libraryItem, libraryItem, mediaSource);

                    info.Item.RunTimeTicks = runtimeTicks;
                }
                else
                {
                    info.Item = current;
                }
            }

            session.NowPlayingItem = info.Item;
            session.LastActivityDate = DateTime.UtcNow;
            session.LastPlaybackCheckIn = DateTime.UtcNow;

            session.PlayState.IsPaused = info.IsPaused;
            session.PlayState.PositionTicks = info.PositionTicks;
            session.PlayState.MediaSourceId = info.MediaSourceId;
            session.PlayState.CanSeek = info.CanSeek;
            session.PlayState.IsMuted = info.IsMuted;
            session.PlayState.VolumeLevel = info.VolumeLevel;
            session.PlayState.AudioStreamIndex = info.AudioStreamIndex;
            session.PlayState.SubtitleStreamIndex = info.SubtitleStreamIndex;
            session.PlayState.PlayMethod = info.PlayMethod;
            session.PlayState.RepeatMode = info.RepeatMode;
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

        private string GetSessionKey(string appName, string deviceId)
        {
            return appName + deviceId;
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <param name="appName">Type of the client.</param>
        /// <param name="appVersion">The app version.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <param name="user">The user.</param>
        /// <returns>SessionInfo.</returns>
        private async Task<SessionInfo> GetSessionInfo(string appName, string appVersion, string deviceId, string deviceName, string remoteEndPoint, User user)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new ArgumentNullException("deviceId");
            }
            var key = GetSessionKey(appName, deviceId);

            await _sessionLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            var userId = user == null ? (Guid?)null : user.Id;
            var username = user == null ? null : user.Name;

            try
            {
                SessionInfo sessionInfo;
                DeviceInfo device = null;

                if (!_activeConnections.TryGetValue(key, out sessionInfo))
                {
                    sessionInfo = new SessionInfo
                    {
                        Client = appName,
                        DeviceId = deviceId,
                        ApplicationVersion = appVersion,
                        Id = key.GetMD5().ToString("N")
                    };

                    sessionInfo.DeviceName = deviceName;
                    sessionInfo.UserId = userId;
                    sessionInfo.UserName = username;
                    sessionInfo.RemoteEndPoint = remoteEndPoint;

                    OnSessionStarted(sessionInfo);

                    _activeConnections.TryAdd(key, sessionInfo);

                    if (!string.IsNullOrEmpty(deviceId))
                    {
                        var userIdString = userId.HasValue ? userId.Value.ToString("N") : null;
                        device = await _deviceManager.RegisterDevice(deviceId, deviceName, appName, appVersion, userIdString).ConfigureAwait(false);
                    }
                }

                device = device ?? _deviceManager.GetDevice(deviceId);

                if (device == null)
                {
                    var userIdString = userId.HasValue ? userId.Value.ToString("N") : null;
                    device = await _deviceManager.RegisterDevice(deviceId, deviceName, appName, appVersion, userIdString).ConfigureAwait(false);
                }

                if (device != null)
                {
                    if (!string.IsNullOrEmpty(device.CustomName))
                    {
                        deviceName = device.CustomName;
                    }
                }

                sessionInfo.DeviceName = deviceName;
                sessionInfo.UserId = userId;
                sessionInfo.UserName = username;
                sessionInfo.RemoteEndPoint = remoteEndPoint;
                sessionInfo.ApplicationVersion = appVersion;

                if (!userId.HasValue)
                {
                    sessionInfo.AdditionalUsers.Clear();
                }

                if (sessionInfo.SessionController == null)
                {
                    sessionInfo.SessionController = _sessionFactories
                        .Select(i => i.GetSessionController(sessionInfo))
                        .FirstOrDefault(i => i != null);
                }

                return sessionInfo;
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

        private Timer _idleTimer;

        private void StartIdleCheckTimer()
        {
            if (_idleTimer == null)
            {
                _idleTimer = new Timer(CheckForIdlePlayback, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            }
        }
        private void StopIdleCheckTimer()
        {
            if (_idleTimer != null)
            {
                _idleTimer.Dispose();
                _idleTimer = null;
            }
        }

        private async void CheckForIdlePlayback(object state)
        {
            var playingSessions = Sessions.Where(i => i.NowPlayingItem != null)
                .ToList();

            if (playingSessions.Count > 0)
            {
                var idle = playingSessions
                    .Where(i => (DateTime.UtcNow - i.LastPlaybackCheckIn).TotalMinutes > 5)
                    .ToList();

                foreach (var session in idle)
                {
                    _logger.Debug("Session {0} has gone idle while playing", session.Id);

                    try
                    {
                        await OnPlaybackStopped(new PlaybackStopInfo
                        {
                            Item = session.NowPlayingItem,
                            ItemId = session.NowPlayingItem == null ? null : session.NowPlayingItem.Id,
                            SessionId = session.Id,
                            MediaSourceId = session.PlayState == null ? null : session.PlayState.MediaSourceId,
                            PositionTicks = session.PlayState == null ? null : session.PlayState.PositionTicks
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug("Error calling OnPlaybackStopped", ex);
                    }
                }

                playingSessions = Sessions.Where(i => i.NowPlayingItem != null)
                    .ToList();
            }

            if (playingSessions.Count == 0)
            {
                StopIdleCheckTimer();
            }
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

            await UpdateNowPlayingItem(session, info, libraryItem).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(session.DeviceId) && info.PlayMethod != PlayMethod.Transcode)
            {
                ClearTranscodingInfo(session.DeviceId);
            }

            session.QueueableMediaTypes = info.QueueableMediaTypes;

            var users = GetUsers(session);

            if (libraryItem != null)
            {
                foreach (var user in users)
                {
                    await OnPlaybackStart(user.Id, libraryItem).ConfigureAwait(false);
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

            StartIdleCheckTimer();
        }

        /// <summary>
        /// Called when [playback start].
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="item">The item.</param>
        /// <returns>Task.</returns>
        private async Task OnPlaybackStart(Guid userId, IHasUserData item)
        {
            var data = _userDataRepository.GetUserData(userId, item);

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

            await UpdateNowPlayingItem(session, info, libraryItem).ConfigureAwait(false);

            var users = GetUsers(session);

            if (libraryItem != null)
            {
                foreach (var user in users)
                {
                    await OnPlaybackProgress(user, libraryItem, info).ConfigureAwait(false);
                }
            }

            if (!string.IsNullOrWhiteSpace(info.LiveStreamId))
            {
                try
                {
                    await _mediaSourceManager.PingLiveStream(info.LiveStreamId, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error closing live stream", ex);
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
                DeviceId = session.DeviceId,
                IsPaused = info.IsPaused,
                PlaySessionId = info.PlaySessionId

            }, _logger);

            StartIdleCheckTimer();
        }

        private async Task OnPlaybackProgress(User user, BaseItem item, PlaybackProgressInfo info)
        {
            var data = _userDataRepository.GetUserData(user.Id, item);

            var positionTicks = info.PositionTicks;

            if (positionTicks.HasValue)
            {
                _userDataRepository.UpdatePlayState(item, data, positionTicks.Value);

                UpdatePlaybackSettings(user, info, data);

                await _userDataRepository.SaveUserData(user.Id, item, data, UserDataSaveReason.PlaybackProgress, CancellationToken.None).ConfigureAwait(false);
            }
        }

        private void UpdatePlaybackSettings(User user, PlaybackProgressInfo info, UserItemData data)
        {
            if (user.Configuration.RememberAudioSelections)
            {
                data.AudioStreamIndex = info.AudioStreamIndex;
            }
            else
            {
                data.AudioStreamIndex = null;
            }

            if (user.Configuration.RememberSubtitleSelections)
            {
                data.SubtitleStreamIndex = info.SubtitleStreamIndex;
            }
            else
            {
                data.SubtitleStreamIndex = null;
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

            if (!string.IsNullOrWhiteSpace(info.ItemId) && info.Item == null && libraryItem != null)
            {
                var current = session.NowPlayingItem;

                if (current == null || !string.Equals(current.Id, info.ItemId, StringComparison.OrdinalIgnoreCase))
                {
                    MediaSourceInfo mediaSource = null;

                    var hasMediaSources = libraryItem as IHasMediaSources;
                    if (hasMediaSources != null)
                    {
                        mediaSource = await GetMediaSource(hasMediaSources, info.MediaSourceId).ConfigureAwait(false);
                    }

                    info.Item = GetItemInfo(libraryItem, libraryItem, mediaSource);
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
                foreach (var user in users)
                {
                    playedToCompletion = await OnPlaybackStopped(user.Id, libraryItem, info.PositionTicks, info.Failed).ConfigureAwait(false);
                }
            }

            if (!string.IsNullOrWhiteSpace(info.LiveStreamId))
            {
                try
                {
                    await _mediaSourceManager.CloseLiveStream(info.LiveStreamId, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error closing live stream", ex);
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

        private async Task<bool> OnPlaybackStopped(Guid userId, BaseItem item, long? positionTicks, bool playbackFailed)
        {
            bool playedToCompletion = false;

            if (!playbackFailed)
            {
                var data = _userDataRepository.GetUserData(userId, item);

                if (positionTicks.HasValue)
                {
                    playedToCompletion = _userDataRepository.UpdatePlayState(item, data, positionTicks.Value);
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
            }

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

        private SessionInfo GetSessionToRemoteControl(string sessionId)
        {
            // Accept either device id or session id
            var session = Sessions.FirstOrDefault(i => string.Equals(i.Id, sessionId));

            if (session == null)
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
            var session = GetSessionToRemoteControl(sessionId);

            var controllingSession = GetSession(controllingSessionId);
            AssertCanControl(session, controllingSession);

            return session.SessionController.SendGeneralCommand(command, cancellationToken);
        }

        public async Task SendPlayCommand(string controllingSessionId, string sessionId, PlayRequest command, CancellationToken cancellationToken)
        {
            var session = GetSessionToRemoteControl(sessionId);

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
                var list = new List<BaseItem>();
                foreach (var itemId in command.ItemIds)
                {
                    var subItems = await TranslateItemForPlayback(itemId, user).ConfigureAwait(false);
                    list.AddRange(subItems);
                }
                
                items = list
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
                    throw new ArgumentException(string.Format("{0} is unable to queue the requested media type.", session.DeviceName ?? session.Id));
                }
            }
            else
            {
                if (items.Any(i => !session.PlayableMediaTypes.Contains(i.MediaType, StringComparer.OrdinalIgnoreCase)))
                {
                    throw new ArgumentException(string.Format("{0} is unable to play the requested media type.", session.DeviceName ?? session.Id));
                }
            }

            if (user != null && command.ItemIds.Length == 1 && user.Configuration.EnableNextEpisodeAutoPlay)
            {
                var episode = _libraryManager.GetItemById(command.ItemIds[0]) as Episode;
                if (episode != null)
                {
                    var series = episode.Series;
                    if (series != null)
                    {
                        var episodes = series.GetEpisodes(user, false, false)
                            .SkipWhile(i => i.Id != episode.Id)
                            .ToList();

                        if (episodes.Count > 0)
                        {
                            command.ItemIds = episodes.Select(i => i.Id.ToString("N")).ToArray();
                        }
                    }
                }
            }

            var controllingSession = GetSession(controllingSessionId);
            AssertCanControl(session, controllingSession);
            if (controllingSession.UserId.HasValue)
            {
                command.ControllingUserId = controllingSession.UserId.Value.ToString("N");
            }

            await session.SessionController.SendPlayCommand(command, cancellationToken).ConfigureAwait(false);
        }

        private async Task<List<BaseItem>> TranslateItemForPlayback(string id, User user)
        {
            var item = _libraryManager.GetItemById(id);

            if (item == null)
            {
                _logger.Error("A non-existant item Id {0} was passed into TranslateItemForPlayback", id);
                return new List<BaseItem>();
            }

            var byName = item as IItemByName;

            if (byName != null)
            {
                var items = byName.GetTaggedItems(new InternalItemsQuery(user)
                {
                    IsFolder = false,
                    Recursive = true
                });

                return FilterToSingleMediaType(items)
                    .OrderBy(i => i.SortName)
                    .ToList();
            }

            if (item.IsFolder)
            {
                var folder = (Folder)item;

                var itemsResult = await folder.GetItems(new InternalItemsQuery(user)
                {
                    Recursive = true,
                    IsFolder = false

                }).ConfigureAwait(false);

                return FilterToSingleMediaType(itemsResult.Items)
                    .OrderBy(i => i.SortName)
                    .ToList();
            }

            return new List<BaseItem> { item };
        }

        private IEnumerable<BaseItem> FilterToSingleMediaType(IEnumerable<BaseItem> items)
        {
            return items
                .Where(i => !string.IsNullOrWhiteSpace(i.MediaType))
                .ToLookup(i => i.MediaType, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(i => i.Count())
                .FirstOrDefault();
        }

        private IEnumerable<BaseItem> TranslateItemForInstantMix(string id, User user)
        {
            var item = _libraryManager.GetItemById(id);

            if (item == null)
            {
                _logger.Error("A non-existant item Id {0} was passed into TranslateItemForInstantMix", id);
                return new List<BaseItem>();
            }

            return _musicManager.GetInstantMixFromItem(item, user);
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
            var session = GetSessionToRemoteControl(sessionId);

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
        public async Task SendRestartRequiredNotification(CancellationToken cancellationToken)
        {
            var sessions = Sessions.Where(i => i.IsActive && i.SessionController != null).ToList();

            var info = await _appHost.GetSystemInfo().ConfigureAwait(false);

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

            await Task.WhenAll(tasks).ConfigureAwait(false);
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
        public void AddAdditionalUser(string sessionId, string userId)
        {
            var session = GetSession(sessionId);

            if (session.UserId.HasValue && session.UserId.Value == new Guid(userId))
            {
                throw new ArgumentException("The requested user is already the primary user of the session.");
            }

            if (session.AdditionalUsers.All(i => new Guid(i.UserId) != new Guid(userId)))
            {
                var user = _userManager.GetUserById(userId);

                session.AdditionalUsers.Add(new SessionUserInfo
                {
                    UserId = userId,
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
        public void RemoveAdditionalUser(string sessionId, string userId)
        {
            var session = GetSession(sessionId);

            if (session.UserId.HasValue && session.UserId.Value == new Guid(userId))
            {
                throw new ArgumentException("The requested user is already the primary user of the session.");
            }

            var user = session.AdditionalUsers.FirstOrDefault(i => new Guid(i.UserId) == new Guid(userId));

            if (user != null)
            {
                session.AdditionalUsers.Remove(user);
            }
        }

        /// <summary>
        /// Authenticates the new session.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{SessionInfo}.</returns>
        public Task<AuthenticationResult> AuthenticateNewSession(AuthenticationRequest request)
        {
            return AuthenticateNewSessionInternal(request, true);
        }

        public Task<AuthenticationResult> CreateNewSession(AuthenticationRequest request)
        {
            return AuthenticateNewSessionInternal(request, false);
        }

        private async Task<AuthenticationResult> AuthenticateNewSessionInternal(AuthenticationRequest request, bool enforcePassword)
        {
            var user = _userManager.Users
                .FirstOrDefault(i => string.Equals(request.Username, i.Name, StringComparison.OrdinalIgnoreCase));

            if (user != null && !string.IsNullOrWhiteSpace(request.DeviceId))
            {
                if (!_deviceManager.CanAccessDevice(user.Id.ToString("N"), request.DeviceId))
                {
                    throw new SecurityException("User is not allowed access from this device.");
                }
            }

            if (enforcePassword)
            {
                var result = await _userManager.AuthenticateUser(request.Username, request.PasswordSha1, request.PasswordMd5, request.RemoteEndPoint).ConfigureAwait(false);

                if (!result)
                {
                    EventHelper.FireEventIfNotNull(AuthenticationFailed, this, new GenericEventArgs<AuthenticationRequest>(request), _logger);

                    throw new SecurityException("Invalid user or password entered.");
                }
            }

            var token = await GetAuthorizationToken(user.Id.ToString("N"), request.DeviceId, request.App, request.AppVersion, request.DeviceName).ConfigureAwait(false);

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


        private async Task<string> GetAuthorizationToken(string userId, string deviceId, string app, string appVersion, string deviceName)
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
                var token = existing.Items[0].AccessToken;
                _logger.Info("Reissuing access token: " + token);
                return token;
            }

            var newToken = new AuthenticationInfo
            {
                AppName = app,
                AppVersion = appVersion,
                DateCreated = DateTime.UtcNow,
                DeviceId = deviceId,
                DeviceName = deviceName,
                UserId = userId,
                IsActive = true,
                AccessToken = Guid.NewGuid().ToString("N")
            };

            _logger.Info("Creating new access token for user {0}", userId);
            await _authRepo.Create(newToken, CancellationToken.None).ConfigureAwait(false);

            return newToken.AccessToken;
        }

        public async Task Logout(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new ArgumentNullException("accessToken");
            }

            _logger.Info("Logging out access token {0}", accessToken);

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

        public async Task RevokeUserTokens(string userId, string currentAccessToken)
        {
            var existing = _authRepo.Get(new AuthenticationInfoQuery
            {
                IsActive = true,
                UserId = userId
            });

            foreach (var info in existing.Items)
            {
                if (!string.Equals(currentAccessToken, info.AccessToken, StringComparison.OrdinalIgnoreCase))
                {
                    await Logout(info.AccessToken).ConfigureAwait(false);
                }
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
        public void ReportCapabilities(string sessionId, ClientCapabilities capabilities)
        {
            var session = GetSession(sessionId);

            ReportCapabilities(session, capabilities, true);
        }

        private async void ReportCapabilities(SessionInfo session,
            ClientCapabilities capabilities,
            bool saveCapabilities)
        {
            session.Capabilities = capabilities;

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
                try
                {
                    await SaveCapabilities(session.DeviceId, capabilities).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error saving device capabilities", ex);
                }
            }
        }

        private ClientCapabilities GetSavedCapabilities(string deviceId)
        {
            return _deviceManager.GetCapabilities(deviceId);
        }

        private Task SaveCapabilities(string deviceId, ClientCapabilities capabilities)
        {
            return _deviceManager.SaveCapabilities(deviceId, capabilities);
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
                AppIconUrl = session.AppIconUrl,
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
        /// <param name="mediaSource">The media source.</param>
        /// <returns>BaseItemInfo.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        private BaseItemInfo GetItemInfo(BaseItem item, BaseItem chapterOwner, MediaSourceInfo mediaSource)
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
            if (recording != null)
            {
                if (recording.IsSeries)
                {
                    info.Name = recording.EpisodeTitle;
                    info.SeriesName = recording.Name;

                    if (string.IsNullOrWhiteSpace(info.Name))
                    {
                        info.Name = recording.Name;
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
                    var album = audio.AlbumEntity;

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
                info.Artists = musicVideo.Artists.ToList();
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
                backropItem = item.GetParents().FirstOrDefault(i => i.HasImage(ImageType.Backdrop));
            }

            if (thumbItem == null)
            {
                thumbItem = item.GetParents().FirstOrDefault(i => i.HasImage(ImageType.Thumb));
            }

            if (logoItem == null)
            {
                logoItem = item.GetParents().FirstOrDefault(i => i.HasImage(ImageType.Logo));
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

                info.Chapters = _dtoService.GetChapterInfoDtos(chapterOwner).ToList();
            }

            if (mediaSource != null)
            {
                info.MediaStreams = mediaSource.MediaStreams;
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
            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

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

        public Task<SessionInfo> GetSessionByAuthenticationToken(AuthenticationInfo info, string deviceId, string remoteEndpoint, string appVersion)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            var user = string.IsNullOrWhiteSpace(info.UserId)
                ? null
                : _userManager.GetUserById(info.UserId);

            appVersion = string.IsNullOrWhiteSpace(appVersion)
                ? info.AppVersion
                : appVersion;

            var deviceName = info.DeviceName;
            var appName = info.AppName;

            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                // Replace the info from the token with more recent info
                var device = _deviceManager.GetDevice(deviceId);
                if (device != null)
                {
                    deviceName = device.Name;
                    appName = device.AppName;

                    if (!string.IsNullOrWhiteSpace(device.AppVersion))
                    {
                        appVersion = device.AppVersion;
                    }
                }
            }
            else
            {
                deviceId = info.DeviceId;
            }

            // Prevent argument exception
            if (string.IsNullOrWhiteSpace(appVersion))
            {
                appVersion = "1";
            }

            return LogSessionActivity(appName, appVersion, deviceId, deviceName, remoteEndpoint, user);
        }

        public Task<SessionInfo> GetSessionByAuthenticationToken(string token, string deviceId, string remoteEndpoint)
        {
            var result = _authRepo.Get(new AuthenticationInfoQuery
            {
                AccessToken = token
            });

            var info = result.Items.FirstOrDefault();

            if (info == null)
            {
                return Task.FromResult<SessionInfo>(null);
            }

            return GetSessionByAuthenticationToken(info, deviceId, remoteEndpoint, null);
        }

        public Task SendMessageToUserSessions<T>(string userId, string name, T data,
            CancellationToken cancellationToken)
        {
            var sessions = Sessions.Where(i => i.IsActive && i.SessionController != null && i.ContainsUser(userId)).ToList();

            var tasks = sessions.Select(session => Task.Run(async () =>
            {
                try
                {
                    await session.SessionController.SendMessage(name, data, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error sending message", ex);
                }

            }, cancellationToken));

            return Task.WhenAll(tasks);
        }

        public Task SendMessageToUserDeviceSessions<T>(string deviceId, string name, T data,
            CancellationToken cancellationToken)
        {
            var sessions = Sessions.Where(i => i.IsActive && i.SessionController != null && string.Equals(i.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase)).ToList();

            var tasks = sessions.Select(session => Task.Run(async () =>
            {
                try
                {
                    await session.SessionController.SendMessage(name, data, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error sending message", ex);
                }

            }, cancellationToken));

            return Task.WhenAll(tasks);
        }
    }
}