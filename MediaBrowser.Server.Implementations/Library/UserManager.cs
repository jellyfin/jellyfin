using MediaBrowser.Common.Events;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Connect;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Connect;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Users;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

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
        private readonly IJsonSerializer _jsonSerializer;

        private readonly INetworkManager _networkManager;

        private readonly Func<IImageProcessor> _imageProcessorFactory;
        private readonly Func<IDtoService> _dtoServiceFactory;
        private readonly Func<IConnectManager> _connectFactory;
        private readonly IServerApplicationHost _appHost;
        private readonly IFileSystem _fileSystem;

        public UserManager(ILogger logger, IServerConfigurationManager configurationManager, IUserRepository userRepository, IXmlSerializer xmlSerializer, INetworkManager networkManager, Func<IImageProcessor> imageProcessorFactory, Func<IDtoService> dtoServiceFactory, Func<IConnectManager> connectFactory, IServerApplicationHost appHost, IJsonSerializer jsonSerializer, IFileSystem fileSystem)
        {
            _logger = logger;
            UserRepository = userRepository;
            _xmlSerializer = xmlSerializer;
            _networkManager = networkManager;
            _imageProcessorFactory = imageProcessorFactory;
            _dtoServiceFactory = dtoServiceFactory;
            _connectFactory = connectFactory;
            _appHost = appHost;
            _jsonSerializer = jsonSerializer;
            _fileSystem = fileSystem;
            ConfigurationManager = configurationManager;
            Users = new List<User>();

            DeletePinFile();
        }

        #region UserUpdated Event
        /// <summary>
        /// Occurs when [user updated].
        /// </summary>
        public event EventHandler<GenericEventArgs<User>> UserUpdated;
        public event EventHandler<GenericEventArgs<User>> UserConfigurationUpdated;
        public event EventHandler<GenericEventArgs<User>> UserLockedOut;

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

        public User GetUserByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            return Users.FirstOrDefault(u => string.Equals(u.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public async Task Initialize()
        {
            Users = await LoadUsers().ConfigureAwait(false);

            var users = Users.ToList();

            // If there are no local users with admin rights, make them all admins
            if (!users.Any(i => i.Policy.IsAdministrator))
            {
                foreach (var user in users)
                {
                    if (!user.ConnectLinkType.HasValue || user.ConnectLinkType.Value == UserLinkType.LinkedUser)
                    {
                        user.Policy.IsAdministrator = true;
                        await UpdateUserPolicy(user, user.Policy, false).ConfigureAwait(false);
                    }
                }
            }
        }

        public Task<bool> AuthenticateUser(string username, string passwordSha1, string remoteEndPoint)
        {
            return AuthenticateUser(username, passwordSha1, null, remoteEndPoint);
        }

        public bool IsValidUsername(string username)
        {
            // Usernames can contain letters (a-z), numbers (0-9), dashes (-), underscores (_), apostrophes ('), and periods (.)
            return username.All(IsValidUsernameCharacter);
        }

        private bool IsValidUsernameCharacter(char i)
        {
            return char.IsLetterOrDigit(i) || char.Equals(i, '-') || char.Equals(i, '_') || char.Equals(i, '\'') ||
                   char.Equals(i, '.');
        }

        public string MakeValidUsername(string username)
        {
            if (IsValidUsername(username))
            {
                return username;
            }

            // Usernames can contain letters (a-z), numbers (0-9), dashes (-), underscores (_), apostrophes ('), and periods (.)
            var builder = new StringBuilder();

            foreach (var c in username)
            {
                if (IsValidUsernameCharacter(c))
                {
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }

        public async Task<bool> AuthenticateUser(string username, string passwordSha1, string passwordMd5, string remoteEndPoint)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentNullException("username");
            }

            var user = Users
                .FirstOrDefault(i => string.Equals(username, i.Name, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                throw new SecurityException("Invalid username or password entered.");
            }

            if (user.Policy.IsDisabled)
            {
                throw new SecurityException(string.Format("The {0} account is currently disabled. Please consult with your administrator.", user.Name));
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

            // Update LastActivityDate and LastLoginDate, then save
            if (success)
            {
                user.LastActivityDate = user.LastLoginDate = DateTime.UtcNow;
                await UpdateUser(user).ConfigureAwait(false);
                await UpdateInvalidLoginAttemptCount(user, 0).ConfigureAwait(false);
            }
            else
            {
                await UpdateInvalidLoginAttemptCount(user, user.Policy.InvalidLoginAttemptCount + 1).ConfigureAwait(false);
            }

            _logger.Info("Authentication request for {0} {1}.", user.Name, success ? "has succeeded" : "has been denied");

            return success;
        }

        private async Task UpdateInvalidLoginAttemptCount(User user, int newValue)
        {
            if (user.Policy.InvalidLoginAttemptCount != newValue || newValue > 0)
            {
                user.Policy.InvalidLoginAttemptCount = newValue;

                var maxCount = user.Policy.IsAdministrator ? 
                    3 : 
                    5;

                var fireLockout = false;

                if (newValue >= maxCount)
                {
                    //_logger.Debug("Disabling user {0} due to {1} unsuccessful login attempts.", user.Name, newValue.ToString(CultureInfo.InvariantCulture));
                    //user.Policy.IsDisabled = true;

                    //fireLockout = true;
                }

                await UpdateUserPolicy(user, user.Policy, false).ConfigureAwait(false);

                if (fireLockout)
                {
                    if (UserLockedOut != null)
                    {
                        EventHelper.FireEventIfNotNull(UserLockedOut, this, new GenericEventArgs<User>(user), _logger);
                    }
                }
            }
        }

        private string GetPasswordHash(User user)
        {
            return string.IsNullOrEmpty(user.Password)
                ? GetSha1String(string.Empty)
                : user.Password;
        }

        private string GetLocalPasswordHash(User user)
        {
            return string.IsNullOrEmpty(user.EasyPassword)
                ? GetSha1String(string.Empty)
                : user.EasyPassword;
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
                var name = MakeValidUsername(Environment.UserName);

                var user = InstantiateNewUser(name);

                user.DateLastSaved = DateTime.UtcNow;

                await UserRepository.SaveUser(user, CancellationToken.None).ConfigureAwait(false);

                users.Add(user);

                user.Policy.IsAdministrator = true;
                user.Policy.EnableContentDeletion = true;
                user.Policy.EnableRemoteControlOfOtherUsers = true;
                await UpdateUserPolicy(user, user.Policy, false).ConfigureAwait(false);
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

            var hasConfiguredPassword = !IsPasswordEmpty(passwordHash);
            var hasConfiguredEasyPassword = !IsPasswordEmpty(GetLocalPasswordHash(user));

            var hasPassword = user.Configuration.EnableLocalPassword && !string.IsNullOrEmpty(remoteEndPoint) && _networkManager.IsInLocalNetwork(remoteEndPoint) ?
                hasConfiguredEasyPassword :
                hasConfiguredPassword;

            var dto = new UserDto
            {
                Id = user.Id.ToString("N"),
                Name = user.Name,
                HasPassword = hasPassword,
                HasConfiguredPassword = hasConfiguredPassword,
                HasConfiguredEasyPassword = hasConfiguredEasyPassword,
                LastActivityDate = user.LastActivityDate,
                LastLoginDate = user.LastLoginDate,
                Configuration = user.Configuration,
                ConnectLinkType = user.ConnectLinkType,
                ConnectUserId = user.ConnectUserId,
                ConnectUserName = user.ConnectUserName,
                ServerId = _appHost.SystemId,
                Policy = user.Policy
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

        public UserDto GetOfflineUserDto(User user)
        {
            var dto = GetUserDto(user);

            var offlinePasswordHash = GetLocalPasswordHash(user);
            dto.HasPassword = !IsPasswordEmpty(offlinePasswordHash);

            dto.OfflinePasswordSalt = Guid.NewGuid().ToString("N");

            // Hash the pin with the device Id to create a unique result for this device
            dto.OfflinePassword = GetSha1String((offlinePasswordHash + dto.OfflinePasswordSalt).ToLower());

            dto.ServerName = _appHost.FriendlyName;

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
            var tasks = Users.Select(user => user.RefreshMetadata(new MetadataRefreshOptions(_fileSystem), cancellationToken)).ToList();

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

            if (!IsValidUsername(name))
            {
                throw new ArgumentException("Usernames can contain letters (a-z), numbers (0-9), dashes (-), underscores (_), apostrophes ('), and periods (.)");
            }

            if (Users.Any(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(string.Format("A user with the name '{0}' already exists.", name));
            }

            await _userListLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                var user = InstantiateNewUser(name);

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

            if (user.Policy.IsAdministrator && allUsers.Count(i => i.Policy.IsAdministrator) == 1)
            {
                throw new ArgumentException(string.Format("The user '{0}' cannot be deleted because there must be at least one admin user in the system.", user.Name));
            }

            await _userListLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                var configPath = GetConfigurationFilePath(user);

                await UserRepository.DeleteUser(user, CancellationToken.None).ConfigureAwait(false);

                try
                {
                    _fileSystem.DeleteFile(configPath);
                }
                catch (IOException ex)
                {
                    _logger.ErrorException("Error deleting file {0}", ex, configPath);
                }

                DeleteUserPolicy(user);

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
            return ChangePassword(user, GetSha1String(string.Empty));
        }

        public Task ResetEasyPassword(User user)
        {
            return ChangeEasyPassword(user, GetSha1String(string.Empty));
        }

        public async Task ChangePassword(User user, string newPasswordSha1)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (string.IsNullOrWhiteSpace(newPasswordSha1))
            {
                throw new ArgumentNullException("newPasswordSha1");
            }

            if (user.ConnectLinkType.HasValue && user.ConnectLinkType.Value == UserLinkType.Guest)
            {
                throw new ArgumentException("Passwords for guests cannot be changed.");
            }

            user.Password = newPasswordSha1;

            await UpdateUser(user).ConfigureAwait(false);

            EventHelper.FireEventIfNotNull(UserPasswordChanged, this, new GenericEventArgs<User>(user), _logger);
        }

        public async Task ChangeEasyPassword(User user, string newPasswordSha1)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (string.IsNullOrWhiteSpace(newPasswordSha1))
            {
                throw new ArgumentNullException("newPasswordSha1");
            }

            user.EasyPassword = newPasswordSha1;

            await UpdateUser(user).ConfigureAwait(false);

            EventHelper.FireEventIfNotNull(UserPasswordChanged, this, new GenericEventArgs<User>(user), _logger);
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
                Id = Guid.NewGuid(),
                DateCreated = DateTime.UtcNow,
                DateModified = DateTime.UtcNow,
                UsesIdForConfigurationPath = true
            };
        }

        private string PasswordResetFile
        {
            get { return Path.Combine(ConfigurationManager.ApplicationPaths.ProgramDataPath, "passwordreset.txt"); }
        }

        private string _lastPin;
        private PasswordPinCreationResult _lastPasswordPinCreationResult;
        private int _pinAttempts;

        private PasswordPinCreationResult CreatePasswordResetPin()
        {
            var num = new Random().Next(1, 9999);

            var path = PasswordResetFile;

            var pin = num.ToString("0000", CultureInfo.InvariantCulture);
            _lastPin = pin;

            var time = TimeSpan.FromMinutes(5);
            var expiration = DateTime.UtcNow.Add(time);

            var text = new StringBuilder();

            var localAddress = _appHost.GetLocalApiUrl().Result ?? string.Empty;

            text.AppendLine("Use your web browser to visit:");
            text.AppendLine(string.Empty);
            text.AppendLine(localAddress + "/web/forgotpasswordpin.html");
            text.AppendLine(string.Empty);
            text.AppendLine("Enter the following pin code:");
            text.AppendLine(string.Empty);
            text.AppendLine(pin);
            text.AppendLine(string.Empty);
            text.AppendLine("The pin code will expire at " + expiration.ToLocalTime().ToShortDateString() + " " + expiration.ToLocalTime().ToShortTimeString());

			_fileSystem.WriteAllText(path, text.ToString(), Encoding.UTF8);

            var result = new PasswordPinCreationResult
            {
                PinFile = path,
                ExpirationDate = expiration
            };

            _lastPasswordPinCreationResult = result;
            _pinAttempts = 0;

            return result;
        }

        public ForgotPasswordResult StartForgotPasswordProcess(string enteredUsername, bool isInNetwork)
        {
            DeletePinFile();

            var user = string.IsNullOrWhiteSpace(enteredUsername) ?
                null :
                GetUserByName(enteredUsername);

            if (user != null && user.ConnectLinkType.HasValue && user.ConnectLinkType.Value == UserLinkType.Guest)
            {
                throw new ArgumentException("Unable to process forgot password request for guests.");
            }

            var action = ForgotPasswordAction.InNetworkRequired;
            string pinFile = null;
            DateTime? expirationDate = null;

            if (user != null && !user.Policy.IsAdministrator)
            {
                action = ForgotPasswordAction.ContactAdmin;
            }
            else
            {
                if (isInNetwork)
                {
                    action = ForgotPasswordAction.PinCode;
                }

                var result = CreatePasswordResetPin();
                pinFile = result.PinFile;
                expirationDate = result.ExpirationDate;
            }

            return new ForgotPasswordResult
            {
                Action = action,
                PinFile = pinFile,
                PinExpirationDate = expirationDate
            };
        }

        public async Task<PinRedeemResult> RedeemPasswordResetPin(string pin)
        {
            DeletePinFile();

            var usersReset = new List<string>();

            var valid = !string.IsNullOrWhiteSpace(_lastPin) &&
                string.Equals(_lastPin, pin, StringComparison.OrdinalIgnoreCase) &&
                _lastPasswordPinCreationResult != null &&
                _lastPasswordPinCreationResult.ExpirationDate > DateTime.UtcNow;

            if (valid)
            {
                _lastPin = null;
                _lastPasswordPinCreationResult = null;

                var users = Users.Where(i => !i.ConnectLinkType.HasValue || i.ConnectLinkType.Value != UserLinkType.Guest)
                        .ToList();

                foreach (var user in users)
                {
                    await ResetPassword(user).ConfigureAwait(false);

                    if (user.Policy.IsDisabled)
                    {
                        user.Policy.IsDisabled = false;
                        await UpdateUserPolicy(user, user.Policy, true).ConfigureAwait(false);
                    }
                    usersReset.Add(user.Name);
                }
            }
            else
            {
                _pinAttempts++;
                if (_pinAttempts >= 3)
                {
                    _lastPin = null;
                    _lastPasswordPinCreationResult = null;
                }
            }

            return new PinRedeemResult
            {
                Success = valid,
                UsersReset = usersReset.ToArray()
            };
        }

        private void DeletePinFile()
        {
            try
            {
                _fileSystem.DeleteFile(PasswordResetFile);
            }
            catch
            {

            }
        }

        class PasswordPinCreationResult
        {
            public string PinFile { get; set; }
            public DateTime ExpirationDate { get; set; }
        }

        public UserPolicy GetUserPolicy(User user)
        {
            var path = GetPolifyFilePath(user);

            try
            {
                lock (_policySyncLock)
                {
                    return (UserPolicy)_xmlSerializer.DeserializeFromFile(typeof(UserPolicy), path);
                }
            }
            catch (DirectoryNotFoundException)
            {
                return GetDefaultPolicy(user);
            }
            catch (FileNotFoundException)
            {
                return GetDefaultPolicy(user);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error reading policy file: {0}", ex, path);

                return GetDefaultPolicy(user);
            }
        }

        private UserPolicy GetDefaultPolicy(User user)
        {
            return new UserPolicy
            {
                EnableSync = true
            };
        }

        private readonly object _policySyncLock = new object();
        public Task UpdateUserPolicy(string userId, UserPolicy userPolicy)
        {
            var user = GetUserById(userId);
            return UpdateUserPolicy(user, userPolicy, true);
        }

        private async Task UpdateUserPolicy(User user, UserPolicy userPolicy, bool fireEvent)
        {
            // The xml serializer will output differently if the type is not exact
            if (userPolicy.GetType() != typeof(UserPolicy))
            {
                var json = _jsonSerializer.SerializeToString(userPolicy);
                userPolicy = _jsonSerializer.DeserializeFromString<UserPolicy>(json);
            }

            var path = GetPolifyFilePath(user);

			_fileSystem.CreateDirectory(Path.GetDirectoryName(path));

            lock (_policySyncLock)
            {
                _xmlSerializer.SerializeToFile(userPolicy, path);
                user.Policy = userPolicy;
            }

            await UpdateConfiguration(user, user.Configuration, true).ConfigureAwait(false);
        }

        private void DeleteUserPolicy(User user)
        {
            var path = GetPolifyFilePath(user);

            try
            {
                lock (_policySyncLock)
                {
                    _fileSystem.DeleteFile(path);
                }
            }
            catch (IOException)
            {

            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error deleting policy file", ex);
            }
        }

        private string GetPolifyFilePath(User user)
        {
            return Path.Combine(user.ConfigurationDirectoryPath, "policy.xml");
        }

        private string GetConfigurationFilePath(User user)
        {
            return Path.Combine(user.ConfigurationDirectoryPath, "config.xml");
        }

        public UserConfiguration GetUserConfiguration(User user)
        {
            var path = GetConfigurationFilePath(user);

            try
            {
                lock (_configSyncLock)
                {
                    return (UserConfiguration)_xmlSerializer.DeserializeFromFile(typeof(UserConfiguration), path);
                }
            }
            catch (DirectoryNotFoundException)
            {
                return new UserConfiguration();
            }
            catch (FileNotFoundException)
            {
                return new UserConfiguration();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error reading policy file: {0}", ex, path);

                return new UserConfiguration();
            }
        }

        private readonly object _configSyncLock = new object();
        public Task UpdateConfiguration(string userId, UserConfiguration config)
        {
            var user = GetUserById(userId);
            return UpdateConfiguration(user, config, true);
        }

        private async Task UpdateConfiguration(User user, UserConfiguration config, bool fireEvent)
        {
            var path = GetConfigurationFilePath(user);

            // The xml serializer will output differently if the type is not exact
            if (config.GetType() != typeof(UserConfiguration))
            {
                var json = _jsonSerializer.SerializeToString(config);
                config = _jsonSerializer.DeserializeFromString<UserConfiguration>(json);
            }

			_fileSystem.CreateDirectory(Path.GetDirectoryName(path));

            lock (_configSyncLock)
            {
                _xmlSerializer.SerializeToFile(config, path);
                user.Configuration = config;
            }

            if (fireEvent)
            {
                EventHelper.FireEventIfNotNull(UserConfigurationUpdated, this, new GenericEventArgs<User> { Argument = user }, _logger);
            }
        }
    }
}