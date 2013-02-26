using MediaBrowser.Common.Events;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Connectivity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Class UserManager
    /// </summary>
    public class UserManager : BaseManager<Kernel>
    {
        /// <summary>
        /// The _active connections
        /// </summary>
        private readonly ConcurrentBag<ClientConnectionInfo> _activeConnections =
            new ConcurrentBag<ClientConnectionInfo>();

        /// <summary>
        /// Gets all connections.
        /// </summary>
        /// <value>All connections.</value>
        public IEnumerable<ClientConnectionInfo> AllConnections
        {
            get { return _activeConnections.Where(c => Kernel.GetUserById(c.UserId) != null).OrderByDescending(c => c.LastActivityDate); }
        }
        
        /// <summary>
        /// Gets the active connections.
        /// </summary>
        /// <value>The active connections.</value>
        public IEnumerable<ClientConnectionInfo> ActiveConnections
        {
            get { return AllConnections.Where(c => (DateTime.UtcNow - c.LastActivityDate).TotalMinutes <= 10); }
        }

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserManager" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        /// <param name="logger">The logger.</param>
        public UserManager(Kernel kernel, ILogger logger)
            : base(kernel)
        {
            _logger = logger;
        }

        #region UserUpdated Event
        /// <summary>
        /// Occurs when [user updated].
        /// </summary>
        public event EventHandler<GenericEventArgs<User>> UserUpdated;

        /// <summary>
        /// Called when [user updated].
        /// </summary>
        /// <param name="user">The user.</param>
        internal void OnUserUpdated(User user)
        {
            EventHelper.QueueEventIfNotNull(UserUpdated, this, new GenericEventArgs<User> { Argument = user }, _logger);

            // Notify connected ui's
            Kernel.ServerManager.SendWebSocketMessage("UserUpdated", new DtoBuilder(_logger).GetDtoUser(user));
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
        internal void OnUserDeleted(User user)
        {
            EventHelper.QueueEventIfNotNull(UserDeleted, this, new GenericEventArgs<User> { Argument = user }, _logger);

            // Notify connected ui's
            Kernel.ServerManager.SendWebSocketMessage("UserDeleted", user.Id.ToString());
        }
        #endregion

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

            password = password ?? string.Empty;
            var existingPassword = string.IsNullOrEmpty(user.Password) ? string.Empty.GetMD5().ToString() : user.Password;

            var success = password.GetMD5().ToString().Equals(existingPassword);

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
        /// Logs the user activity.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        public Task LogUserActivity(User user, ClientType clientType, string deviceName)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            var activityDate = DateTime.UtcNow;

            user.LastActivityDate = activityDate;

            LogConnection(user.Id, clientType, deviceName, activityDate);

            // Save this directly. No need to fire off all the events for this.
            return Kernel.UserRepository.SaveUser(user, CancellationToken.None);
        }

        /// <summary>
        /// Updates the now playing item id.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <param name="item">The item.</param>
        /// <param name="currentPositionTicks">The current position ticks.</param>
        public void UpdateNowPlayingItemId(User user, ClientType clientType, string deviceName, BaseItem item, long? currentPositionTicks = null)
        {
            var conn = GetConnection(user.Id, clientType, deviceName);

            conn.NowPlayingPositionTicks = currentPositionTicks;
            conn.NowPlayingItem = DtoBuilder.GetBaseItemInfo(item);
        }

        /// <summary>
        /// Removes the now playing item id.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <param name="item">The item.</param>
        public void RemoveNowPlayingItemId(User user, ClientType clientType, string deviceName, BaseItem item)
        {
            var conn = GetConnection(user.Id, clientType, deviceName);

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
        /// <param name="deviceName">Name of the device.</param>
        /// <param name="lastActivityDate">The last activity date.</param>
        private void LogConnection(Guid userId, ClientType clientType, string deviceName, DateTime lastActivityDate)
        {
            GetConnection(userId, clientType, deviceName).LastActivityDate = lastActivityDate;
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <returns>ClientConnectionInfo.</returns>
        private ClientConnectionInfo GetConnection(Guid userId, ClientType clientType, string deviceName)
        {
            var conn = _activeConnections.FirstOrDefault(c => c.UserId == userId && c.ClientType == clientType && string.Equals(deviceName, c.DeviceName, StringComparison.OrdinalIgnoreCase));

            if (conn == null)
            {
                conn = new ClientConnectionInfo
                {
                    UserId = userId,
                    ClientType = clientType,
                    DeviceName = deviceName
                };

                _activeConnections.Add(conn);
            }

            return conn;
        }

        /// <summary>
        /// Loads the users from the repository
        /// </summary>
        /// <returns>IEnumerable{User}.</returns>
        internal IEnumerable<User> LoadUsers()
        {
            var users = Kernel.UserRepository.RetrieveAllUsers().ToList();

            // There always has to be at least one user.
            if (users.Count == 0)
            {
                var name = Environment.UserName;

                var user = InstantiateNewUser(name);

                var task = Kernel.UserRepository.SaveUser(user, CancellationToken.None);

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
            var tasks = Kernel.Users.Select(user => user.RefreshMetadata(cancellationToken, forceRefresh: force)).ToList();

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

            if (Kernel.Users.Any(u => u.Id != user.Id && u.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
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

            if (user.Id == Guid.Empty || !Kernel.Users.Any(u => u.Id.Equals(user.Id)))
            {
                throw new ArgumentException(string.Format("User with name '{0}' and Id {1} does not exist.", user.Name, user.Id));
            }

            user.DateModified = DateTime.UtcNow;

            await Kernel.UserRepository.SaveUser(user, CancellationToken.None).ConfigureAwait(false);

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

            if (Kernel.Users.Any(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(string.Format("A user with the name '{0}' already exists.", name));
            }

            var user = InstantiateNewUser(name);

            var list = Kernel.Users.ToList();
            list.Add(user);
            Kernel.Users = list;

            await Kernel.UserRepository.SaveUser(user, CancellationToken.None).ConfigureAwait(false);

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

            if (Kernel.Users.FirstOrDefault(u => u.Id == user.Id) == null)
            {
                throw new ArgumentException(string.Format("The user cannot be deleted because there is no user with the Name {0} and Id {1}.", user.Name, user.Id));
            }

            if (Kernel.Users.Count() == 1)
            {
                throw new ArgumentException(string.Format("The user '{0}' be deleted because there must be at least one user in the system.", user.Name));
            }

            await Kernel.UserRepository.DeleteUser(user, CancellationToken.None).ConfigureAwait(false);

            OnUserDeleted(user);

            // Force this to be lazy loaded again
            Kernel.Users = null;
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
    }
}
