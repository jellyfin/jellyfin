using MediaBrowser.Common.Events;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
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
        /// Gets the users.
        /// </summary>
        /// <value>The users.</value>
        public IEnumerable<User> Users { get; private set; }

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Gets or sets the configuration manager.
        /// </summary>
        /// <value>The configuration manager.</value>
        private IServerConfigurationManager ConfigurationManager { get; set; }

        /// <summary>
        /// Gets the active user repository
        /// </summary>
        /// <value>The user repository.</value>
        private IUserRepository UserRepository { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserManager" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="userRepository">The user repository.</param>
        public UserManager(ILogger logger, IServerConfigurationManager configurationManager, IUserRepository userRepository)
        {
            _logger = logger;
            UserRepository = userRepository;
            ConfigurationManager = configurationManager;
            Users = new List<User>();
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

        public async Task Initialize()
        {
            Users = await LoadUsers().ConfigureAwait(false);
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

            if (user.Configuration.IsDisabled)
            {
                throw new UnauthorizedAccessException(string.Format("The {0} account is currently disabled. Please consult with your administrator.", user.Name));
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
        /// Loads the users from the repository
        /// </summary>
        /// <returns>IEnumerable{User}.</returns>
        private async Task<IEnumerable<User>> LoadUsers()
        {
            var users = UserRepository.RetrieveAllUsers().ToList();

            // There always has to be at least one user.
            if (users.Count == 0)
            {
                var name = Environment.UserName;

                var user = InstantiateNewUser(name);

                user.DateLastSaved = DateTime.UtcNow;

                await UserRepository.SaveUser(user, CancellationToken.None).ConfigureAwait(false);

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
            var tasks = Users.Select(user => user.RefreshMetadata(new MetadataRefreshOptions
            {
                ReplaceAllMetadata = force

            }, cancellationToken)).ToList();

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
            user.DateLastSaved = DateTime.UtcNow;

            await UserRepository.SaveUser(user, CancellationToken.None).ConfigureAwait(false);

            OnUserUpdated(user);
        }

        public event EventHandler<GenericEventArgs<User>> UserCreated;

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

            user.DateLastSaved = DateTime.UtcNow;

            await UserRepository.SaveUser(user, CancellationToken.None).ConfigureAwait(false);

            EventHelper.QueueEventIfNotNull(UserCreated, this, new GenericEventArgs<User> { Argument = user }, _logger);

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

            var allUsers = Users.ToList();

            if (allUsers.FirstOrDefault(u => u.Id == user.Id) == null)
            {
                throw new ArgumentException(string.Format("The user cannot be deleted because there is no user with the Name {0} and Id {1}.", user.Name, user.Id));
            }

            if (allUsers.Count == 1)
            {
                throw new ArgumentException(string.Format("The user '{0}' cannot be deleted because there must be at least one user in the system.", user.Name));
            }

            if (user.Configuration.IsAdministrator && allUsers.Count(i => i.Configuration.IsAdministrator) == 1)
            {
                throw new ArgumentException(string.Format("The user '{0}' cannot be deleted because there must be at least one admin user in the system.", user.Name));
            }

            await UserRepository.DeleteUser(user, CancellationToken.None).ConfigureAwait(false);

            var path = user.ConfigurationFilePath;

            try
            {
                File.Delete(path);
            }
            catch (IOException ex)
            {
                _logger.ErrorException("Error deleting file {0}", ex, path);
            }

            // Force this to be lazy loaded again
            Users = await LoadUsers().ConfigureAwait(false);

            OnUserDeleted(user);
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


    }
}
