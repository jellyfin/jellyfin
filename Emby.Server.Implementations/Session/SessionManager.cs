#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Entities.Security;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Events;
using Jellyfin.Data.Queries;
using Jellyfin.Extensions;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Events.Authentication;
using MediaBrowser.Controller.Events.Session;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Episode = MediaBrowser.Controller.Entities.TV.Episode;

namespace Emby.Server.Implementations.Session
{
    /// <summary>
    /// Class SessionManager.
    /// </summary>
    public sealed class SessionManager : ISessionManager, IAsyncDisposable
    {
        private readonly IUserDataManager _userDataManager;
        private readonly IServerConfigurationManager _config;
        private readonly ILogger<SessionManager> _logger;
        private readonly IEventManager _eventManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IMusicManager _musicManager;
        private readonly IDtoService _dtoService;
        private readonly IImageProcessor _imageProcessor;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IServerApplicationHost _appHost;
        private readonly IDeviceManager _deviceManager;
        private readonly CancellationTokenRegistration _shutdownCallback;
        private readonly ConcurrentDictionary<string, SessionInfo> _activeConnections
            = new(StringComparer.OrdinalIgnoreCase);

        private Timer _idleTimer;
        private Timer _inactiveTimer;

        private DtoOptions _itemInfoDtoOptions;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionManager"/> class.
        /// </summary>
        /// <param name="logger">Instance of <see cref="ILogger{SessionManager}"/> interface.</param>
        /// <param name="eventManager">Instance of <see cref="IEventManager"/> interface.</param>
        /// <param name="userDataManager">Instance of <see cref="IUserDataManager"/> interface.</param>
        /// <param name="serverConfigurationManager">Instance of <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="libraryManager">Instance of <see cref="ILibraryManager"/> interface.</param>
        /// <param name="userManager">Instance of <see cref="IUserManager"/> interface.</param>
        /// <param name="musicManager">Instance of <see cref="IMusicManager"/> interface.</param>
        /// <param name="dtoService">Instance of <see cref="IDtoService"/> interface.</param>
        /// <param name="imageProcessor">Instance of <see cref="IImageProcessor"/> interface.</param>
        /// <param name="appHost">Instance of <see cref="IServerApplicationHost"/> interface.</param>
        /// <param name="deviceManager">Instance of <see cref="IDeviceManager"/> interface.</param>
        /// <param name="mediaSourceManager">Instance of <see cref="IMediaSourceManager"/> interface.</param>
        /// <param name="hostApplicationLifetime">Instance of <see cref="IHostApplicationLifetime"/> interface.</param>
        public SessionManager(
            ILogger<SessionManager> logger,
            IEventManager eventManager,
            IUserDataManager userDataManager,
            IServerConfigurationManager serverConfigurationManager,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IMusicManager musicManager,
            IDtoService dtoService,
            IImageProcessor imageProcessor,
            IServerApplicationHost appHost,
            IDeviceManager deviceManager,
            IMediaSourceManager mediaSourceManager,
            IHostApplicationLifetime hostApplicationLifetime)
        {
            _logger = logger;
            _eventManager = eventManager;
            _userDataManager = userDataManager;
            _config = serverConfigurationManager;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _musicManager = musicManager;
            _dtoService = dtoService;
            _imageProcessor = imageProcessor;
            _appHost = appHost;
            _deviceManager = deviceManager;
            _mediaSourceManager = mediaSourceManager;
            _shutdownCallback = hostApplicationLifetime.ApplicationStopping.Register(OnApplicationStopping);

            _deviceManager.DeviceOptionsUpdated += OnDeviceManagerDeviceOptionsUpdated;
        }

        /// <summary>
        /// Occurs when playback has started.
        /// </summary>
        public event EventHandler<PlaybackProgressEventArgs> PlaybackStart;

        /// <summary>
        /// Occurs when playback has progressed.
        /// </summary>
        public event EventHandler<PlaybackProgressEventArgs> PlaybackProgress;

        /// <summary>
        /// Occurs when playback has stopped.
        /// </summary>
        public event EventHandler<PlaybackStopEventArgs> PlaybackStopped;

        /// <inheritdoc />
        public event EventHandler<SessionEventArgs> SessionStarted;

        /// <inheritdoc />
        public event EventHandler<SessionEventArgs> CapabilitiesChanged;

        /// <inheritdoc />
        public event EventHandler<SessionEventArgs> SessionEnded;

        /// <inheritdoc />
        public event EventHandler<SessionEventArgs> SessionActivity;

        /// <inheritdoc />
        public event EventHandler<SessionEventArgs> SessionControllerConnected;

        /// <summary>
        /// Gets all connections.
        /// </summary>
        /// <value>All connections.</value>
        public IEnumerable<SessionInfo> Sessions => _activeConnections.Values.OrderByDescending(c => c.LastActivityDate);

        private void OnDeviceManagerDeviceOptionsUpdated(object sender, GenericEventArgs<Tuple<string, DeviceOptions>> e)
        {
            foreach (var session in Sessions)
            {
                if (string.Equals(session.DeviceId, e.Argument.Item1, StringComparison.Ordinal))
                {
                    if (!string.IsNullOrWhiteSpace(e.Argument.Item2.CustomName))
                    {
                        session.HasCustomDeviceName = true;
                        session.DeviceName = e.Argument.Item2.CustomName;
                    }
                    else
                    {
                        session.HasCustomDeviceName = false;
                    }
                }
            }
        }

        private void CheckDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        private void OnSessionStarted(SessionInfo info)
        {
            if (!string.IsNullOrEmpty(info.DeviceId))
            {
                var capabilities = _deviceManager.GetCapabilities(info.DeviceId);

                if (capabilities is not null)
                {
                    ReportCapabilities(info, capabilities, false);
                }
            }

            _eventManager.Publish(new SessionStartedEventArgs(info));

            EventHelper.QueueEventIfNotNull(
                SessionStarted,
                this,
                new SessionEventArgs
                {
                    SessionInfo = info
                },
                _logger);
        }

        private async ValueTask OnSessionEnded(SessionInfo info)
        {
            EventHelper.QueueEventIfNotNull(
                SessionEnded,
                this,
                new SessionEventArgs
                {
                    SessionInfo = info
                },
                _logger);

            _eventManager.Publish(new SessionEndedEventArgs(info));

            await info.DisposeAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void UpdateDeviceName(string sessionId, string reportedDeviceName)
        {
            var session = GetSession(sessionId);
            if (session is not null)
            {
                session.DeviceName = reportedDeviceName;
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
        /// <returns>SessionInfo.</returns>
        public async Task<SessionInfo> LogSessionActivity(
            string appName,
            string appVersion,
            string deviceId,
            string deviceName,
            string remoteEndPoint,
            User user)
        {
            CheckDisposed();

            ArgumentException.ThrowIfNullOrEmpty(appName);
            ArgumentException.ThrowIfNullOrEmpty(appVersion);
            ArgumentException.ThrowIfNullOrEmpty(deviceId);

            var activityDate = DateTime.UtcNow;
            var session = GetSessionInfo(appName, appVersion, deviceId, deviceName, remoteEndPoint, user);
            var lastActivityDate = session.LastActivityDate;
            session.LastActivityDate = activityDate;

            if (user is not null)
            {
                var userLastActivityDate = user.LastActivityDate ?? DateTime.MinValue;

                if ((activityDate - userLastActivityDate).TotalSeconds > 60)
                {
                    try
                    {
                        user.LastActivityDate = activityDate;
                        await _userManager.UpdateUserAsync(user).ConfigureAwait(false);
                    }
                    catch (DbUpdateConcurrencyException e)
                    {
                        _logger.LogDebug(e, "Error updating user's last activity date.");
                    }
                }
            }

            if ((activityDate - lastActivityDate).TotalSeconds > 10)
            {
                SessionActivity?.Invoke(
                    this,
                    new SessionEventArgs
                    {
                        SessionInfo = session
                    });
            }

            return session;
        }

        /// <inheritdoc />
        public void OnSessionControllerConnected(SessionInfo session)
        {
            EventHelper.QueueEventIfNotNull(
                SessionControllerConnected,
                this,
                new SessionEventArgs
                {
                    SessionInfo = session
                },
                _logger);
        }

        /// <inheritdoc />
        public async Task CloseIfNeededAsync(SessionInfo session)
        {
            if (!session.SessionControllers.Any(i => i.IsSessionActive))
            {
                var key = GetSessionKey(session.Client, session.DeviceId);

                _activeConnections.TryRemove(key, out _);
                if (!string.IsNullOrEmpty(session.PlayState?.LiveStreamId))
                {
                    await _mediaSourceManager.CloseLiveStream(session.PlayState.LiveStreamId).ConfigureAwait(false);
                }

                await OnSessionEnded(session).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async ValueTask ReportSessionEnded(string sessionId)
        {
            CheckDisposed();
            var session = GetSession(sessionId, false);

            if (session is not null)
            {
                var key = GetSessionKey(session.Client, session.DeviceId);

                _activeConnections.TryRemove(key, out _);

                await OnSessionEnded(session).ConfigureAwait(false);
            }
        }

        private Task<MediaSourceInfo> GetMediaSource(BaseItem item, string mediaSourceId, string liveStreamId)
        {
            return _mediaSourceManager.GetMediaSource(item, mediaSourceId, liveStreamId, false, CancellationToken.None);
        }

        /// <summary>
        /// Updates the now playing item id.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task UpdateNowPlayingItem(SessionInfo session, PlaybackProgressInfo info, BaseItem libraryItem, bool updateLastCheckInTime)
        {
            if (string.IsNullOrEmpty(info.MediaSourceId))
            {
                info.MediaSourceId = info.ItemId.ToString("N", CultureInfo.InvariantCulture);
            }

            if (!info.ItemId.IsEmpty() && info.Item is null && libraryItem is not null)
            {
                var current = session.NowPlayingItem;

                if (current is null || !info.ItemId.Equals(current.Id))
                {
                    var runtimeTicks = libraryItem.RunTimeTicks;

                    MediaSourceInfo mediaSource = null;
                    if (libraryItem is IHasMediaSources)
                    {
                        mediaSource = await GetMediaSource(libraryItem, info.MediaSourceId, info.LiveStreamId).ConfigureAwait(false);

                        if (mediaSource is not null)
                        {
                            runtimeTicks = mediaSource.RunTimeTicks;
                        }
                    }

                    info.Item = GetItemInfo(libraryItem, mediaSource);

                    info.Item.RunTimeTicks = runtimeTicks;
                }
                else
                {
                    info.Item = current;
                }
            }

            session.NowPlayingItem = info.Item;
            session.LastActivityDate = DateTime.UtcNow;

            if (updateLastCheckInTime)
            {
                session.LastPlaybackCheckIn = DateTime.UtcNow;
            }

            if (info.IsPaused && session.LastPausedDate is null)
            {
                session.LastPausedDate = DateTime.UtcNow;
            }
            else if (!info.IsPaused)
            {
                session.LastPausedDate = null;
            }

            session.PlayState.IsPaused = info.IsPaused;
            session.PlayState.PositionTicks = info.PositionTicks;
            session.PlayState.MediaSourceId = info.MediaSourceId;
            session.PlayState.LiveStreamId = info.LiveStreamId;
            session.PlayState.CanSeek = info.CanSeek;
            session.PlayState.IsMuted = info.IsMuted;
            session.PlayState.VolumeLevel = info.VolumeLevel;
            session.PlayState.AudioStreamIndex = info.AudioStreamIndex;
            session.PlayState.SubtitleStreamIndex = info.SubtitleStreamIndex;
            session.PlayState.PlayMethod = info.PlayMethod;
            session.PlayState.RepeatMode = info.RepeatMode;
            session.PlayState.PlaybackOrder = info.PlaybackOrder;
            session.PlaylistItemId = info.PlaylistItemId;

            var nowPlayingQueue = info.NowPlayingQueue;

            if (nowPlayingQueue?.Length > 0)
            {
                session.NowPlayingQueue = nowPlayingQueue;

                var itemIds = Array.ConvertAll(nowPlayingQueue, queue => queue.Id);
                session.NowPlayingQueueFullItems = _dtoService.GetBaseItemDtos(
                    _libraryManager.GetItemList(new InternalItemsQuery { ItemIds = itemIds }),
                    new DtoOptions(true));
            }
        }

        /// <summary>
        /// Removes the now playing item id.
        /// </summary>
        /// <param name="session">The session.</param>
        private void RemoveNowPlayingItem(SessionInfo session)
        {
            session.NowPlayingItem = null;
            session.PlayState = new PlayerStateInfo();

            if (!string.IsNullOrEmpty(session.DeviceId))
            {
                ClearTranscodingInfo(session.DeviceId);
            }
        }

        private static string GetSessionKey(string appName, string deviceId)
            => appName + deviceId;

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
        private SessionInfo GetSessionInfo(
            string appName,
            string appVersion,
            string deviceId,
            string deviceName,
            string remoteEndPoint,
            User user)
        {
            CheckDisposed();

            ArgumentException.ThrowIfNullOrEmpty(deviceId);

            var key = GetSessionKey(appName, deviceId);

            CheckDisposed();

            if (!_activeConnections.TryGetValue(key, out var sessionInfo))
            {
                sessionInfo = CreateSession(key, appName, appVersion, deviceId, deviceName, remoteEndPoint, user);
                _activeConnections[key] = sessionInfo;
            }

            sessionInfo.UserId = user?.Id ?? Guid.Empty;
            sessionInfo.UserName = user?.Username;
            sessionInfo.UserPrimaryImageTag = user?.ProfileImage is null ? null : GetImageCacheTag(user);
            sessionInfo.RemoteEndPoint = remoteEndPoint;
            sessionInfo.Client = appName;

            if (!sessionInfo.HasCustomDeviceName || string.IsNullOrEmpty(sessionInfo.DeviceName))
            {
                sessionInfo.DeviceName = deviceName;
            }

            sessionInfo.ApplicationVersion = appVersion;

            if (user is null)
            {
                sessionInfo.AdditionalUsers = Array.Empty<SessionUserInfo>();
            }

            return sessionInfo;
        }

        private SessionInfo CreateSession(
            string key,
            string appName,
            string appVersion,
            string deviceId,
            string deviceName,
            string remoteEndPoint,
            User user)
        {
            var sessionInfo = new SessionInfo(this, _logger)
            {
                Client = appName,
                DeviceId = deviceId,
                ApplicationVersion = appVersion,
                Id = key.GetMD5().ToString("N", CultureInfo.InvariantCulture),
                ServerId = _appHost.SystemId
            };

            var username = user?.Username;

            sessionInfo.UserId = user?.Id ?? Guid.Empty;
            sessionInfo.UserName = username;
            sessionInfo.UserPrimaryImageTag = user?.ProfileImage is null ? null : GetImageCacheTag(user);
            sessionInfo.RemoteEndPoint = remoteEndPoint;

            if (string.IsNullOrEmpty(deviceName))
            {
                deviceName = "Network Device";
            }

            var deviceOptions = _deviceManager.GetDeviceOptions(deviceId) ?? new()
            {
                DeviceId = deviceId
            };
            if (string.IsNullOrEmpty(deviceOptions.CustomName))
            {
                sessionInfo.DeviceName = deviceName;
            }
            else
            {
                sessionInfo.DeviceName = deviceOptions.CustomName;
                sessionInfo.HasCustomDeviceName = true;
            }

            OnSessionStarted(sessionInfo);
            return sessionInfo;
        }

        private List<User> GetUsers(SessionInfo session)
        {
            var users = new List<User>();

            if (session.UserId.IsEmpty())
            {
                return users;
            }

            var user = _userManager.GetUserById(session.UserId);

            if (user is null)
            {
                throw new InvalidOperationException("User not found");
            }

            users.Add(user);

            users.AddRange(session.AdditionalUsers
                .Select(i => _userManager.GetUserById(i.UserId))
                .Where(i => i is not null));

            return users;
        }

        private void StartCheckTimers()
        {
            _idleTimer ??= new Timer(CheckForIdlePlayback, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            if (_config.Configuration.InactiveSessionThreshold > 0)
            {
                _inactiveTimer ??= new Timer(CheckForInactiveSteams, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            }
            else
            {
                StopInactiveCheckTimer();
            }
        }

        private void StopIdleCheckTimer()
        {
            if (_idleTimer is not null)
            {
                _idleTimer.Dispose();
                _idleTimer = null;
            }
        }

        private void StopInactiveCheckTimer()
        {
            if (_inactiveTimer is not null)
            {
                _inactiveTimer.Dispose();
                _inactiveTimer = null;
            }
        }

        private async void CheckForIdlePlayback(object state)
        {
            var playingSessions = Sessions.Where(i => i.NowPlayingItem is not null)
                .ToList();

            if (playingSessions.Count > 0)
            {
                var idle = playingSessions
                    .Where(i => (DateTime.UtcNow - i.LastPlaybackCheckIn).TotalMinutes > 5)
                    .ToList();

                foreach (var session in idle)
                {
                    _logger.LogDebug("Session {0} has gone idle while playing", session.Id);

                    try
                    {
                        await OnPlaybackStopped(new PlaybackStopInfo
                        {
                            Item = session.NowPlayingItem,
                            ItemId = session.NowPlayingItem is null ? Guid.Empty : session.NowPlayingItem.Id,
                            SessionId = session.Id,
                            MediaSourceId = session.PlayState?.MediaSourceId,
                            PositionTicks = session.PlayState?.PositionTicks
                        }).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error calling OnPlaybackStopped");
                    }
                }
            }
            else
            {
                StopIdleCheckTimer();
            }
        }

        private async void CheckForInactiveSteams(object state)
        {
            var inactiveSessions = Sessions.Where(i =>
                    i.NowPlayingItem is not null
                    && i.PlayState.IsPaused
                    && (DateTime.UtcNow - i.LastPausedDate).Value.TotalMinutes > _config.Configuration.InactiveSessionThreshold);

            foreach (var session in inactiveSessions)
            {
                _logger.LogDebug("Session {Session} has been inactive for {InactiveTime} minutes. Stopping it.", session.Id, _config.Configuration.InactiveSessionThreshold);

                try
                {
                    await SendPlaystateCommand(
                        session.Id,
                        session.Id,
                        new PlaystateRequest()
                        {
                            Command = PlaystateCommand.Stop,
                            ControllingUserId = session.UserId.ToString(),
                            SeekPositionTicks = session.PlayState?.PositionTicks
                        },
                        CancellationToken.None).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error calling SendPlaystateCommand for stopping inactive session {Session}.", session.Id);
                }
            }

            bool playingSessions = Sessions.Any(i => i.NowPlayingItem is not null);

            if (!playingSessions)
            {
                StopInactiveCheckTimer();
            }
        }

        private BaseItem GetNowPlayingItem(SessionInfo session, Guid itemId)
        {
            var item = session.FullNowPlayingItem;
            if (item is not null && item.Id.Equals(itemId))
            {
                return item;
            }

            item = _libraryManager.GetItemById(itemId);

            session.FullNowPlayingItem = item;

            return item;
        }

        /// <summary>
        /// Used to report that playback has started for an item.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException"><c>info</c> is <c>null</c>.</exception>
        public async Task OnPlaybackStart(PlaybackStartInfo info)
        {
            CheckDisposed();

            ArgumentNullException.ThrowIfNull(info);

            var session = GetSession(info.SessionId);

            var libraryItem = info.ItemId.IsEmpty()
                ? null
                : GetNowPlayingItem(session, info.ItemId);

            await UpdateNowPlayingItem(session, info, libraryItem, true).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(session.DeviceId) && info.PlayMethod != PlayMethod.Transcode)
            {
                ClearTranscodingInfo(session.DeviceId);
            }

            session.StartAutomaticProgress(info);

            var users = GetUsers(session);

            if (libraryItem is not null)
            {
                foreach (var user in users)
                {
                    OnPlaybackStart(user, libraryItem);
                }
            }

            var eventArgs = new PlaybackStartEventArgs
            {
                Item = libraryItem,
                Users = users,
                MediaSourceId = info.MediaSourceId,
                MediaInfo = info.Item,
                DeviceName = session.DeviceName,
                ClientName = session.Client,
                DeviceId = session.DeviceId,
                Session = session,
                PlaybackPositionTicks = info.PositionTicks,
                PlaySessionId = info.PlaySessionId
            };

            await _eventManager.PublishAsync(eventArgs).ConfigureAwait(false);

            // Nothing to save here
            // Fire events to inform plugins
            EventHelper.QueueEventIfNotNull(
                PlaybackStart,
                this,
                eventArgs,
                _logger);

            StartCheckTimers();
        }

        /// <summary>
        /// Called when [playback start].
        /// </summary>
        /// <param name="user">The user object.</param>
        /// <param name="item">The item.</param>
        private void OnPlaybackStart(User user, BaseItem item)
        {
            var data = _userDataManager.GetUserData(user, item);

            data.PlayCount++;
            data.LastPlayedDate = DateTime.UtcNow;

            if (item.SupportsPlayedStatus && !item.SupportsPositionTicksResume)
            {
                data.Played = true;
            }
            else
            {
                data.Played = false;
            }

            _userDataManager.SaveUserData(user, item, data, UserDataSaveReason.PlaybackStart, CancellationToken.None);
        }

        /// <inheritdoc />
        public Task OnPlaybackProgress(PlaybackProgressInfo info)
        {
            return OnPlaybackProgress(info, false);
        }

        /// <summary>
        /// Used to report playback progress for an item.
        /// </summary>
        /// <param name="info">The playback progress info.</param>
        /// <param name="isAutomated">Whether this is an automated update.</param>
        /// <returns>Task.</returns>
        public async Task OnPlaybackProgress(PlaybackProgressInfo info, bool isAutomated)
        {
            CheckDisposed();

            ArgumentNullException.ThrowIfNull(info);

            var session = GetSession(info.SessionId);

            var libraryItem = info.ItemId.IsEmpty()
                ? null
                : GetNowPlayingItem(session, info.ItemId);

            await UpdateNowPlayingItem(session, info, libraryItem, !isAutomated).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(session.DeviceId) && info.PlayMethod != PlayMethod.Transcode)
            {
                ClearTranscodingInfo(session.DeviceId);
            }

            var users = GetUsers(session);

            // only update saved user data on actual check-ins, not automated ones
            if (libraryItem is not null && !isAutomated)
            {
                foreach (var user in users)
                {
                    OnPlaybackProgress(user, libraryItem, info);
                }
            }

            var eventArgs = new PlaybackProgressEventArgs
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
                PlaySessionId = info.PlaySessionId,
                IsAutomated = isAutomated,
                Session = session
            };

            await _eventManager.PublishAsync(eventArgs).ConfigureAwait(false);

            PlaybackProgress?.Invoke(this, eventArgs);

            if (!isAutomated)
            {
                session.StartAutomaticProgress(info);
            }

            StartCheckTimers();
        }

        private void OnPlaybackProgress(User user, BaseItem item, PlaybackProgressInfo info)
        {
            var data = _userDataManager.GetUserData(user, item);

            var positionTicks = info.PositionTicks;

            var changed = false;

            if (positionTicks.HasValue)
            {
                _userDataManager.UpdatePlayState(item, data, positionTicks.Value);
                changed = true;
            }

            var tracksChanged = UpdatePlaybackSettings(user, info, data);
            if (!tracksChanged)
            {
                changed = true;
            }

            if (changed)
            {
                _userDataManager.SaveUserData(user, item, data, UserDataSaveReason.PlaybackProgress, CancellationToken.None);
            }
        }

        private static bool UpdatePlaybackSettings(User user, PlaybackProgressInfo info, UserItemData data)
        {
            var changed = false;

            if (user.RememberAudioSelections)
            {
                if (data.AudioStreamIndex != info.AudioStreamIndex)
                {
                    data.AudioStreamIndex = info.AudioStreamIndex;
                    changed = true;
                }
            }
            else
            {
                if (data.AudioStreamIndex.HasValue)
                {
                    data.AudioStreamIndex = null;
                    changed = true;
                }
            }

            if (user.RememberSubtitleSelections)
            {
                if (data.SubtitleStreamIndex != info.SubtitleStreamIndex)
                {
                    data.SubtitleStreamIndex = info.SubtitleStreamIndex;
                    changed = true;
                }
            }
            else
            {
                if (data.SubtitleStreamIndex.HasValue)
                {
                    data.SubtitleStreamIndex = null;
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>
        /// Used to report that playback has ended for an item.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException"><c>info</c> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><c>info.PositionTicks</c> is <c>null</c> or negative.</exception>
        public async Task OnPlaybackStopped(PlaybackStopInfo info)
        {
            CheckDisposed();

            ArgumentNullException.ThrowIfNull(info);

            if (info.PositionTicks.HasValue && info.PositionTicks.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(info), "The PlaybackStopInfo's PositionTicks was negative.");
            }

            var session = GetSession(info.SessionId);

            session.StopAutomaticProgress();

            var libraryItem = info.ItemId.IsEmpty()
                ? null
                : GetNowPlayingItem(session, info.ItemId);

            // Normalize
            if (string.IsNullOrEmpty(info.MediaSourceId))
            {
                info.MediaSourceId = info.ItemId.ToString("N", CultureInfo.InvariantCulture);
            }

            if (!info.ItemId.IsEmpty() && info.Item is null && libraryItem is not null)
            {
                var current = session.NowPlayingItem;

                if (current is null || !info.ItemId.Equals(current.Id))
                {
                    MediaSourceInfo mediaSource = null;

                    if (libraryItem is IHasMediaSources)
                    {
                        mediaSource = await GetMediaSource(libraryItem, info.MediaSourceId, info.LiveStreamId).ConfigureAwait(false);
                    }

                    info.Item = GetItemInfo(libraryItem, mediaSource);
                }
                else
                {
                    info.Item = current;
                }
            }

            if (info.Item is not null)
            {
                var msString = info.PositionTicks.HasValue ? (info.PositionTicks.Value / 10000).ToString(CultureInfo.InvariantCulture) : "unknown";

                _logger.LogInformation(
                    "Playback stopped reported by app {0} {1} playing {2}. Stopped at {3} ms",
                    session.Client,
                    session.ApplicationVersion,
                    info.Item.Name,
                    msString);
            }

            if (info.NowPlayingQueue is not null)
            {
                session.NowPlayingQueue = info.NowPlayingQueue;
            }

            session.PlaylistItemId = info.PlaylistItemId;

            RemoveNowPlayingItem(session);

            var users = GetUsers(session);
            var playedToCompletion = false;

            if (libraryItem is not null)
            {
                foreach (var user in users)
                {
                    playedToCompletion = OnPlaybackStopped(user, libraryItem, info.PositionTicks, info.Failed);
                }
            }

            if (!string.IsNullOrEmpty(info.LiveStreamId))
            {
                try
                {
                    await _mediaSourceManager.CloseLiveStream(info.LiveStreamId).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error closing live stream");
                }
            }

            var eventArgs = new PlaybackStopEventArgs
            {
                Item = libraryItem,
                Users = users,
                PlaybackPositionTicks = info.PositionTicks,
                PlayedToCompletion = playedToCompletion,
                MediaSourceId = info.MediaSourceId,
                MediaInfo = info.Item,
                DeviceName = session.DeviceName,
                ClientName = session.Client,
                DeviceId = session.DeviceId,
                Session = session,
                PlaySessionId = info.PlaySessionId
            };

            await _eventManager.PublishAsync(eventArgs).ConfigureAwait(false);

            EventHelper.QueueEventIfNotNull(PlaybackStopped, this, eventArgs, _logger);
        }

        private bool OnPlaybackStopped(User user, BaseItem item, long? positionTicks, bool playbackFailed)
        {
            if (playbackFailed)
            {
                return false;
            }

            var data = _userDataManager.GetUserData(user, item);
            bool playedToCompletion;
            if (positionTicks.HasValue)
            {
                playedToCompletion = _userDataManager.UpdatePlayState(item, data, positionTicks.Value);
            }
            else
            {
                // If the client isn't able to report this, then we'll just have to make an assumption
                data.PlayCount++;
                data.Played = item.SupportsPlayedStatus;
                data.PlaybackPositionTicks = 0;
                playedToCompletion = true;
            }

            _userDataManager.SaveUserData(user, item, data, UserDataSaveReason.PlaybackFinished, CancellationToken.None);

            return playedToCompletion;
        }

        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="throwOnMissing">if set to <c>true</c> [throw on missing].</param>
        /// <returns>SessionInfo.</returns>
        /// <exception cref="ResourceNotFoundException">
        /// No session with an Id equal to <c>sessionId</c> was found
        /// and <c>throwOnMissing</c> is <c>true</c>.
        /// </exception>
        private SessionInfo GetSession(string sessionId, bool throwOnMissing = true)
        {
            var session = Sessions.FirstOrDefault(i => string.Equals(i.Id, sessionId, StringComparison.Ordinal));
            if (session is null && throwOnMissing)
            {
                throw new ResourceNotFoundException(
                    string.Format(CultureInfo.InvariantCulture, "Session {0} not found.", sessionId));
            }

            return session;
        }

        private SessionInfo GetSessionToRemoteControl(string sessionId)
        {
            // Accept either device id or session id
            var session = Sessions.FirstOrDefault(i => string.Equals(i.Id, sessionId, StringComparison.Ordinal));

            if (session is null)
            {
                throw new ResourceNotFoundException(
                    string.Format(CultureInfo.InvariantCulture, "Session {0} not found.", sessionId));
            }

            return session;
        }

        private SessionInfoDto ToSessionInfoDto(SessionInfo sessionInfo)
        {
            return new SessionInfoDto
            {
                PlayState = sessionInfo.PlayState,
                AdditionalUsers = sessionInfo.AdditionalUsers,
                Capabilities = _deviceManager.ToClientCapabilitiesDto(sessionInfo.Capabilities),
                RemoteEndPoint = sessionInfo.RemoteEndPoint,
                PlayableMediaTypes = sessionInfo.PlayableMediaTypes,
                Id = sessionInfo.Id,
                UserId = sessionInfo.UserId,
                UserName = sessionInfo.UserName,
                Client = sessionInfo.Client,
                LastActivityDate = sessionInfo.LastActivityDate,
                LastPlaybackCheckIn = sessionInfo.LastPlaybackCheckIn,
                LastPausedDate = sessionInfo.LastPausedDate,
                DeviceName = sessionInfo.DeviceName,
                DeviceType = sessionInfo.DeviceType,
                NowPlayingItem = sessionInfo.NowPlayingItem,
                NowViewingItem = sessionInfo.NowViewingItem,
                DeviceId = sessionInfo.DeviceId,
                ApplicationVersion = sessionInfo.ApplicationVersion,
                TranscodingInfo = sessionInfo.TranscodingInfo,
                IsActive = sessionInfo.IsActive,
                SupportsMediaControl = sessionInfo.SupportsMediaControl,
                SupportsRemoteControl = sessionInfo.SupportsRemoteControl,
                NowPlayingQueue = sessionInfo.NowPlayingQueue,
                NowPlayingQueueFullItems = sessionInfo.NowPlayingQueueFullItems,
                HasCustomDeviceName = sessionInfo.HasCustomDeviceName,
                PlaylistItemId = sessionInfo.PlaylistItemId,
                ServerId = sessionInfo.ServerId,
                UserPrimaryImageTag = sessionInfo.UserPrimaryImageTag,
                SupportedCommands = sessionInfo.SupportedCommands
            };
        }

        /// <inheritdoc />
        public Task SendMessageCommand(string controllingSessionId, string sessionId, MessageCommand command, CancellationToken cancellationToken)
        {
            CheckDisposed();

            var generalCommand = new GeneralCommand
            {
                Name = GeneralCommandType.DisplayMessage
            };

            generalCommand.Arguments["Header"] = command.Header;
            generalCommand.Arguments["Text"] = command.Text;

            if (command.TimeoutMs.HasValue)
            {
                generalCommand.Arguments["TimeoutMs"] = command.TimeoutMs.Value.ToString(CultureInfo.InvariantCulture);
            }

            return SendGeneralCommand(controllingSessionId, sessionId, generalCommand, cancellationToken);
        }

        /// <inheritdoc />
        public Task SendGeneralCommand(string controllingSessionId, string sessionId, GeneralCommand command, CancellationToken cancellationToken)
        {
            CheckDisposed();

            var session = GetSessionToRemoteControl(sessionId);

            if (!string.IsNullOrEmpty(controllingSessionId))
            {
                var controllingSession = GetSession(controllingSessionId);
                AssertCanControl(session, controllingSession);
            }

            return SendMessageToSession(session, SessionMessageType.GeneralCommand, command, cancellationToken);
        }

        private static async Task SendMessageToSession<T>(SessionInfo session, SessionMessageType name, T data, CancellationToken cancellationToken)
        {
            var controllers = session.SessionControllers;
            var messageId = Guid.NewGuid();

            foreach (var controller in controllers)
            {
                await controller.SendMessage(name, messageId, data, cancellationToken).ConfigureAwait(false);
            }
        }

        private static Task SendMessageToSessions<T>(IEnumerable<SessionInfo> sessions, SessionMessageType name, T data, CancellationToken cancellationToken)
        {
            IEnumerable<Task> GetTasks()
            {
                var messageId = Guid.NewGuid();
                foreach (var session in sessions)
                {
                    var controllers = session.SessionControllers;
                    foreach (var controller in controllers)
                    {
                        yield return controller.SendMessage(name, messageId, data, cancellationToken);
                    }
                }
            }

            return Task.WhenAll(GetTasks());
        }

        /// <inheritdoc />
        public async Task SendPlayCommand(string controllingSessionId, string sessionId, PlayRequest command, CancellationToken cancellationToken)
        {
            CheckDisposed();

            var session = GetSessionToRemoteControl(sessionId);

            var user = session.UserId.IsEmpty() ? null : _userManager.GetUserById(session.UserId);

            List<BaseItem> items;

            if (command.PlayCommand == PlayCommand.PlayInstantMix)
            {
                items = command.ItemIds.SelectMany(i => TranslateItemForInstantMix(i, user))
                    .ToList();

                command.PlayCommand = PlayCommand.PlayNow;
            }
            else
            {
                var list = new List<BaseItem>();
                foreach (var itemId in command.ItemIds)
                {
                    var subItems = TranslateItemForPlayback(itemId, user);
                    list.AddRange(subItems);
                }

                items = list;
            }

            if (command.PlayCommand == PlayCommand.PlayShuffle)
            {
                items.Shuffle();
                command.PlayCommand = PlayCommand.PlayNow;
            }

            command.ItemIds = items.Select(i => i.Id).ToArray();

            if (user is not null)
            {
                if (items.Any(i => i.GetPlayAccess(user) != PlayAccess.Full))
                {
                    throw new ArgumentException(
                        string.Format(CultureInfo.InvariantCulture, "{0} is not allowed to play media.", user.Username));
                }
            }

            if (user is not null
                && command.ItemIds.Length == 1
                && user.EnableNextEpisodeAutoPlay
                && _libraryManager.GetItemById(command.ItemIds[0]) is Episode episode)
            {
                var series = episode.Series;
                if (series is not null)
                {
                    var episodes = series.GetEpisodes(
                            user,
                            new DtoOptions(false)
                            {
                                EnableImages = false
                            },
                            user.DisplayMissingEpisodes)
                        .Where(i => !i.IsVirtualItem)
                        .SkipWhile(i => !i.Id.Equals(episode.Id))
                        .ToList();

                    if (episodes.Count > 0)
                    {
                        command.ItemIds = episodes.Select(i => i.Id).ToArray();
                    }
                }
            }

            if (!string.IsNullOrEmpty(controllingSessionId))
            {
                var controllingSession = GetSession(controllingSessionId);
                AssertCanControl(session, controllingSession);
                if (!controllingSession.UserId.IsEmpty())
                {
                    command.ControllingUserId = controllingSession.UserId;
                }
            }

            await SendMessageToSession(session, SessionMessageType.Play, command, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task SendSyncPlayCommand(string sessionId, SendCommand command, CancellationToken cancellationToken)
        {
            CheckDisposed();
            var session = GetSession(sessionId);
            await SendMessageToSession(session, SessionMessageType.SyncPlayCommand, command, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task SendSyncPlayGroupUpdate<T>(string sessionId, GroupUpdate<T> command, CancellationToken cancellationToken)
        {
            CheckDisposed();
            var session = GetSession(sessionId);
            await SendMessageToSession(session, SessionMessageType.SyncPlayGroupUpdate, command, cancellationToken).ConfigureAwait(false);
        }

        private IEnumerable<BaseItem> TranslateItemForPlayback(Guid id, User user)
        {
            var item = _libraryManager.GetItemById(id);

            if (item is null)
            {
                _logger.LogError("A nonexistent item Id {0} was passed into TranslateItemForPlayback", id);
                return Array.Empty<BaseItem>();
            }

            if (item is IItemByName byName)
            {
                return byName.GetTaggedItems(new InternalItemsQuery(user)
                {
                    IsFolder = false,
                    Recursive = true,
                    DtoOptions = new DtoOptions(false)
                    {
                        EnableImages = false,
                        Fields = new[]
                        {
                            ItemFields.SortName
                        }
                    },
                    IsVirtualItem = false,
                    OrderBy = new[] { (ItemSortBy.SortName, SortOrder.Ascending) }
                });
            }

            if (item.IsFolder)
            {
                var folder = (Folder)item;

                return folder.GetItemList(new InternalItemsQuery(user)
                {
                    Recursive = true,
                    IsFolder = false,
                    DtoOptions = new DtoOptions(false)
                    {
                        EnableImages = false,
                        Fields = new ItemFields[]
                        {
                            ItemFields.SortName
                        }
                    },
                    IsVirtualItem = false,
                    OrderBy = new[] { (ItemSortBy.SortName, SortOrder.Ascending) }
                });
            }

            return new[] { item };
        }

        private List<BaseItem> TranslateItemForInstantMix(Guid id, User user)
        {
            var item = _libraryManager.GetItemById(id);

            if (item is null)
            {
                _logger.LogError("A nonexistent item Id {0} was passed into TranslateItemForInstantMix", id);
                return new List<BaseItem>();
            }

            return _musicManager.GetInstantMixFromItem(item, user, new DtoOptions(false) { EnableImages = false }).ToList();
        }

        /// <inheritdoc />
        public Task SendBrowseCommand(string controllingSessionId, string sessionId, BrowseRequest command, CancellationToken cancellationToken)
        {
            var generalCommand = new GeneralCommand
            {
                Name = GeneralCommandType.DisplayContent,
                Arguments =
                {
                    ["ItemId"] = command.ItemId,
                    ["ItemName"] = command.ItemName,
                    ["ItemType"] = command.ItemType.ToString()
                }
            };

            return SendGeneralCommand(controllingSessionId, sessionId, generalCommand, cancellationToken);
        }

        /// <inheritdoc />
        public Task SendPlaystateCommand(string controllingSessionId, string sessionId, PlaystateRequest command, CancellationToken cancellationToken)
        {
            CheckDisposed();

            var session = GetSessionToRemoteControl(sessionId);

            if (!string.IsNullOrEmpty(controllingSessionId))
            {
                var controllingSession = GetSession(controllingSessionId);
                AssertCanControl(session, controllingSession);
                if (!controllingSession.UserId.IsEmpty())
                {
                    command.ControllingUserId = controllingSession.UserId.ToString("N", CultureInfo.InvariantCulture);
                }
            }

            return SendMessageToSession(session, SessionMessageType.Playstate, command, cancellationToken);
        }

        private static void AssertCanControl(SessionInfo session, SessionInfo controllingSession)
        {
            ArgumentNullException.ThrowIfNull(session);

            ArgumentNullException.ThrowIfNull(controllingSession);
        }

        /// <summary>
        /// Sends the restart required message.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendRestartRequiredNotification(CancellationToken cancellationToken)
        {
            CheckDisposed();

            return SendMessageToSessions(Sessions, SessionMessageType.RestartRequired, string.Empty, cancellationToken);
        }

        /// <summary>
        /// Adds the additional user.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <exception cref="UnauthorizedAccessException">Cannot modify additional users without authenticating first.</exception>
        /// <exception cref="ArgumentException">The requested user is already the primary user of the session.</exception>
        public void AddAdditionalUser(string sessionId, Guid userId)
        {
            CheckDisposed();

            var session = GetSession(sessionId);

            if (session.UserId.Equals(userId))
            {
                throw new ArgumentException("The requested user is already the primary user of the session.");
            }

            if (session.AdditionalUsers.All(i => !i.UserId.Equals(userId)))
            {
                var user = _userManager.GetUserById(userId);
                var newUser = new SessionUserInfo
                {
                    UserId = userId,
                    UserName = user.Username
                };

                session.AdditionalUsers = [.. session.AdditionalUsers, newUser];
            }
        }

        /// <summary>
        /// Removes the additional user.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <exception cref="UnauthorizedAccessException">Cannot modify additional users without authenticating first.</exception>
        /// <exception cref="ArgumentException">The requested user is already the primary user of the session.</exception>
        public void RemoveAdditionalUser(string sessionId, Guid userId)
        {
            CheckDisposed();

            var session = GetSession(sessionId);

            if (session.UserId.Equals(userId))
            {
                throw new ArgumentException("The requested user is already the primary user of the session.");
            }

            var user = session.AdditionalUsers.FirstOrDefault(i => i.UserId.Equals(userId));

            if (user is not null)
            {
                var list = session.AdditionalUsers.ToList();
                list.Remove(user);

                session.AdditionalUsers = list.ToArray();
            }
        }

        /// <summary>
        /// Authenticates the new session.
        /// </summary>
        /// <param name="request">The authenticationrequest.</param>
        /// <returns>The authentication result.</returns>
        public Task<AuthenticationResult> AuthenticateNewSession(AuthenticationRequest request)
        {
            return AuthenticateNewSessionInternal(request, true);
        }

        /// <summary>
        /// Directly authenticates the session without enforcing password.
        /// </summary>
        /// <param name="request">The authentication request.</param>
        /// <returns>The authentication result.</returns>
        public Task<AuthenticationResult> AuthenticateDirect(AuthenticationRequest request)
        {
            return AuthenticateNewSessionInternal(request, false);
        }

        internal async Task<AuthenticationResult> AuthenticateNewSessionInternal(AuthenticationRequest request, bool enforcePassword)
        {
            CheckDisposed();

            ArgumentException.ThrowIfNullOrEmpty(request.App);
            ArgumentException.ThrowIfNullOrEmpty(request.DeviceId);
            ArgumentException.ThrowIfNullOrEmpty(request.DeviceName);
            ArgumentException.ThrowIfNullOrEmpty(request.AppVersion);

            User user = null;
            if (!request.UserId.IsEmpty())
            {
                user = _userManager.GetUserById(request.UserId);
            }

            user ??= _userManager.GetUserByName(request.Username);

            if (enforcePassword)
            {
                user = await _userManager.AuthenticateUser(
                    request.Username,
                    request.Password,
                    request.RemoteEndPoint,
                    true).ConfigureAwait(false);
            }

            if (user is null)
            {
                await _eventManager.PublishAsync(new AuthenticationRequestEventArgs(request)).ConfigureAwait(false);
                throw new AuthenticationException("Invalid username or password entered.");
            }

            if (!string.IsNullOrEmpty(request.DeviceId)
                && !_deviceManager.CanAccessDevice(user, request.DeviceId))
            {
                throw new SecurityException("User is not allowed access from this device.");
            }

            int sessionsCount = Sessions.Count(i => i.UserId.Equals(user.Id));
            int maxActiveSessions = user.MaxActiveSessions;
            _logger.LogInformation("Current/Max sessions for user {User}: {Sessions}/{Max}", user.Username, sessionsCount, maxActiveSessions);
            if (maxActiveSessions >= 1 && sessionsCount >= maxActiveSessions)
            {
                throw new SecurityException("User is at their maximum number of sessions.");
            }

            var token = await GetAuthorizationToken(user, request.DeviceId, request.App, request.AppVersion, request.DeviceName).ConfigureAwait(false);

            var session = await LogSessionActivity(
                request.App,
                request.AppVersion,
                request.DeviceId,
                request.DeviceName,
                request.RemoteEndPoint,
                user).ConfigureAwait(false);

            var returnResult = new AuthenticationResult
            {
                User = _userManager.GetUserDto(user, request.RemoteEndPoint),
                SessionInfo = ToSessionInfoDto(session),
                AccessToken = token,
                ServerId = _appHost.SystemId
            };

            await _eventManager.PublishAsync(new AuthenticationResultEventArgs(returnResult)).ConfigureAwait(false);
            return returnResult;
        }

        internal async Task<string> GetAuthorizationToken(User user, string deviceId, string app, string appVersion, string deviceName)
        {
            // This should be validated above, but if it isn't don't delete all tokens.
            ArgumentException.ThrowIfNullOrEmpty(deviceId);

            var existing = _deviceManager.GetDevices(
                new DeviceQuery
                {
                    DeviceId = deviceId,
                    UserId = user.Id
                }).Items;

            foreach (var auth in existing)
            {
                try
                {
                    // Logout any existing sessions for the user on this device
                    await Logout(auth).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while logging out existing session.");
                }
            }

            _logger.LogInformation("Creating new access token for user {0}", user.Id);
            var device = await _deviceManager.CreateDevice(new Device(user.Id, app, appVersion, deviceName, deviceId)).ConfigureAwait(false);

            return device.AccessToken;
        }

        /// <inheritdoc />
        public async Task Logout(string accessToken)
        {
            CheckDisposed();

            ArgumentException.ThrowIfNullOrEmpty(accessToken);

            var existing = _deviceManager.GetDevices(
                new DeviceQuery
                {
                    Limit = 1,
                    AccessToken = accessToken
                }).Items;

            if (existing.Count > 0)
            {
                await Logout(existing[0]).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task Logout(Device device)
        {
            CheckDisposed();

            _logger.LogInformation("Logging out access token {0}", device.AccessToken);

            await _deviceManager.DeleteDevice(device).ConfigureAwait(false);

            var sessions = Sessions
                .Where(i => string.Equals(i.DeviceId, device.DeviceId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var session in sessions)
            {
                try
                {
                    await ReportSessionEnded(session.Id).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reporting session ended");
                }
            }
        }

        /// <inheritdoc />
        public async Task RevokeUserTokens(Guid userId, string currentAccessToken)
        {
            CheckDisposed();

            var existing = _deviceManager.GetDevices(new DeviceQuery
            {
                UserId = userId
            });

            foreach (var info in existing.Items)
            {
                if (!string.Equals(currentAccessToken, info.AccessToken, StringComparison.OrdinalIgnoreCase))
                {
                    await Logout(info).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Reports the capabilities.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="capabilities">The capabilities.</param>
        public void ReportCapabilities(string sessionId, ClientCapabilities capabilities)
        {
            CheckDisposed();

            var session = GetSession(sessionId);

            ReportCapabilities(session, capabilities, true);
        }

        private void ReportCapabilities(
            SessionInfo session,
            ClientCapabilities capabilities,
            bool saveCapabilities)
        {
            session.Capabilities = capabilities;

            if (saveCapabilities)
            {
                CapabilitiesChanged?.Invoke(
                    this,
                    new SessionEventArgs
                    {
                        SessionInfo = session
                    });

                _deviceManager.SaveCapabilities(session.DeviceId, capabilities);
            }
        }

        /// <summary>
        /// Converts a BaseItem to a BaseItemInfo.
        /// </summary>
        private BaseItemDto GetItemInfo(BaseItem item, MediaSourceInfo mediaSource)
        {
            ArgumentNullException.ThrowIfNull(item);

            var dtoOptions = _itemInfoDtoOptions;

            if (_itemInfoDtoOptions is null)
            {
                dtoOptions = new DtoOptions
                {
                    AddProgramRecordingInfo = false
                };

                var fields = dtoOptions.Fields.ToList();

                fields.Remove(ItemFields.CanDelete);
                fields.Remove(ItemFields.CanDownload);
                fields.Remove(ItemFields.ChildCount);
                fields.Remove(ItemFields.CustomRating);
                fields.Remove(ItemFields.DateLastMediaAdded);
                fields.Remove(ItemFields.DateLastRefreshed);
                fields.Remove(ItemFields.DateLastSaved);
                fields.Remove(ItemFields.DisplayPreferencesId);
                fields.Remove(ItemFields.Etag);
                fields.Remove(ItemFields.InheritedParentalRatingValue);
                fields.Remove(ItemFields.ItemCounts);
                fields.Remove(ItemFields.MediaSourceCount);
                fields.Remove(ItemFields.MediaStreams);
                fields.Remove(ItemFields.MediaSources);
                fields.Remove(ItemFields.People);
                fields.Remove(ItemFields.PlayAccess);
                fields.Remove(ItemFields.People);
                fields.Remove(ItemFields.ProductionLocations);
                fields.Remove(ItemFields.RecursiveItemCount);
                fields.Remove(ItemFields.RemoteTrailers);
                fields.Remove(ItemFields.SeasonUserData);
                fields.Remove(ItemFields.Settings);
                fields.Remove(ItemFields.SortName);
                fields.Remove(ItemFields.Tags);
                fields.Remove(ItemFields.ExtraIds);

                dtoOptions.Fields = fields.ToArray();

                _itemInfoDtoOptions = dtoOptions;
            }

            var info = _dtoService.GetBaseItemDto(item, dtoOptions);

            if (mediaSource is not null)
            {
                info.MediaStreams = mediaSource.MediaStreams.ToArray();
            }

            return info;
        }

        private string GetImageCacheTag(User user)
        {
            try
            {
                return _imageProcessor.GetImageCacheTag(user);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error getting image information for profile image");
                return null;
            }
        }

        /// <inheritdoc />
        public void ReportNowViewingItem(string sessionId, string itemId)
        {
            ArgumentException.ThrowIfNullOrEmpty(itemId);

            var item = _libraryManager.GetItemById(new Guid(itemId));
            var session = GetSession(sessionId);

            session.NowViewingItem = GetItemInfo(item, null);
        }

        /// <inheritdoc />
        public void ReportTranscodingInfo(string deviceId, TranscodingInfo info)
        {
            var session = Sessions.FirstOrDefault(i =>
                string.Equals(i.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));

            if (session is not null)
            {
                session.TranscodingInfo = info;
            }
        }

        /// <inheritdoc />
        public void ClearTranscodingInfo(string deviceId)
        {
            ReportTranscodingInfo(deviceId, null);
        }

        /// <inheritdoc />
        public SessionInfo GetSession(string deviceId, string client, string version)
        {
            return Sessions.FirstOrDefault(i =>
                string.Equals(i.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(i.Client, client, StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc />
        public Task<SessionInfo> GetSessionByAuthenticationToken(Device info, string deviceId, string remoteEndpoint, string appVersion)
        {
            ArgumentNullException.ThrowIfNull(info);

            var user = info.UserId.IsEmpty()
                ? null
                : _userManager.GetUserById(info.UserId);

            appVersion = string.IsNullOrEmpty(appVersion)
                ? info.AppVersion
                : appVersion;

            var deviceName = info.DeviceName;
            var appName = info.AppName;

            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = info.DeviceId;
            }

            // Prevent argument exception
            if (string.IsNullOrEmpty(appVersion))
            {
                appVersion = "1";
            }

            return LogSessionActivity(appName, appVersion, deviceId, deviceName, remoteEndpoint, user);
        }

        /// <inheritdoc />
        public async Task<SessionInfo> GetSessionByAuthenticationToken(string token, string deviceId, string remoteEndpoint)
        {
            var items = _deviceManager.GetDevices(new DeviceQuery
            {
                AccessToken = token,
                Limit = 1
            }).Items;

            if (items.Count == 0)
            {
                return null;
            }

            return await GetSessionByAuthenticationToken(items[0], deviceId, remoteEndpoint, null).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public IReadOnlyList<SessionInfoDto> GetSessions(
            Guid userId,
            string deviceId,
            int? activeWithinSeconds,
            Guid? controllableUserToCheck,
            bool isApiKey)
        {
            var result = Sessions;
            if (!string.IsNullOrEmpty(deviceId))
            {
                result = result.Where(i => string.Equals(i.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));
            }

            var userCanControlOthers = false;
            var userIsAdmin = false;
            User user = null;

            if (isApiKey)
            {
                userCanControlOthers = true;
                userIsAdmin = true;
            }
            else if (!userId.IsEmpty())
            {
                user = _userManager.GetUserById(userId);
                if (user is not null)
                {
                    userCanControlOthers = user.HasPermission(PermissionKind.EnableRemoteControlOfOtherUsers);
                    userIsAdmin = user.HasPermission(PermissionKind.IsAdministrator);
                }
                else
                {
                    return [];
                }
            }

            if (!controllableUserToCheck.IsNullOrEmpty())
            {
                result = result.Where(i => i.SupportsRemoteControl);

                var controlledUser = _userManager.GetUserById(controllableUserToCheck.Value);
                if (controlledUser is null)
                {
                    return [];
                }

                if (!controlledUser.HasPermission(PermissionKind.EnableSharedDeviceControl))
                {
                    // Controlled user has device sharing disabled
                    result = result.Where(i => !i.UserId.IsEmpty());
                }

                if (!userCanControlOthers)
                {
                    // User cannot control other user's sessions, validate user id.
                    result = result.Where(i => i.UserId.IsEmpty() || i.ContainsUser(userId));
                }

                result = result.Where(i =>
                {
                    if (isApiKey)
                    {
                        return true;
                    }

                    if (user is null)
                    {
                        return false;
                    }

                    return string.IsNullOrWhiteSpace(i.DeviceId) || _deviceManager.CanAccessDevice(user, i.DeviceId);
                });
            }
            else if (!userIsAdmin)
            {
                // Request isn't from administrator, limit to "own" sessions.
                result = result.Where(i => i.UserId.IsEmpty() || i.ContainsUser(userId));
            }

            if (!userIsAdmin)
            {
                // Don't report acceleration type for non-admin users.
                result = result.Select(r =>
                {
                    if (r.TranscodingInfo is not null)
                    {
                        r.TranscodingInfo.HardwareAccelerationType = HardwareAccelerationType.none;
                    }

                    return r;
                });
            }

            if (activeWithinSeconds.HasValue && activeWithinSeconds.Value > 0)
            {
                var minActiveDate = DateTime.UtcNow.AddSeconds(0 - activeWithinSeconds.Value);
                result = result.Where(i => i.LastActivityDate >= minActiveDate);
            }

            return result.Select(ToSessionInfoDto).ToList();
        }

        /// <inheritdoc />
        public Task SendMessageToAdminSessions<T>(SessionMessageType name, T data, CancellationToken cancellationToken)
        {
            CheckDisposed();

            var adminUserIds = _userManager.Users
                .Where(i => i.HasPermission(PermissionKind.IsAdministrator))
                .Select(i => i.Id)
                .ToList();

            return SendMessageToUserSessions(adminUserIds, name, data, cancellationToken);
        }

        /// <inheritdoc />
        public Task SendMessageToUserSessions<T>(List<Guid> userIds, SessionMessageType name, Func<T> dataFn, CancellationToken cancellationToken)
        {
            CheckDisposed();

            var sessions = Sessions.Where(i => userIds.Any(i.ContainsUser)).ToList();

            if (sessions.Count == 0)
            {
                return Task.CompletedTask;
            }

            return SendMessageToSessions(sessions, name, dataFn(), cancellationToken);
        }

        /// <inheritdoc />
        public Task SendMessageToUserSessions<T>(List<Guid> userIds, SessionMessageType name, T data, CancellationToken cancellationToken)
        {
            CheckDisposed();

            var sessions = Sessions.Where(i => userIds.Any(i.ContainsUser));
            return SendMessageToSessions(sessions, name, data, cancellationToken);
        }

        /// <inheritdoc />
        public Task SendMessageToUserDeviceSessions<T>(string deviceId, SessionMessageType name, T data, CancellationToken cancellationToken)
        {
            CheckDisposed();

            var sessions = Sessions.Where(i => string.Equals(i.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));

            return SendMessageToSessions(sessions, name, data, cancellationToken);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            foreach (var session in _activeConnections.Values)
            {
                await session.DisposeAsync().ConfigureAwait(false);
            }

            if (_idleTimer is not null)
            {
                await _idleTimer.DisposeAsync().ConfigureAwait(false);
                _idleTimer = null;
            }

            if (_inactiveTimer is not null)
            {
                await _inactiveTimer.DisposeAsync().ConfigureAwait(false);
                _inactiveTimer = null;
            }

            await _shutdownCallback.DisposeAsync().ConfigureAwait(false);

            _deviceManager.DeviceOptionsUpdated -= OnDeviceManagerDeviceOptionsUpdated;
            _disposed = true;
        }

        private async void OnApplicationStopping()
        {
            _logger.LogInformation("Sending shutdown notifications");
            try
            {
                var messageType = _appHost.ShouldRestart ? SessionMessageType.ServerRestarting : SessionMessageType.ServerShuttingDown;

                await SendMessageToSessions(Sessions, messageType, string.Empty, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending server shutdown notifications");
            }

            // Close open websockets to allow Kestrel to shut down cleanly
            foreach (var session in _activeConnections.Values)
            {
                await session.DisposeAsync().ConfigureAwait(false);
            }

            _activeConnections.Clear();
        }
    }
}
