using MediaBrowser.Common.Events;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Connect;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Connect;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Server.Implementations.Security;
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
        public event EventHandler<GenericEventArgs<User>> UserPasswordChanged;

        private readonly IXmlSerializer _xmlSerializer;

        private readonly INetworkManager _networkManager;

        private readonly Func<IImageProcessor> _imageProcessorFactory;
        private readonly Func<IDtoService> _dtoServiceFactory;
        private readonly Func<IConnectManager> _connectFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserManager" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="userRepository">The user repository.</param>
        public UserManager(ILogger logger, IServerConfigurationManager configurationManager, IUserRepository userRepository, IXmlSerializer xmlSerializer, INetworkManager networkManager, Func<IImageProcessor> imageProcessorFactory, Func<IDtoService> dtoServiceFactory, Func<IConnectManager> connectFactory)
        {
            _logger = logger;
            UserRepository = userRepository;
            _xmlSerializer = xmlSerializer;
            _networkManager = networkManager;
            _imageProcessorFactory = imageProcessorFactory;
            _dtoServiceFactory = dtoServiceFactory;
            _connectFactory = connectFactory;
            ConfigurationManager = configurationManager;
            Users = new List<User>();
        }

        #region UserUpdated Event
        /// <summary>
        /// Occurs when [user updated].
        /// </summary>
        public event EventHandler<GenericEventArgs<User>> UserUpdated;
        public event EventHandler<GenericEventArgs<User>> UserConfigurationUpdated;

        /// <summary>
        /// Called when [user updated].
        /// </summary>
        /// <param name="user">The user.</param>
        private void OnUserUpdated(User user)
        {
            EventHelper.FireEventIfNotNull(UserUpdated, this, new GenericEventArgs<User> { Argument = user }, _logger);
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
        /// Gets the user by identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>User.</returns>
        public User GetUserById(string id)
        {
            return GetUserById(new Guid(id));
        }

        public async Task Initialize()
        {
            Users = await LoadUsers().ConfigureAwait(false);
        }

        public Task<bool> AuthenticateUser(string username, string passwordSha1, string remoteEndPoint)
        {
            return AuthenticateUser(username, passwordSha1, null, remoteEndPoint);
        }

        public async Task<bool> AuthenticateUser(string username, string passwordSha1, string passwordMd5, string remoteEndPoint)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentNullException("username");
            }

            var user = Users.First(i => string.Equals(username, i.Name, StringComparison.OrdinalIgnoreCase));

            if (user.Configuration.IsDisabled)
            {
                throw new AuthenticationException(string.Format("The {0} account is currently disabled. Please consult with your administrator.", user.Name));
            }

            var success = false;

            // Authenticate using local credentials if not a guest
            if (!user.ConnectLinkType.HasValue || user.ConnectLinkType.Value != UserLinkType.Guest)
            {
                success = string.Equals(GetPasswordHash(user), passwordSha1.Replace("-", string.Empty), StringComparison.OrdinalIgnoreCase);

                if (!success && _networkManager.IsInLocalNetwork(remoteEndPoint) && user.Configuration.EnableLocalPassword)
                {
                    success = string.Equals(GetLocalPasswordHash(user), passwordSha1.Replace("-", string.Empty), StringComparison.OrdinalIgnoreCase);
                }
            }

            // Maybe user accidently entered connect credentials. let's be flexible
            if (!success && user.ConnectLinkType.HasValue && !string.IsNullOrWhiteSpace(passwordMd5))
            {
                try
                {
                    await _connectFactory().Authenticate(user.ConnectUserName, passwordMd5).ConfigureAwait(false);
                    success = true;
                }
                catch
                {

                }
            }

            // Update LastActivityDate and LastLoginDate, then save
            if (success)
            {
                user.LastActivityDate = user.LastLoginDate = DateTime.UtcNow;
                await UpdateUser(user).ConfigureAwait(false);
            }

            _logger.Info("Authentication request for {0} {1}.", user.Name, (success ? "has succeeded" : "has been denied"));

            return success;
        }

        private string GetPasswordHash(User user)
        {
            return string.IsNullOrEmpty(user.Password)
                ? GetSha1String(string.Empty)
                : user.Password;
        }

        private string GetLocalPasswordHash(User user)
        {
            return string.IsNullOrEmpty(user.LocalPassword)
                ? GetSha1String(string.Empty)
                : user.LocalPassword;
        }

        private bool IsPasswordEmpty(string passwordHash)
        {
            return string.Equals(passwordHash, GetSha1String(string.Empty), StringComparison.OrdinalIgnoreCase);
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

                var user = InstantiateNewUser(name, false);

                user.DateLastSaved = DateTime.UtcNow;

                await UserRepository.SaveUser(user, CancellationToken.None).ConfigureAwait(false);

                users.Add(user);

                user.Configuration.IsAdministrator = true;
                user.Configuration.EnableRemoteControlOfOtherUsers = true;
                UpdateConfiguration(user, user.Configuration);
            }

            return users;
        }

        public UserDto GetUserDto(User user, string remoteEndPoint = null)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            var passwordHash = GetPasswordHash(user);

            var hasConfiguredDefaultPassword = !IsPasswordEmpty(passwordHash);

            var hasPassword = user.Configuration.EnableLocalPassword && !string.IsNullOrEmpty(remoteEndPoint) && _networkManager.IsInLocalNetwork(remoteEndPoint) ?
                !IsPasswordEmpty(GetLocalPasswordHash(user)) :
                hasConfiguredDefaultPassword;

            var dto = new UserDto
            {
                Id = user.Id.ToString("N"),
                Name = user.Name,
                HasPassword = hasPassword,
                HasConfiguredPassword = hasConfiguredDefaultPassword,
                LastActivityDate = user.LastActivityDate,
                LastLoginDate = user.LastLoginDate,
                Configuration = user.Configuration,
                ConnectLinkType = user.ConnectLinkType,
                ConnectUserId = user.ConnectUserId,
                ConnectUserName = user.ConnectUserName
            };

            var image = user.GetImageInfo(ImageType.Primary, 0);

            if (image != null)
            {
                dto.PrimaryImageTag = GetImageCacheTag(user, image);

                try
                {
                    _dtoServiceFactory().AttachPrimaryImageAspectRatio(dto, user);
                }
                catch (Exception ex)
                {
                    // Have to use a catch-all unfortunately because some .net image methods throw plain Exceptions
                    _logger.ErrorException("Error generating PrimaryImageAspectRatio for {0}", ex, user.Name);
                }
            }

            return dto;
        }

        private string GetImageCacheTag(BaseItem item, ItemImageInfo image)
        {
            try
            {
                return _imageProcessorFactory().GetImageCacheTag(item, image);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting {0} image info for {1}", ex, image.Type, image.Path);
                return null;
            }
        }

        /// <summary>
        /// Refreshes metadata for each user
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task RefreshUsersMetadata(CancellationToken cancellationToken)
        {
            var tasks = Users.Select(user => user.RefreshMetadata(new MetadataRefreshOptions(), cancellationToken)).ToList();

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

        private readonly SemaphoreSlim _userListLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Creates the user.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>User.</returns>
        /// <exception cref="System.ArgumentNullException">name</exception>
        /// <exception cref="System.ArgumentException"></exception>
        public async Task<User> CreateUser(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            if (Users.Any(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(string.Format("A user with the name '{0}' already exists.", name));
            }

            await _userListLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                var user = InstantiateNewUser(name, true);

                var list = Users.ToList();
                list.Add(user);
                Users = list;

                user.DateLastSaved = DateTime.UtcNow;

                await UserRepository.SaveUser(user, CancellationToken.None).ConfigureAwait(false);

                EventHelper.QueueEventIfNotNull(UserCreated, this, new GenericEventArgs<User> { Argument = user }, _logger);

                return user;
            }
            finally
            {
                _userListLock.Release();
            }
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

            if (user.ConnectLinkType.HasValue)
            {
                await _connectFactory().RemoveConnect(user.Id.ToString("N")).ConfigureAwait(false);
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

            await _userListLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
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
            finally
            {
                _userListLock.Release();
            }
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
        public async Task ChangePassword(User user, string newPassword)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (user.ConnectLinkType.HasValue && user.ConnectLinkType.Value == UserLinkType.Guest)
            {
                throw new ArgumentException("Passwords for guests cannot be changed.");
            }

            user.Password = string.IsNullOrEmpty(newPassword) ? GetSha1String(string.Empty) : GetSha1String(newPassword);

            await UpdateUser(user).ConfigureAwait(false);

            EventHelper.FireEventIfNotNull(UserPasswordChanged, this, new GenericEventArgs<User>(user), _logger);
        }

        /// <summary>
        /// Instantiates the new user.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="checkId">if set to <c>true</c> [check identifier].</param>
        /// <returns>User.</returns>
        private User InstantiateNewUser(string name, bool checkId)
        {
            var id = ("MBUser" + name).GetMD5();

            if (checkId && Users.Select(i => i.Id).Contains(id))
            {
                id = Guid.NewGuid();
            }

            return new User
            {
                Name = name,
                Id = id,
                DateCreated = DateTime.UtcNow,
                DateModified = DateTime.UtcNow,
                UsesIdForConfigurationPath = true
            };
        }

        public void UpdateConfiguration(User user, UserConfiguration newConfiguration)
        {
            var xmlPath = user.ConfigurationFilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(xmlPath));
            _xmlSerializer.SerializeToFile(newConfiguration, xmlPath);

            EventHelper.FireEventIfNotNull(UserConfigurationUpdated, this, new GenericEventArgs<User> { Argument = user }, _logger);
        }
    }
}
