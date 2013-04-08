using MediaBrowser.Common.Events;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Connectivity;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Library
{
    /// <summary>
    /// Class UserManager
    /// </summary>
    public class UserManager : IUserManager
    {
        /// <summary>
        /// The _active connections
        /// </summary>
        private readonly ConcurrentDictionary<string, ClientConnectionInfo> _activeConnections =
            new ConcurrentDictionary<string, ClientConnectionInfo>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The _users
        /// </summary>
        private IEnumerable<User> _users;
        /// <summary>
        /// The _user lock
        /// </summary>
        private object _usersSyncLock = new object();
        /// <summary>
        /// The _users initialized
        /// </summary>
        private bool _usersInitialized;
        /// <summary>
        /// Gets the users.
        /// </summary>
        /// <value>The users.</value>
        public IEnumerable<User> Users
        {
            get
            {
                // Call ToList to exhaust the stream because we'll be iterating over this multiple times
                LazyInitializer.EnsureInitialized(ref _users, ref _usersInitialized, ref _usersSyncLock, LoadUsers);
                return _users;
            }
            internal set
            {
                _users = value;

                if (value == null)
                {
                    _usersInitialized = false;
                }
            }
        }

        /// <summary>
        /// Gets all connections.
        /// </summary>
        /// <value>All connections.</value>
        public IEnumerable<ClientConnectionInfo> AllConnections
        {
            get { return _activeConnections.Values.OrderByDescending(c => c.LastActivityDate); }
        }

        /// <summary>
        /// Gets the active connections.
        /// </summary>
        /// <value>The active connections.</value>
        public IEnumerable<ClientConnectionInfo> RecentConnections
        {
            get { return AllConnections.Where(c => (DateTime.UtcNow - c.LastActivityDate).TotalMinutes <= 5); }
        }

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Gets or sets the configuration manager.
        /// </summary>
        /// <value>The configuration manager.</value>
        private IServerConfigurationManager ConfigurationManager { get; set; }

        private readonly ConcurrentDictionary<string, Task<UserItemData>> _userData = new ConcurrentDictionary<string, Task<UserItemData>>();

        /// <summary>
        /// Gets the active user data repository
        /// </summary>
        /// <value>The user data repository.</value>
        public IUserDataRepository UserDataRepository { get; set; }

        /// <summary>
        /// Gets the active user repository
        /// </summary>
        /// <value>The user repository.</value>
        public IUserRepository UserRepository { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserManager" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        public UserManager(ILogger logger, IServerConfigurationManager configurationManager)
        {
            _logger = logger;
            ConfigurationManager = configurationManager;
        }

        #region Events
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
        #endregion

        #region UserUpdated Event
        /// <summary>
        /// Occurs when [user updated].
        /// </summary>
        public event EventHandler<GenericEventArgs<User>> UserUpdated;

        /// <summary>
        /// Called when [user updated].
        /// </summary>
        /// <param name="user">The user.</param>
        private void OnUserUpdated(User user)
        {
            EventHelper.QueueEventIfNotNull(UserUpdated, this, new GenericEventArgs<User> { Argument = user }, _logger);
        }
        #endregion

        #region UserDeleted Event
        /// <summary>
        /// Occurs when [user deleted].
        /// </summary>
        public event EventHandler<GenericEventArgs<User>> UserDeleted;
        /// <summary>
        /// Called when [user deleted].
        /// </summary>
        /// <param name="user">The user.</param>
        private void OnUserDeleted(User user)
        {
            EventHelper.QueueEventIfNotNull(UserDeleted, this, new GenericEventArgs<User> { Argument = user }, _logger);
        }
        #endregion

        /// <summary>
        /// Gets a User by Id
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>User.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public User GetUserById(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            return Users.FirstOrDefault(u => u.Id == id);
        }

        /// <summary>
        /// Authenticates a User and returns a result indicating whether or not it succeeded
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <returns>Task{System.Boolean}.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        public async Task<bool> AuthenticateUser(User user, string password)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            var existingPasswordString = string.IsNullOrEmpty(user.Password) ? GetSha1String(string.Empty) : user.Password;

            var success = string.Equals(existingPasswordString, password.Replace("-", string.Empty), StringComparison.OrdinalIgnoreCase);

            // Update LastActivityDate and LastLoginDate, then save
            if (success)
            {
                user.LastActivityDate = user.LastLoginDate = DateTime.UtcNow;
                await UpdateUser(user).ConfigureAwait(false);
            }

            _logger.Info("Authentication request for {0} {1}.", user.Name, (success ? "has succeeded" : "has been denied"));

            return success;
        }

        /// <summary>
        /// Gets the sha1 string.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <returns>System.String.</returns>
        private static string GetSha1String(string str)
        {
            using (var provider = SHA1.Create())
            {
                var hash = provider.ComputeHash(Encoding.UTF8.GetBytes(str));
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        /// <summary>
        /// Logs the user activity.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        public Task LogUserActivity(User user, string clientType, string deviceId, string deviceName)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            var activityDate = DateTime.UtcNow;

            var lastActivityDate = user.LastActivityDate;

            user.LastActivityDate = activityDate;

            LogConnection(user.Id, clientType, deviceId, deviceName, activityDate);

            // Don't log in the db anymore frequently than 10 seconds
            if (lastActivityDate.HasValue && (activityDate - lastActivityDate.Value).TotalSeconds < 10)
            {
                return Task.FromResult(true);
            }

            // Save this directly. No need to fire off all the events for this.
            return UserRepository.SaveUser(user, CancellationToken.None);
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
            var conn = GetConnection(user.Id, clientType, deviceId, deviceName);

            conn.NowPlayingPositionTicks = currentPositionTicks;
            conn.NowPlayingItem = DtoBuilder.GetBaseItemInfo(item);
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
            var conn = GetConnection(user.Id, clientType, deviceId, deviceName);

            if (conn.NowPlayingItem != null && conn.NowPlayingItem.Id.Equals(item.Id.ToString()))
            {
                conn.NowPlayingItem = null;
                conn.NowPlayingPositionTicks = null;
            }
        }

        /// <summary>
        /// Logs the connection.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <param name="lastActivityDate">The last activity date.</param>
        private void LogConnection(Guid userId, string clientType, string deviceId, string deviceName, DateTime lastActivityDate)
        {
            GetConnection(userId, clientType, deviceId, deviceName).LastActivityDate = lastActivityDate;
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <returns>ClientConnectionInfo.</returns>
        private ClientConnectionInfo GetConnection(Guid userId, string clientType, string deviceId, string deviceName)
        {
            var key = clientType + deviceId;

            var connection = _activeConnections.GetOrAdd(key, keyName => new ClientConnectionInfo
            {
                UserId = userId,
                Client = clientType,
                DeviceName = deviceName,
                DeviceId = deviceId
            });

            connection.UserId = userId;
            
            return connection;
        }

        /// <summary>
        /// Loads the users from the repository
        /// </summary>
        /// <returns>IEnumerable{User}.</returns>
        private IEnumerable<User> LoadUsers()
        {
            var users = UserRepository.RetrieveAllUsers().ToList();

            // There always has to be at least one user.
            if (users.Count == 0)
            {
                var name = Environment.UserName;

                var user = InstantiateNewUser(name);

                var task = UserRepository.SaveUser(user, CancellationToken.None);

                // Hate having to block threads
                Task.WaitAll(task);

                users.Add(user);
            }

            return users;
        }

        /// <summary>
        /// Refreshes metadata for each user
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <returns>Task.</returns>
        public Task RefreshUsersMetadata(CancellationToken cancellationToken, bool force = false)
        {
            var tasks = Users.Select(user => user.RefreshMetadata(cancellationToken, forceRefresh: force)).ToList();

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Renames the user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="newName">The new name.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        /// <exception cref="System.ArgumentException"></exception>
        public async Task RenameUser(User user, string newName)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (string.IsNullOrEmpty(newName))
            {
                throw new ArgumentNullException("newName");
            }

            if (Users.Any(u => u.Id != user.Id && u.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(string.Format("A user with the name '{0}' already exists.", newName));
            }

            if (user.Name.Equals(newName, StringComparison.Ordinal))
            {
                throw new ArgumentException("The new and old names must be different.");
            }

            await user.Rename(newName);

            OnUserUpdated(user);
        }

        /// <summary>
        /// Updates the user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <exception cref="System.ArgumentNullException">user</exception>
        /// <exception cref="System.ArgumentException"></exception>
        public async Task UpdateUser(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (user.Id == Guid.Empty || !Users.Any(u => u.Id.Equals(user.Id)))
            {
                throw new ArgumentException(string.Format("User with name '{0}' and Id {1} does not exist.", user.Name, user.Id));
            }

            user.DateModified = DateTime.UtcNow;

            await UserRepository.SaveUser(user, CancellationToken.None).ConfigureAwait(false);

            OnUserUpdated(user);
        }

        /// <summary>
        /// Creates the user.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>User.</returns>
        /// <exception cref="System.ArgumentNullException">name</exception>
        /// <exception cref="System.ArgumentException"></exception>
        public async Task<User> CreateUser(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            if (Users.Any(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(string.Format("A user with the name '{0}' already exists.", name));
            }

            var user = InstantiateNewUser(name);

            var list = Users.ToList();
            list.Add(user);
            Users = list;

            await UserRepository.SaveUser(user, CancellationToken.None).ConfigureAwait(false);

            return user;
        }

        /// <summary>
        /// Deletes the user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        /// <exception cref="System.ArgumentException"></exception>
        public async Task DeleteUser(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (Users.FirstOrDefault(u => u.Id == user.Id) == null)
            {
                throw new ArgumentException(string.Format("The user cannot be deleted because there is no user with the Name {0} and Id {1}.", user.Name, user.Id));
            }

            if (Users.Count() == 1)
            {
                throw new ArgumentException(string.Format("The user '{0}' be deleted because there must be at least one user in the system.", user.Name));
            }

            await UserRepository.DeleteUser(user, CancellationToken.None).ConfigureAwait(false);

            OnUserDeleted(user);

            // Force this to be lazy loaded again
            Users = null;
        }

        /// <summary>
        /// Resets the password by clearing it.
        /// </summary>
        /// <returns>Task.</returns>
        public Task ResetPassword(User user)
        {
            return ChangePassword(user, string.Empty);
        }

        /// <summary>
        /// Changes the password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="newPassword">The new password.</param>
        /// <returns>Task.</returns>
        public Task ChangePassword(User user, string newPassword)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            user.Password = string.IsNullOrEmpty(newPassword) ? string.Empty : GetSha1String(newPassword);

            return UpdateUser(user);
        }

        /// <summary>
        /// Instantiates the new user.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>User.</returns>
        private User InstantiateNewUser(string name)
        {
            return new User
            {
                Name = name,
                Id = ("MBUser" + name).GetMD5(),
                DateCreated = DateTime.UtcNow,
                DateModified = DateTime.UtcNow
            };
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

            if (positionTicks.HasValue)
            {
                var data = await GetUserData(user.Id, item.UserDataId).ConfigureAwait(false);

                UpdatePlayState(item, data, positionTicks.Value, false);
                await SaveUserData(user.Id, item.UserDataId, data, CancellationToken.None).ConfigureAwait(false);
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

            var data = await GetUserData(user.Id, item.UserDataId).ConfigureAwait(false);

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

            await SaveUserData(user.Id, item.UserDataId, data, CancellationToken.None).ConfigureAwait(false);

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
                if (pctIn < ConfigurationManager.Configuration.MinResumePct)
                {
                    positionTicks = 0;
                    incrementPlayCount = false;
                }

                // If we're at the end, assume completed
                else if (pctIn > ConfigurationManager.Configuration.MaxResumePct || positionTicks >= item.RunTimeTicks.Value)
                {
                    positionTicks = 0;
                    data.Played = true;
                }

                else
                {
                    // Enforce MinResumeDuration
                    var durationSeconds = TimeSpan.FromTicks(item.RunTimeTicks.Value).TotalSeconds;

                    if (durationSeconds < ConfigurationManager.Configuration.MinResumeDurationSeconds)
                    {
                        positionTicks = 0;
                        data.Played = true;
                    }
                }
            }

            data.PlaybackPositionTicks = positionTicks;

            if (incrementPlayCount)
            {
                data.PlayCount++;
                data.LastPlayedDate = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Saves display preferences for an item
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="userDataId">The user data id.</param>
        /// <param name="userData">The user data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task SaveUserData(Guid userId, Guid userDataId, UserItemData userData, CancellationToken cancellationToken)
        {
            var key = userId + userDataId.ToString();
            try
            {
                await UserDataRepository.SaveUserData(userId, userDataId, userData, cancellationToken).ConfigureAwait(false);

                var newValue = Task.FromResult(userData);

                // Once it succeeds, put it into the dictionary to make it available to everyone else
                _userData.AddOrUpdate(key, newValue, delegate { return newValue; });
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error saving user data", ex);

                throw;
            }
        }

        /// <summary>
        /// Gets the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="userDataId">The user data id.</param>
        /// <returns>Task{UserItemData}.</returns>
        public Task<UserItemData> GetUserData(Guid userId, Guid userDataId)
        {
            var key = userId + userDataId.ToString();

            return _userData.GetOrAdd(key, keyName => RetrieveUserData(userId, userDataId));
        }

        /// <summary>
        /// Retrieves the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="userDataId">The user data id.</param>
        /// <returns>Task{UserItemData}.</returns>
        private async Task<UserItemData> RetrieveUserData(Guid userId, Guid userDataId)
        {
            var userdata = await UserDataRepository.GetUserData(userId, userDataId).ConfigureAwait(false);

            return userdata ?? new UserItemData();
        }
    }
}
