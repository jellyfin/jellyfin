using MediaBrowser.Common.Events;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Session;
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
    public class SessionManager : ISessionManager
    {
        private readonly IUserDataRepository _userDataRepository;

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

        public SessionManager(IUserDataRepository userDataRepository, IServerConfigurationManager configurationManager, ILogger logger, IUserRepository userRepository)
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
        public IEnumerable<SessionInfo> AllConnections
        {
            get { return _activeConnections.Values.OrderByDescending(c => c.LastActivityDate); }
        }

        /// <summary>
        /// Gets the active connections.
        /// </summary>
        /// <value>The active connections.</value>
        public IEnumerable<SessionInfo> RecentConnections
        {
            get { return AllConnections.Where(c => (DateTime.UtcNow - c.LastActivityDate).TotalMinutes <= 5); }
        }

        private readonly Task _trueTaskResult = Task.FromResult(true);

        /// <summary>
        /// Logs the user activity.
        /// </summary>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <param name="user">The user.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        public Task LogConnectionActivity(string clientType, string deviceId, string deviceName, User user)
        {
            var activityDate = DateTime.UtcNow;

            GetConnection(clientType, deviceId, deviceName, user).LastActivityDate = activityDate;

            if (user == null)
            {
                return _trueTaskResult;
            }

            var lastActivityDate = user.LastActivityDate;

            user.LastActivityDate = activityDate;

            // Don't log in the db anymore frequently than 10 seconds
            if (lastActivityDate.HasValue && (activityDate - lastActivityDate.Value).TotalSeconds < 10)
            {
                return _trueTaskResult;
            }

            // Save this directly. No need to fire off all the events for this.
            return _userRepository.SaveUser(user, CancellationToken.None);
        }

        /// <summary>
        /// Updates the now playing item id.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <param name="item">The item.</param>
        /// <param name="currentPositionTicks">The current position ticks.</param>
        private void UpdateNowPlayingItemId(User user, string clientType, string deviceId, string deviceName, BaseItem item, long? currentPositionTicks = null)
        {
            var conn = GetConnection(clientType, deviceId, deviceName, user);

            conn.NowPlayingPositionTicks = currentPositionTicks;
            conn.NowPlayingItem = item;
            conn.LastActivityDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Removes the now playing item id.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <param name="item">The item.</param>
        private void RemoveNowPlayingItemId(User user, string clientType, string deviceId, string deviceName, BaseItem item)
        {
            var conn = GetConnection(clientType, deviceId, deviceName, user);

            if (conn.NowPlayingItem != null && conn.NowPlayingItem.Id == item.Id)
            {
                conn.NowPlayingItem = null;
                conn.NowPlayingPositionTicks = null;
            }
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <param name="user">The user.</param>
        /// <returns>SessionInfo.</returns>
        private SessionInfo GetConnection(string clientType, string deviceId, string deviceName, User user)
        {
            var key = clientType + deviceId;

            var connection = _activeConnections.GetOrAdd(key, keyName => new SessionInfo
            {
                Client = clientType,
                DeviceId = deviceId,
                Id = Guid.NewGuid()
            });

            connection.DeviceName = deviceName;

            connection.UserId = user == null ? (Guid?)null : user.Id;

            return connection;
        }

        /// <summary>
        /// Used to report that playback has started for an item
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="item">The item.</param>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public void OnPlaybackStart(User user, BaseItem item, string clientType, string deviceId, string deviceName)
        {
            if (user == null)
            {
                throw new ArgumentNullException();
            }
            if (item == null)
            {
                throw new ArgumentNullException();
            }

            UpdateNowPlayingItemId(user, clientType, deviceId, deviceName, item);

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
        /// <param name="user">The user.</param>
        /// <param name="item">The item.</param>
        /// <param name="positionTicks">The position ticks.</param>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public async Task OnPlaybackProgress(User user, BaseItem item, long? positionTicks, string clientType, string deviceId, string deviceName)
        {
            if (user == null)
            {
                throw new ArgumentNullException();
            }
            if (item == null)
            {
                throw new ArgumentNullException();
            }

            UpdateNowPlayingItemId(user, clientType, deviceId, deviceName, item, positionTicks);

            var key = item.GetUserDataKey();

            if (positionTicks.HasValue)
            {
                var data = await _userDataRepository.GetUserData(user.Id, key).ConfigureAwait(false);

                UpdatePlayState(item, data, positionTicks.Value, false);
                await _userDataRepository.SaveUserData(user.Id, key, data, CancellationToken.None).ConfigureAwait(false);
            }

            EventHelper.QueueEventIfNotNull(PlaybackProgress, this, new PlaybackProgressEventArgs
            {
                Item = item,
                User = user,
                PlaybackPositionTicks = positionTicks
            }, _logger);
        }

        /// <summary>
        /// Used to report that playback has ended for an item
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="item">The item.</param>
        /// <param name="positionTicks">The position ticks.</param>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public async Task OnPlaybackStopped(User user, BaseItem item, long? positionTicks, string clientType, string deviceId, string deviceName)
        {
            if (user == null)
            {
                throw new ArgumentNullException();
            }
            if (item == null)
            {
                throw new ArgumentNullException();
            }

            RemoveNowPlayingItemId(user, clientType, deviceId, deviceName, item);

            var key = item.GetUserDataKey();

            var data = await _userDataRepository.GetUserData(user.Id, key).ConfigureAwait(false);

            if (positionTicks.HasValue)
            {
                UpdatePlayState(item, data, positionTicks.Value, true);
            }
            else
            {
                // If the client isn't able to report this, then we'll just have to make an assumption
                data.PlayCount++;
                data.Played = true;
            }

            await _userDataRepository.SaveUserData(user.Id, key, data, CancellationToken.None).ConfigureAwait(false);

            EventHelper.QueueEventIfNotNull(PlaybackStopped, this, new PlaybackProgressEventArgs
            {
                Item = item,
                User = user,
                PlaybackPositionTicks = positionTicks
            }, _logger);
        }

        /// <summary>
        /// Updates playstate position for an item but does not save
        /// </summary>
        /// <param name="item">The item</param>
        /// <param name="data">User data for the item</param>
        /// <param name="positionTicks">The current playback position</param>
        /// <param name="incrementPlayCount">Whether or not to increment playcount</param>
        private void UpdatePlayState(BaseItem item, UserItemData data, long positionTicks, bool incrementPlayCount)
        {
            // If a position has been reported, and if we know the duration
            if (positionTicks > 0 && item.RunTimeTicks.HasValue && item.RunTimeTicks > 0)
            {
                var pctIn = Decimal.Divide(positionTicks, item.RunTimeTicks.Value) * 100;

                // Don't track in very beginning
                if (pctIn < _configurationManager.Configuration.MinResumePct)
                {
                    positionTicks = 0;
                    incrementPlayCount = false;
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

            if (item is Audio)
            {
                data.PlaybackPositionTicks = 0;
            }

            data.PlaybackPositionTicks = positionTicks;

            if (incrementPlayCount)
            {
                data.PlayCount++;
                data.LastPlayedDate = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Identifies the web socket.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="webSocket">The web socket.</param>
        public void IdentifyWebSocket(Guid sessionId, IWebSocketConnection webSocket)
        {
            var session = AllConnections.FirstOrDefault(i => i.Id == sessionId);

            if (session != null)
            {
                session.WebSocket = webSocket;
            }
        }
    }
}
