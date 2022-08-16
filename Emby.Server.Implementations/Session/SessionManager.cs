#nullable disable

#pragma warning disable CS1591

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
using Microsoft.Extensions.Logging;
using Episode = MediaBrowser.Controller.Entities.TV.Episode;

namespace Emby.Server.Implementations.Session
{
    /// <summary>
    /// Class SessionManager.
    /// </summary>
    public class SessionManager : ISessionManager, IDisposable
    {
        private readonly IUserDataManager _userDataManager;
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
        private readonly IServerConfigurationManager _config;

        /// <summary>
        /// The active connections.
        /// </summary>
        private readonly ConcurrentDictionary<string, SessionInfo> _activeConnections = new(StringComparer.OrdinalIgnoreCase);

        private Timer _idleTimer;

        private DtoOptions _itemInfoDtoOptions;
        private bool _disposed = false;

        public SessionManager(
            ILogger<SessionManager> logger,
            IEventManager eventManager,
            IUserDataManager userDataManager,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IMusicManager musicManager,
            IDtoService dtoService,
            IImageProcessor imageProcessor,
            IServerApplicationHost appHost,
            IDeviceManager deviceManager,
            IMediaSourceManager mediaSourceManager,
            IServerConfigurationManager config)
        {
            _logger = logger;
            _eventManager = eventManager;
            _userDataManager = userDataManager;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _musicManager = musicManager;
            _dtoService = dtoService;
            _imageProcessor = imageProcessor;
            _appHost = appHost;
            _deviceManager = deviceManager;
            _mediaSourceManager = mediaSourceManager;
            _config = config;

            _deviceManager.DeviceOptionsUpdated += OnDeviceManagerDeviceOptionsUpdated;
        }

        /// <inheritdoc />
        public event EventHandler<GenericEventArgs<AuthenticationRequest>> AuthenticationFailed;

        /// <inheritdoc />
        public event EventHandler<GenericEventArgs<AuthenticationResult>> AuthenticationSucceeded;

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

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _idleTimer?.Dispose();
            }

            _idleTimer = null;

            _deviceManager.DeviceOptionsUpdated -= OnDeviceManagerDeviceOptionsUpdated;

            _disposed = true;
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private void OnSessionStarted(SessionInfo info)
        {
            if (!string.IsNullOrEmpty(info.DeviceId))
            {
                var capabilities = _deviceManager.GetCapabilities(info.DeviceId);

                if (capabilities != null)
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

        private void OnSessionEnded(SessionInfo info)
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

            info.Dispose();
        }

        /// <inheritdoc />
        public void UpdateDeviceName(string sessionId, string reportedDeviceName)
        {
            var session = GetSession(sessionId);
            if (session != null)
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

            if (string.IsNullOrEmpty(appName))
            {
                throw new ArgumentNullException(nameof(appName));
            }

            if (string.IsNullOrEmpty(appVersion))
            {
                throw new ArgumentNullException(nameof(appVersion));
            }

            if (string.IsNullOrEmpty(deviceId))
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            var activityDate = DateTime.UtcNow;
            var session = await GetSessionInfo(appName, appVersion, deviceId, deviceName, remoteEndPoint, user).ConfigureAwait(false);
            var lastActivityDate = session.LastActivityDate;
            session.LastActivityDate = activityDate;

            if (user != null)
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

                OnSessionEnded(session);
            }
        }

        /// <inheritdoc />
        public void ReportSessionEnded(string sessionId)
        {
            CheckDisposed();
            var session = GetSession(sessionId, false);

            if (session != null)
            {
                var key = GetSessionKey(session.Client, session.DeviceId);

                _activeConnections.TryRemove(key, out _);

                OnSessionEnded(session);
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

            if (!info.ItemId.Equals(default) && info.Item == null && libraryItem != null)
            {
                var current = session.NowPlayingItem;

                if (current == null || !info.ItemId.Equals(current.Id))
                {
                    var runtimeTicks = libraryItem.RunTimeTicks;

                    MediaSourceInfo mediaSource = null;
                    if (libraryItem is IHasMediaSources)
                    {
                        mediaSource = await GetMediaSource(libraryItem, info.MediaSourceId, info.LiveStreamId).ConfigureAwait(false);

                        if (mediaSource != null)
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
            session.PlaylistItemId = info.PlaylistItemId;

            var nowPlayingQueue = info.NowPlayingQueue;

            if (nowPlayingQueue?.Length > 0)
            {
                session.NowPlayingQueue = nowPlayingQueue;

                var itemIds = nowPlayingQueue.Select(queue => queue.Id).ToArray();
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
        private async Task<SessionInfo> GetSessionInfo(
            string appName,
            string appVersion,
            string deviceId,
            string deviceName,
            string remoteEndPoint,
            User user)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(deviceId))
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            var key = GetSessionKey(appName, deviceId);

            CheckDisposed();

            if (!_activeConnections.TryGetValue(key, out var sessionInfo))
            {
                _activeConnections[key] = await CreateSession(key, appName, appVersion, deviceId, deviceName, remoteEndPoint, user).ConfigureAwait(false);
                sessionInfo = _activeConnections[key];
            }

            sessionInfo.UserId = user?.Id ?? Guid.Empty;
            sessionInfo.UserName = user?.Username;
            sessionInfo.UserPrimaryImageTag = user?.ProfileImage == null ? null : GetImageCacheTag(user);
            sessionInfo.RemoteEndPoint = remoteEndPoint;
            sessionInfo.Client = appName;

            if (!sessionInfo.HasCustomDeviceName || string.IsNullOrEmpty(sessionInfo.DeviceName))
            {
                sessionInfo.DeviceName = deviceName;
            }

            sessionInfo.ApplicationVersion = appVersion;

            if (user == null)
            {
                sessionInfo.AdditionalUsers = Array.Empty<SessionUserInfo>();
            }

            return sessionInfo;
        }

        private async Task<SessionInfo> CreateSession(
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
            sessionInfo.UserPrimaryImageTag = user?.ProfileImage == null ? null : GetImageCacheTag(user);
            sessionInfo.RemoteEndPoint = remoteEndPoint;

            if (string.IsNullOrEmpty(deviceName))
            {
                deviceName = "Network Device";
            }

            var deviceOptions = await _deviceManager.GetDeviceOptions(deviceId).ConfigureAwait(false);
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

            if (session.UserId.Equals(default))
            {
                return users;
            }

            var user = _userManager.GetUserById(session.UserId);

            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            users.Add(user);

            users.AddRange(session.AdditionalUsers
                .Select(i => _userManager.GetUserById(i.UserId))
                .Where(i => i != null));

            return users;
        }

        private void StartIdleCheckTimer()
        {
            _idleTimer ??= new Timer(CheckForIdlePlayback, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
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
                    _logger.LogDebug("Session {0} has gone idle while playing", session.Id);

                    try
                    {
                        await OnPlaybackStopped(new PlaybackStopInfo
                        {
                            Item = session.NowPlayingItem,
                            ItemId = session.NowPlayingItem == null ? Guid.Empty : session.NowPlayingItem.Id,
                            SessionId = session.Id,
                            MediaSourceId = session.PlayState?.MediaSourceId,
                            PositionTicks = session.PlayState?.PositionTicks
                        }).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("Error calling OnPlaybackStopped", ex);
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

        private BaseItem GetNowPlayingItem(SessionInfo session, Guid itemId)
        {
            var item = session.FullNowPlayingItem;
            if (item != null && item.Id.Equals(itemId))
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

            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            var session = GetSession(info.SessionId);

            var libraryItem = info.ItemId.Equals(default)
                ? null
                : GetNowPlayingItem(session, info.ItemId);

            await UpdateNowPlayingItem(session, info, libraryItem, true).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(session.DeviceId) && info.PlayMethod != PlayMethod.Transcode)
            {
                ClearTranscodingInfo(session.DeviceId);
            }

            session.StartAutomaticProgress(info);

            var users = GetUsers(session);

            if (libraryItem != null)
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

            StartIdleCheckTimer();
        }

        /// <summary>
        /// Called when [playback start].
        /// </summary>
        /// <param name="user">The user object.</param>
        /// <param name="item">The item.</param>
        private void OnPlaybackStart(User user, BaseItem item)
        {
            var data = _userDataManager.GetUserData(user, item);

            if (!_config.Configuration.UpdateLastPlayedAndPlayCountOnPlayCompletion)
            {
                data.PlayCount++;
                data.LastPlayedDate = DateTime.UtcNow;
            }

            if (item.SupportsPlayedStatus)
            {
                if (!item.SupportsPositionTicksResume)
                {
                    data.Played = true;
                }
                else if (_config.Configuration.MarkResumableItemUnplayedOnPlay)
                {
                    data.Played = false;
                }
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

            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            var session = GetSession(info.SessionId);

            var libraryItem = info.ItemId.Equals(default)
                ? null
                : GetNowPlayingItem(session, info.ItemId);

            await UpdateNowPlayingItem(session, info, libraryItem, !isAutomated).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(session.DeviceId) && info.PlayMethod != PlayMethod.Transcode)
            {
                ClearTranscodingInfo(session.DeviceId);
            }

            var users = GetUsers(session);

            // only update saved user data on actual check-ins, not automated ones
            if (libraryItem != null && !isAutomated)
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

            StartIdleCheckTimer();
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

            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            if (info.PositionTicks.HasValue && info.PositionTicks.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(info), "The PlaybackStopInfo's PositionTicks was negative.");
            }

            var session = GetSession(info.SessionId);

            session.StopAutomaticProgress();

            var libraryItem = info.ItemId.Equals(default)
                ? null
                : GetNowPlayingItem(session, info.ItemId);

            // Normalize
            if (string.IsNullOrEmpty(info.MediaSourceId))
            {
                info.MediaSourceId = info.ItemId.ToString("N", CultureInfo.InvariantCulture);
            }

            if (!info.ItemId.Equals(default) && info.Item == null && libraryItem != null)
            {
                var current = session.NowPlayingItem;

                if (current == null || !info.ItemId.Equals(current.Id))
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

            if (info.Item != null)
            {
                var msString = info.PositionTicks.HasValue ? (info.PositionTicks.Value / 10000).ToString(CultureInfo.InvariantCulture) : "unknown";

                _logger.LogInformation(
                    "Playback stopped reported by app {0} {1} playing {2}. Stopped at {3} ms",
                    session.Client,
                    session.ApplicationVersion,
                    info.Item.Name,
                    msString);
            }

            if (info.NowPlayingQueue != null)
            {
                session.NowPlayingQueue = info.NowPlayingQueue;
            }

            session.PlaylistItemId = info.PlaylistItemId;

            RemoveNowPlayingItem(session);

            var users = GetUsers(session);
            var playedToCompletion = false;

            if (libraryItem != null)
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
                    _logger.LogError("Error closing live stream", ex);
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
            bool playedToCompletion = false;

            if (!playbackFailed)
            {
                var data = _userDataManager.GetUserData(user, item);

                if (positionTicks.HasValue)
                {
                    playedToCompletion = _userDataManager.UpdatePlayState(item, data, positionTicks.Value);
                    if (playedToCompletion && _config.Configuration.UpdateLastPlayedAndPlayCountOnPlayCompletion)
                    {
                        data.PlayCount++;
                        data.LastPlayedDate = DateTime.UtcNow;
                    }
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
            }

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
            if (session == null && throwOnMissing)
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

            if (session == null)
            {
                throw new ResourceNotFoundException(
                    string.Format(CultureInfo.InvariantCulture, "Session {0} not found.", sessionId));
            }

            return session;
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

            var user = session.UserId.Equals(default) ? null : _userManager.GetUserById(session.UserId);

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

            if (user != null)
            {
                if (items.Any(i => i.GetPlayAccess(user) != PlayAccess.Full))
                {
                    throw new ArgumentException(
                        string.Format(CultureInfo.InvariantCulture, "{0} is not allowed to play media.", user.Username));
                }
            }

            if (user != null
                && command.ItemIds.Length == 1
                && user.EnableNextEpisodeAutoPlay
                && _libraryManager.GetItemById(command.ItemIds[0]) is Episode episode)
            {
                var series = episode.Series;
                if (series != null)
                {
                    var episodes = series.GetEpisodes(
                            user,
                            new DtoOptions(false)
                            {
                                EnableImages = false
                            })
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
                if (!controllingSession.UserId.Equals(default))
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

            if (item == null)
            {
                _logger.LogError("A non-existant item Id {0} was passed into TranslateItemForPlayback", id);
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

        private IEnumerable<BaseItem> TranslateItemForInstantMix(Guid id, User user)
        {
            var item = _libraryManager.GetItemById(id);

            if (item == null)
            {
                _logger.LogError("A non-existent item Id {0} was passed into TranslateItemForInstantMix", id);
                return new List<BaseItem>();
            }

            return _musicManager.GetInstantMixFromItem(item, user, new DtoOptions(false) { EnableImages = false });
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
                if (!controllingSession.UserId.Equals(default))
                {
                    command.ControllingUserId = controllingSession.UserId.ToString("N", CultureInfo.InvariantCulture);
                }
            }

            return SendMessageToSession(session, SessionMessageType.Playstate, command, cancellationToken);
        }

        private static void AssertCanControl(SessionInfo session, SessionInfo controllingSession)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (controllingSession == null)
            {
                throw new ArgumentNullException(nameof(controllingSession));
            }
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
        /// Sends the server shutdown notification.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendServerShutdownNotification(CancellationToken cancellationToken)
        {
            CheckDisposed();

            return SendMessageToSessions(Sessions, SessionMessageType.ServerShuttingDown, string.Empty, cancellationToken);
        }

        /// <summary>
        /// Sends the server restart notification.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendServerRestartNotification(CancellationToken cancellationToken)
        {
            CheckDisposed();

            _logger.LogDebug("Beginning SendServerRestartNotification");

            return SendMessageToSessions(Sessions, SessionMessageType.ServerRestarting, string.Empty, cancellationToken);
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

                var list = session.AdditionalUsers.ToList();

                list.Add(new SessionUserInfo
                {
                    UserId = userId,
                    UserName = user.Username
                });

                session.AdditionalUsers = list.ToArray();
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

            if (user != null)
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

        private async Task<AuthenticationResult> AuthenticateNewSessionInternal(AuthenticationRequest request, bool enforcePassword)
        {
            CheckDisposed();

            User user = null;
            if (!request.UserId.Equals(default))
            {
                user = _userManager.GetUserById(request.UserId);
            }

            user ??= _userManager.GetUserByName(request.Username);

            if (enforcePassword)
            {
                user = await _userManager.AuthenticateUser(
                    request.Username,
                    request.Password,
                    null,
                    request.RemoteEndPoint,
                    true).ConfigureAwait(false);
            }

            if (user == null)
            {
                AuthenticationFailed?.Invoke(this, new GenericEventArgs<AuthenticationRequest>(request));
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
                SessionInfo = session,
                AccessToken = token,
                ServerId = _appHost.SystemId
            };

            AuthenticationSucceeded?.Invoke(this, new GenericEventArgs<AuthenticationResult>(returnResult));

            return returnResult;
        }

        private async Task<string> GetAuthorizationToken(User user, string deviceId, string app, string appVersion, string deviceName)
        {
            var existing = (await _deviceManager.GetDevices(
                new DeviceQuery
                {
                    DeviceId = deviceId,
                    UserId = user.Id,
                    Limit = 1
                }).ConfigureAwait(false)).Items.FirstOrDefault();

            var allExistingForDevice = (await _deviceManager.GetDevices(
                new DeviceQuery
                {
                    DeviceId = deviceId
                }).ConfigureAwait(false)).Items;

            foreach (var auth in allExistingForDevice)
            {
                if (existing == null || !string.Equals(auth.AccessToken, existing.AccessToken, StringComparison.Ordinal))
                {
                    try
                    {
                        await Logout(auth).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while logging out.");
                    }
                }
            }

            if (existing != null)
            {
                _logger.LogInformation("Reissuing access token: {Token}", existing.AccessToken);
                return existing.AccessToken;
            }

            _logger.LogInformation("Creating new access token for user {0}", user.Id);
            var device = await _deviceManager.CreateDevice(new Device(user.Id, app, appVersion, deviceName, deviceId)).ConfigureAwait(false);

            return device.AccessToken;
        }

        /// <inheritdoc />
        public async Task Logout(string accessToken)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(accessToken))
            {
                throw new ArgumentNullException(nameof(accessToken));
            }

            var existing = (await _deviceManager.GetDevices(
                new DeviceQuery
                {
                    Limit = 1,
                    AccessToken = accessToken
                }).ConfigureAwait(false)).Items;

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
                    ReportSessionEnded(session.Id);
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

            var existing = await _deviceManager.GetDevices(new DeviceQuery
            {
                UserId = userId
            }).ConfigureAwait(false);

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
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var dtoOptions = _itemInfoDtoOptions;

            if (_itemInfoDtoOptions == null)
            {
                dtoOptions = new DtoOptions
                {
                    AddProgramRecordingInfo = false
                };

                var fields = dtoOptions.Fields.ToList();

                fields.Remove(ItemFields.BasicSyncInfo);
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

            if (mediaSource != null)
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
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException(nameof(itemId));
            }

            var item = _libraryManager.GetItemById(new Guid(itemId));
            var session = GetSession(sessionId);

            session.NowViewingItem = GetItemInfo(item, null);
        }

        /// <inheritdoc />
        public void ReportTranscodingInfo(string deviceId, TranscodingInfo info)
        {
            var session = Sessions.FirstOrDefault(i =>
                string.Equals(i.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));

            if (session != null)
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
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            var user = info.UserId.Equals(default)
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
            var items = (await _deviceManager.GetDevices(new DeviceQuery
            {
                AccessToken = token,
                Limit = 1
            }).ConfigureAwait(false)).Items;

            if (items.Count == 0)
            {
                return null;
            }

            return await GetSessionByAuthenticationToken(items[0], deviceId, remoteEndpoint, null).ConfigureAwait(false);
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
    }
}
