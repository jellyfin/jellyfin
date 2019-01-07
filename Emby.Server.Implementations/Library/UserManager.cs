using MediaBrowser.Common.Events;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
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
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Users;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Plugins;

namespace Emby.Server.Implementations.Library
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
        public IEnumerable<User> Users { get { return _users; } }

        private User[] _users;

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
        private readonly IServerApplicationHost _appHost;
        private readonly IFileSystem _fileSystem;
        private readonly ICryptoProvider _cryptographyProvider;

        private IAuthenticationProvider[] _authenticationProviders;
        private DefaultAuthenticationProvider _defaultAuthenticationProvider;

        public UserManager(ILogger logger, IServerConfigurationManager configurationManager, IUserRepository userRepository, IXmlSerializer xmlSerializer, INetworkManager networkManager, Func<IImageProcessor> imageProcessorFactory, Func<IDtoService> dtoServiceFactory, IServerApplicationHost appHost, IJsonSerializer jsonSerializer, IFileSystem fileSystem, ICryptoProvider cryptographyProvider)
        {
            _logger = logger;
            UserRepository = userRepository;
            _xmlSerializer = xmlSerializer;
            _networkManager = networkManager;
            _imageProcessorFactory = imageProcessorFactory;
            _dtoServiceFactory = dtoServiceFactory;
            _appHost = appHost;
            _jsonSerializer = jsonSerializer;
            _fileSystem = fileSystem;
            _cryptographyProvider = cryptographyProvider;
            ConfigurationManager = configurationManager;
            _users = Array.Empty<User>();

            DeletePinFile();
        }

        public NameIdPair[] GetAuthenticationProviders()
        {
            return _authenticationProviders
                .Where(i => i.IsEnabled)
                .OrderBy(i => i is DefaultAuthenticationProvider ? 0 : 1)
                .ThenBy(i => i.Name)
                .Select(i => new NameIdPair
                {
                    Name = i.Name,
                    Id = GetAuthenticationProviderId(i)
                })
                .ToArray();
        }

        public void AddParts(IEnumerable<IAuthenticationProvider> authenticationProviders)
        {
            _authenticationProviders = authenticationProviders.ToArray();

            _defaultAuthenticationProvider = _authenticationProviders.OfType<DefaultAuthenticationProvider>().First();
        }

        #region UserUpdated Event
        /// <summary>
        /// Occurs when [user updated].
        /// </summary>
        public event EventHandler<GenericEventArgs<User>> UserUpdated;
        public event EventHandler<GenericEventArgs<User>> UserPolicyUpdated;
        public event EventHandler<GenericEventArgs<User>> UserConfigurationUpdated;
        public event EventHandler<GenericEventArgs<User>> UserLockedOut;

        /// <summary>
        /// Called when [user updated].
        /// </summary>
        /// <param name="user">The user.</param>
        private void OnUserUpdated(User user)
        {
            UserUpdated?.Invoke(this, new GenericEventArgs<User> { Argument = user });
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
            UserDeleted?.Invoke(this, new GenericEventArgs<User> { Argument = user });
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
            if (id.Equals(Guid.Empty))
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

        public void Initialize()
        {
            _users = LoadUsers();

            var users = Users.ToList();

            // If there are no local users with admin rights, make them all admins
            if (!users.Any(i => i.Policy.IsAdministrator))
            {
                foreach (var user in users)
                {
                    if (!user.ConnectLinkType.HasValue || user.ConnectLinkType.Value == UserLinkType.LinkedUser)
                    {
                        user.Policy.IsAdministrator = true;
                        UpdateUserPolicy(user, user.Policy, false);
                    }
                }
            }
        }

        public bool IsValidUsername(string username)
        {
            // Usernames can contain letters (a-z), numbers (0-9), dashes (-), underscores (_), apostrophes ('), and periods (.)
            foreach (var currentChar in username)
            {
                if (!IsValidUsernameCharacter(currentChar))
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsValidUsernameCharacter(char i)
        {
            return !char.Equals(i, '<') && !char.Equals(i, '>');
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

        public async Task<User> AuthenticateUser(string username, string password, string hashedPassword, string remoteEndPoint, bool isUserSession)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentNullException("username");
            }

            var user = Users
                .FirstOrDefault(i => string.Equals(username, i.Name, StringComparison.OrdinalIgnoreCase));

            var success = false;
            IAuthenticationProvider authenticationProvider = null;

            if (user != null)
            {
                // Authenticate using local credentials if not a guest
                if (!user.ConnectLinkType.HasValue || user.ConnectLinkType.Value != UserLinkType.Guest)
                {
                    var authResult = await AuthenticateLocalUser(username, password, hashedPassword, user, remoteEndPoint).ConfigureAwait(false);
                    authenticationProvider = authResult.Item1;
                    success = authResult.Item2;
                }
            }
            else
            {
                // user is null
                var authResult = await AuthenticateLocalUser(username, password, hashedPassword, null, remoteEndPoint).ConfigureAwait(false);
                authenticationProvider = authResult.Item1;
                success = authResult.Item2;

                if (success && authenticationProvider != null && !(authenticationProvider is DefaultAuthenticationProvider))
                {
                    user = await CreateUser(username).ConfigureAwait(false);

                    var hasNewUserPolicy = authenticationProvider as IHasNewUserPolicy;
                    if (hasNewUserPolicy != null)
                    {
                        var policy = hasNewUserPolicy.GetNewUserPolicy();
                        UpdateUserPolicy(user, policy, true);
                    }
                }
            }

            if (success && user != null && authenticationProvider != null)
            {
                var providerId = GetAuthenticationProviderId(authenticationProvider);

                if (!string.Equals(providerId, user.Policy.AuthenticationProviderId, StringComparison.OrdinalIgnoreCase))
                {
                    user.Policy.AuthenticationProviderId = providerId;
                    UpdateUserPolicy(user, user.Policy, true);
                }
            }

            if (user == null)
            {
                throw new SecurityException("Invalid username or password entered.");
            }

            if (user.Policy.IsDisabled)
            {
                throw new SecurityException(string.Format("The {0} account is currently disabled. Please consult with your administrator.", user.Name));
            }

            if (user != null)
            {
                if (!user.Policy.EnableRemoteAccess && !_networkManager.IsInLocalNetwork(remoteEndPoint))
                {
                    throw new SecurityException("Forbidden.");
                }

                if (!user.IsParentalScheduleAllowed())
                {
                    throw new SecurityException("User is not allowed access at this time.");
                }
            }

            // Update LastActivityDate and LastLoginDate, then save
            if (success)
            {
                if (isUserSession)
                {
                    user.LastActivityDate = user.LastLoginDate = DateTime.UtcNow;
                    UpdateUser(user);
                }
                UpdateInvalidLoginAttemptCount(user, 0);
            }
            else
            {
                UpdateInvalidLoginAttemptCount(user, user.Policy.InvalidLoginAttemptCount + 1);
            }

            _logger.LogInformation("Authentication request for {0} {1}.", user.Name, success ? "has succeeded" : "has been denied");

            return success ? user : null;
        }

        private string GetAuthenticationProviderId(IAuthenticationProvider provider)
        {
            return provider.GetType().FullName;
        }

        private IAuthenticationProvider GetAuthenticationProvider(User user)
        {
            return GetAuthenticationProviders(user).First();
        }

        private IAuthenticationProvider[] GetAuthenticationProviders(User user)
        {
            var authenticationProviderId = user == null ? null : user.Policy.AuthenticationProviderId;

            var providers = _authenticationProviders.Where(i => i.IsEnabled).ToArray();

            if (!string.IsNullOrEmpty(authenticationProviderId))
            {
                providers = providers.Where(i => string.Equals(authenticationProviderId, GetAuthenticationProviderId(i), StringComparison.OrdinalIgnoreCase)).ToArray();
            }

            if (providers.Length == 0)
            {
                providers = new IAuthenticationProvider[] { _defaultAuthenticationProvider };
            }

            return providers;
        }

        private async Task<bool> AuthenticateWithProvider(IAuthenticationProvider provider, string username, string password, User resolvedUser)
        {
            try
            {
                var requiresResolvedUser = provider as IRequiresResolvedUser;
                if (requiresResolvedUser != null)
                {
                    await requiresResolvedUser.Authenticate(username, password, resolvedUser).ConfigureAwait(false);
                }
                else
                {
                    await provider.Authenticate(username, password).ConfigureAwait(false);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating with provider {provider}", provider.Name);

                return false;
            }
        }

        private async Task<Tuple<IAuthenticationProvider, bool>> AuthenticateLocalUser(string username, string password, string hashedPassword, User user, string remoteEndPoint)
        {
            bool success = false;
            IAuthenticationProvider authenticationProvider = null;

            if (password != null && user != null)
            {
                // Doesn't look like this is even possible to be used, because of password == null checks below
                hashedPassword = _defaultAuthenticationProvider.GetHashedString(user, password);
            }

            if (password == null)
            {
                // legacy
                success = string.Equals(_defaultAuthenticationProvider.GetPasswordHash(user), hashedPassword.Replace("-", string.Empty), StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                foreach (var provider in GetAuthenticationProviders(user))
                {
                    success = await AuthenticateWithProvider(provider, username, password, user).ConfigureAwait(false);

                    if (success)
                    {
                        authenticationProvider = provider;
                        break;
                    }
                }
            }

            if (user != null)
            {
                if (!success && _networkManager.IsInLocalNetwork(remoteEndPoint) && user.Configuration.EnableLocalPassword)
                {
                    if (password == null)
                    {
                        // legacy
                        success = string.Equals(GetLocalPasswordHash(user), hashedPassword.Replace("-", string.Empty), StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        success = string.Equals(GetLocalPasswordHash(user), _defaultAuthenticationProvider.GetHashedString(user, password), StringComparison.OrdinalIgnoreCase);
                    }
                }
            }

            return new Tuple<IAuthenticationProvider, bool>(authenticationProvider, success);
        }

        private void UpdateInvalidLoginAttemptCount(User user, int newValue)
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
                    //_logger.LogDebug("Disabling user {0} due to {1} unsuccessful login attempts.", user.Name, newValue.ToString(CultureInfo.InvariantCulture));
                    //user.Policy.IsDisabled = true;

                    //fireLockout = true;
                }

                UpdateUserPolicy(user, user.Policy, false);

                if (fireLockout)
                {
                    UserLockedOut?.Invoke(this, new GenericEventArgs<User>(user));
                }
            }
        }

        private string GetLocalPasswordHash(User user)
        {
            return string.IsNullOrEmpty(user.EasyPassword)
                ? _defaultAuthenticationProvider.GetEmptyHashedString(user)
                : user.EasyPassword;
        }

        private bool IsPasswordEmpty(User user, string passwordHash)
        {
            return string.Equals(passwordHash, _defaultAuthenticationProvider.GetEmptyHashedString(user), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Loads the users from the repository
        /// </summary>
        /// <returns>IEnumerable{User}.</returns>
        private User[] LoadUsers()
        {
            var users = UserRepository.RetrieveAllUsers();

            // There always has to be at least one user.
            if (users.Count == 0)
            {
                var defaultName = Environment.UserName;
                if (string.IsNullOrWhiteSpace(defaultName))
                {
                    defaultName = "MyJellyfinUser";
                }
                var name = MakeValidUsername(defaultName);

                var user = InstantiateNewUser(name);

                user.DateLastSaved = DateTime.UtcNow;

                UserRepository.CreateUser(user);

                users.Add(user);

                user.Policy.IsAdministrator = true;
                user.Policy.EnableContentDeletion = true;
                user.Policy.EnableRemoteControlOfOtherUsers = true;
                UpdateUserPolicy(user, user.Policy, false);
            }

            return users.ToArray();
        }

        public UserDto GetUserDto(User user, string remoteEndPoint = null)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            var hasConfiguredPassword = GetAuthenticationProvider(user).HasPassword(user).Result;
            var hasConfiguredEasyPassword = !IsPasswordEmpty(user, GetLocalPasswordHash(user));

            var hasPassword = user.Configuration.EnableLocalPassword && !string.IsNullOrEmpty(remoteEndPoint) && _networkManager.IsInLocalNetwork(remoteEndPoint) ?
                hasConfiguredEasyPassword :
                hasConfiguredPassword;

            var dto = new UserDto
            {
                Id = user.Id,
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

            if (!hasPassword && Users.Count() == 1)
            {
                dto.EnableAutoLogin = true;
            }

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
                    _logger.LogError(ex, "Error generating PrimaryImageAspectRatio for {user}", user.Name);
                }
            }

            return dto;
        }

        public UserDto GetOfflineUserDto(User user)
        {
            var dto = GetUserDto(user);

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
                _logger.LogError(ex, "Error getting {imageType} image info for {imagePath}", image.Type, image.Path);
                return null;
            }
        }

        /// <summary>
        /// Refreshes metadata for each user
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task RefreshUsersMetadata(CancellationToken cancellationToken)
        {
            foreach (var user in Users)
            {
                await user.RefreshMetadata(new MetadataRefreshOptions(new DirectoryService(_logger, _fileSystem)), cancellationToken).ConfigureAwait(false);
            }
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
        public void UpdateUser(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (user.Id.Equals(Guid.Empty) || !Users.Any(u => u.Id.Equals(user.Id)))
            {
                throw new ArgumentException(string.Format("User with name '{0}' and Id {1} does not exist.", user.Name, user.Id));
            }

            user.DateModified = DateTime.UtcNow;
            user.DateLastSaved = DateTime.UtcNow;

            UserRepository.UpdateUser(user);

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
                _users = list.ToArray();

                user.DateLastSaved = DateTime.UtcNow;

                UserRepository.CreateUser(user);

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

                UserRepository.DeleteUser(user);

                try
                {
                    _fileSystem.DeleteFile(configPath);
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "Error deleting file {path}", configPath);
                }

                DeleteUserPolicy(user);

                _users = allUsers.Where(i => i.Id != user.Id).ToArray();

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

        public void ResetEasyPassword(User user)
        {
            ChangeEasyPassword(user, string.Empty, null);
        }

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

            await GetAuthenticationProvider(user).ChangePassword(user, newPassword).ConfigureAwait(false);

            UpdateUser(user);

            UserPasswordChanged?.Invoke(this, new GenericEventArgs<User>(user));
        }

        public void ChangeEasyPassword(User user, string newPassword, string newPasswordHash)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (newPassword != null)
            {
                newPasswordHash = _defaultAuthenticationProvider.GetHashedString(user, newPassword);
            }

            if (string.IsNullOrWhiteSpace(newPasswordHash))
            {
                throw new ArgumentNullException("newPasswordHash");
            }

            user.EasyPassword = newPasswordHash;

            UpdateUser(user);

            UserPasswordChanged?.Invoke(this, new GenericEventArgs<User>(user));
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
                UsesIdForConfigurationPath = true,
                //Salt = BCrypt.GenerateSalt()
            };
        }

        private string PasswordResetFile
        {
            get { return Path.Combine(ConfigurationManager.ApplicationPaths.ProgramDataPath, "passwordreset.txt"); }
        }

        private string _lastPin;
        private PasswordPinCreationResult _lastPasswordPinCreationResult;
        private int _pinAttempts;

        private async Task<PasswordPinCreationResult> CreatePasswordResetPin()
        {
            var num = new Random().Next(1, 9999);

            var path = PasswordResetFile;

            var pin = num.ToString("0000", CultureInfo.InvariantCulture);
            _lastPin = pin;

            var time = TimeSpan.FromMinutes(5);
            var expiration = DateTime.UtcNow.Add(time);

            var text = new StringBuilder();

            var localAddress = (await _appHost.GetLocalApiUrl(CancellationToken.None).ConfigureAwait(false)) ?? string.Empty;

            text.AppendLine("Use your web browser to visit:");
            text.AppendLine(string.Empty);
            text.AppendLine(localAddress + "/web/forgotpasswordpin.html");
            text.AppendLine(string.Empty);
            text.AppendLine("Enter the following pin code:");
            text.AppendLine(string.Empty);
            text.AppendLine(pin);
            text.AppendLine(string.Empty);

            var localExpirationTime = expiration.ToLocalTime();
            // Tuesday, 22 August 2006 06:30 AM
            text.AppendLine("The pin code will expire at " + localExpirationTime.ToString("f1", CultureInfo.CurrentCulture));

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

        public async Task<ForgotPasswordResult> StartForgotPasswordProcess(string enteredUsername, bool isInNetwork)
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

                var result = await CreatePasswordResetPin().ConfigureAwait(false);
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
                        UpdateUserPolicy(user, user.Policy, true);
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
            var path = GetPolicyFilePath(user);

            try
            {
                lock (_policySyncLock)
                {
                    return (UserPolicy)_xmlSerializer.DeserializeFromFile(typeof(UserPolicy), path);
                }
            }
            catch (FileNotFoundException)
            {
                return GetDefaultPolicy(user);
            }
            catch (IOException)
            {
                return GetDefaultPolicy(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading policy file: {path}", path);

                return GetDefaultPolicy(user);
            }
        }

        private UserPolicy GetDefaultPolicy(User user)
        {
            return new UserPolicy
            {
                EnableContentDownloading = true,
                EnableSyncTranscoding = true
            };
        }

        private readonly object _policySyncLock = new object();
        public void UpdateUserPolicy(Guid userId, UserPolicy userPolicy)
        {
            var user = GetUserById(userId);
            UpdateUserPolicy(user, userPolicy, true);
        }

        private void UpdateUserPolicy(User user, UserPolicy userPolicy, bool fireEvent)
        {
            // The xml serializer will output differently if the type is not exact
            if (userPolicy.GetType() != typeof(UserPolicy))
            {
                var json = _jsonSerializer.SerializeToString(userPolicy);
                userPolicy = _jsonSerializer.DeserializeFromString<UserPolicy>(json);
            }

            var path = GetPolicyFilePath(user);

            _fileSystem.CreateDirectory(_fileSystem.GetDirectoryName(path));

            lock (_policySyncLock)
            {
                _xmlSerializer.SerializeToFile(userPolicy, path);
                user.Policy = userPolicy;
            }

            if (fireEvent)
            {
                UserPolicyUpdated?.Invoke(this, new GenericEventArgs<User> { Argument = user });
            }
        }

        private void DeleteUserPolicy(User user)
        {
            var path = GetPolicyFilePath(user);

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
                _logger.LogError(ex, "Error deleting policy file");
            }
        }

        private string GetPolicyFilePath(User user)
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
            catch (FileNotFoundException)
            {
                return new UserConfiguration();
            }
            catch (IOException)
            {
                return new UserConfiguration();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading policy file: {path}", path);

                return new UserConfiguration();
            }
        }

        private readonly object _configSyncLock = new object();
        public void UpdateConfiguration(Guid userId, UserConfiguration config)
        {
            var user = GetUserById(userId);
            UpdateConfiguration(user, config);
        }

        public void UpdateConfiguration(User user, UserConfiguration config)
        {
            UpdateConfiguration(user, config, true);
        }

        private void UpdateConfiguration(User user, UserConfiguration config, bool fireEvent)
        {
            var path = GetConfigurationFilePath(user);

            // The xml serializer will output differently if the type is not exact
            if (config.GetType() != typeof(UserConfiguration))
            {
                var json = _jsonSerializer.SerializeToString(config);
                config = _jsonSerializer.DeserializeFromString<UserConfiguration>(json);
            }

            _fileSystem.CreateDirectory(_fileSystem.GetDirectoryName(path));

            lock (_configSyncLock)
            {
                _xmlSerializer.SerializeToFile(config, path);
                user.Configuration = config;
            }

            if (fireEvent)
            {
                UserConfigurationUpdated?.Invoke(this, new GenericEventArgs<User> { Argument = user });
            }
        }
    }

    public class DeviceAccessEntryPoint : IServerEntryPoint
    {
        private IUserManager _userManager;
        private IAuthenticationRepository _authRepo;
        private IDeviceManager _deviceManager;
        private ISessionManager _sessionManager;

        public DeviceAccessEntryPoint(IUserManager userManager, IAuthenticationRepository authRepo, IDeviceManager deviceManager, ISessionManager sessionManager)
        {
            _userManager = userManager;
            _authRepo = authRepo;
            _deviceManager = deviceManager;
            _sessionManager = sessionManager;
        }

        public void Run()
        {
            _userManager.UserPolicyUpdated += _userManager_UserPolicyUpdated;
        }

        private void _userManager_UserPolicyUpdated(object sender, GenericEventArgs<User> e)
        {
            var user = e.Argument;
            if (!user.Policy.EnableAllDevices)
            {
                UpdateDeviceAccess(user);
            }
        }

        private void UpdateDeviceAccess(User user)
        {
            var existing = _authRepo.Get(new AuthenticationInfoQuery
            {
                UserId = user.Id

            }).Items;

            foreach (var authInfo in existing)
            {
                if (!string.IsNullOrEmpty(authInfo.DeviceId) && !_deviceManager.CanAccessDevice(user, authInfo.DeviceId))
                {
                    _sessionManager.Logout(authInfo);
                }
            }
        }

        public void Dispose()
        {

        }
    }
}
