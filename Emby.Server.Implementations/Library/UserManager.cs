using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Users;
using Microsoft.Extensions.Logging;

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
        public IEnumerable<User> Users => _users;

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

        private IAuthenticationProvider[] _authenticationProviders;
        private DefaultAuthenticationProvider _defaultAuthenticationProvider;

        private InvalidAuthProvider _invalidAuthProvider;

        private IPasswordResetProvider[] _passwordResetProviders;
        private DefaultPasswordResetProvider _defaultPasswordResetProvider;

        public UserManager(
            ILoggerFactory loggerFactory,
            IServerConfigurationManager configurationManager,
            IUserRepository userRepository,
            IXmlSerializer xmlSerializer,
            INetworkManager networkManager,
            Func<IImageProcessor> imageProcessorFactory,
            Func<IDtoService> dtoServiceFactory,
            IServerApplicationHost appHost,
            IJsonSerializer jsonSerializer,
            IFileSystem fileSystem)
        {
            _logger = loggerFactory.CreateLogger(nameof(UserManager));
            UserRepository = userRepository;
            _xmlSerializer = xmlSerializer;
            _networkManager = networkManager;
            _imageProcessorFactory = imageProcessorFactory;
            _dtoServiceFactory = dtoServiceFactory;
            _appHost = appHost;
            _jsonSerializer = jsonSerializer;
            _fileSystem = fileSystem;
            ConfigurationManager = configurationManager;
            _users = Array.Empty<User>();
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

        public NameIdPair[] GetPasswordResetProviders()
        {
            return _passwordResetProviders
                .Where(i => i.IsEnabled)
                .OrderBy(i => i is DefaultPasswordResetProvider ? 0 : 1)
                .ThenBy(i => i.Name)
                .Select(i => new NameIdPair
                {
                    Name = i.Name,
                    Id = GetPasswordResetProviderId(i)
                })
                .ToArray();
        }

        public void AddParts(IEnumerable<IAuthenticationProvider> authenticationProviders,IEnumerable<IPasswordResetProvider> passwordResetProviders)
        {
            _authenticationProviders = authenticationProviders.ToArray();

            _defaultAuthenticationProvider = _authenticationProviders.OfType<DefaultAuthenticationProvider>().First();

            _invalidAuthProvider = _authenticationProviders.OfType<InvalidAuthProvider>().First();

            _passwordResetProviders = passwordResetProviders.ToArray();

            _defaultPasswordResetProvider = passwordResetProviders.OfType<DefaultPasswordResetProvider>().First();
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
        /// <exception cref="ArgumentNullException"></exception>
        public User GetUserById(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(nameof(id), "Guid can't be empty");
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
                throw new ArgumentNullException(nameof(name));
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
                    user.Policy.IsAdministrator = true;
                    UpdateUserPolicy(user, user.Policy, false);
                }
            }
        }

        public static bool IsValidUsername(string username)
        {
            // This is some regex that matches only on unicode "word" characters, as well as -, _ and @
            // In theory this will cut out most if not all 'control' characters which should help minimize any weirdness
             // Usernames can contain letters (a-z + whatever else unicode is cool with), numbers (0-9), at-signs (@), dashes (-), underscores (_), apostrophes ('), and periods (.)
            return Regex.IsMatch(username, @"^[\w\-'._@]*$");
        }

        private static bool IsValidUsernameCharacter(char i)
        {
            return IsValidUsername(i.ToString());
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
                throw new ArgumentNullException(nameof(username));
            }

            var user = Users
                .FirstOrDefault(i => string.Equals(username, i.Name, StringComparison.OrdinalIgnoreCase));

            var success = false;
            string updatedUsername = null;
            IAuthenticationProvider authenticationProvider = null;

            if (user != null)
            {
                var authResult = await AuthenticateLocalUser(username, password, hashedPassword, user, remoteEndPoint).ConfigureAwait(false);
                authenticationProvider = authResult.Item1;
                updatedUsername = authResult.Item2;
                success = authResult.Item3;
            }
            else
            {
                // user is null
                var authResult = await AuthenticateLocalUser(username, password, hashedPassword, null, remoteEndPoint).ConfigureAwait(false);
                authenticationProvider = authResult.Item1;
                updatedUsername = authResult.Item2;
                success = authResult.Item3;

                if (success && authenticationProvider != null && !(authenticationProvider is DefaultAuthenticationProvider))
                {
                    // We should trust the user that the authprovider says, not what was typed
                    if (updatedUsername != username)
                    {
                        username = updatedUsername;
                    }

                    // Search the database for the user again; the authprovider might have created it
                    user = Users
                        .FirstOrDefault(i => string.Equals(username, i.Name, StringComparison.OrdinalIgnoreCase));

                    if (authenticationProvider.GetType() != typeof(InvalidAuthProvider))
                    {
                        var hasNewUserPolicy = authenticationProvider as IHasNewUserPolicy;
                        if (hasNewUserPolicy != null)
                        {
                            var policy = hasNewUserPolicy.GetNewUserPolicy();
                            UpdateUserPolicy(user, policy, true);
                        }
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

            if (!user.Policy.EnableRemoteAccess && !_networkManager.IsInLocalNetwork(remoteEndPoint))
            {
                throw new SecurityException("Forbidden.");
            }

            if (!user.IsParentalScheduleAllowed())
            {
                throw new SecurityException("User is not allowed access at this time.");
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

        private static string GetAuthenticationProviderId(IAuthenticationProvider provider)
        {
            return provider.GetType().FullName;
        }

        private static string GetPasswordResetProviderId(IPasswordResetProvider provider)
        {
            return provider.GetType().FullName;
        }

        private IAuthenticationProvider GetAuthenticationProvider(User user)
        {
            return GetAuthenticationProviders(user).First();
        }

        private IPasswordResetProvider GetPasswordResetProvider(User user)
        {
            return GetPasswordResetProviders(user)[0];
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
                // this function used to assign any user without an auth provider to the default.
                // we're going to have it use a new function now.
                _logger.LogWarning($"The user {user.Name} was found but no Authentication Provider with ID: {user.Policy.AuthenticationProviderId} was found. Assigning user to InvalidAuthProvider temporarily");
                providers = new IAuthenticationProvider[] { _invalidAuthProvider };
            }

            return providers;
        }

        private IPasswordResetProvider[] GetPasswordResetProviders(User user)
        {
            var passwordResetProviderId = user?.Policy.PasswordResetProviderId;

            var providers = _passwordResetProviders.Where(i => i.IsEnabled).ToArray();

            if (!string.IsNullOrEmpty(passwordResetProviderId))
            {
                providers = providers.Where(i => string.Equals(passwordResetProviderId, GetPasswordResetProviderId(i), StringComparison.OrdinalIgnoreCase)).ToArray();
            }

            if (providers.Length == 0)
            {
                providers = new IPasswordResetProvider[] { _defaultPasswordResetProvider };
            }

            return providers;
        }

        private async Task<Tuple<string, bool>> AuthenticateWithProvider(IAuthenticationProvider provider, string username, string password, User resolvedUser)
        {
            try
            {
                var requiresResolvedUser = provider as IRequiresResolvedUser;
                ProviderAuthenticationResult authenticationResult = null;
                if (requiresResolvedUser != null)
                {
                    authenticationResult = await requiresResolvedUser.Authenticate(username, password, resolvedUser).ConfigureAwait(false);
                }
                else
                {
                    authenticationResult = await provider.Authenticate(username, password).ConfigureAwait(false);
                }

                if(authenticationResult.Username != username)
                {
                    _logger.LogDebug("Authentication provider provided updated username {1}", authenticationResult.Username);
                    username = authenticationResult.Username;
                }

                return new Tuple<string, bool>(username, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating with provider {provider}", provider.Name);

                return new Tuple<string, bool>(username, false);
            }
        }

        private async Task<Tuple<IAuthenticationProvider, string, bool>> AuthenticateLocalUser(string username, string password, string hashedPassword, User user, string remoteEndPoint)
        {
            string updatedUsername = null;
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
                success = string.Equals(GetAuthenticationProvider(user).GetPasswordHash(user), hashedPassword.Replace("-", string.Empty), StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                foreach (var provider in GetAuthenticationProviders(user))
                {
                    var providerAuthResult = await AuthenticateWithProvider(provider, username, password, user).ConfigureAwait(false);
                    updatedUsername = providerAuthResult.Item1;
                    success = providerAuthResult.Item2;

                    if (success)
                    {
                        authenticationProvider = provider;
                        username = updatedUsername;
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
                        success = string.Equals(GetAuthenticationProvider(user).GetEasyPasswordHash(user), hashedPassword.Replace("-", string.Empty), StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        success = string.Equals(GetAuthenticationProvider(user).GetEasyPasswordHash(user), _defaultAuthenticationProvider.GetHashedString(user, password), StringComparison.OrdinalIgnoreCase);
                    }
                }
            }

            return new Tuple<IAuthenticationProvider, string, bool>(authenticationProvider, username, success);
        }

        private void UpdateInvalidLoginAttemptCount(User user, int newValue)
        {
            if (user.Policy.InvalidLoginAttemptCount == newValue || newValue <= 0)
            {
                return;
            }

            user.Policy.InvalidLoginAttemptCount = newValue;

            // Check for users without a value here and then fill in the default value
            // also protect from an always lockout if misconfigured
            if (user.Policy.LoginAttemptsBeforeLockout == null || user.Policy.LoginAttemptsBeforeLockout == 0)
            {
                user.Policy.LoginAttemptsBeforeLockout = user.Policy.IsAdministrator ? 5 : 3;
            }

            var maxCount = user.Policy.LoginAttemptsBeforeLockout;

            var fireLockout = false;

            // -1 can be used to specify no lockout value
            if (maxCount != -1 && newValue >= maxCount)
            {
                _logger.LogDebug("Disabling user {0} due to {1} unsuccessful login attempts.", user.Name, newValue);
                user.Policy.IsDisabled = true;

                fireLockout = true;
            }

            UpdateUserPolicy(user, user.Policy, false);

            if (fireLockout)
            {
                UserLockedOut?.Invoke(this, new GenericEventArgs<User>(user));
            }
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
                throw new ArgumentNullException(nameof(user));
            }

            bool hasConfiguredPassword = GetAuthenticationProvider(user).HasPassword(user).Result;
            bool hasConfiguredEasyPassword = !string.IsNullOrEmpty(GetAuthenticationProvider(user).GetEasyPasswordHash(user));

            bool hasPassword = user.Configuration.EnableLocalPassword && !string.IsNullOrEmpty(remoteEndPoint) && _networkManager.IsInLocalNetwork(remoteEndPoint) ?
                hasConfiguredEasyPassword :
                hasConfiguredPassword;

            UserDto dto = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                HasPassword = hasPassword,
                HasConfiguredPassword = hasConfiguredPassword,
                HasConfiguredEasyPassword = hasConfiguredEasyPassword,
                LastActivityDate = user.LastActivityDate,
                LastLoginDate = user.LastLoginDate,
                Configuration = user.Configuration,
                ServerId = _appHost.SystemId,
                Policy = user.Policy
            };

            if (!hasPassword && Users.Count() == 1)
            {
                dto.EnableAutoLogin = true;
            }

            ItemImageInfo image = user.GetImageInfo(ImageType.Primary, 0);

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
        /// <exception cref="ArgumentNullException">user</exception>
        /// <exception cref="ArgumentException"></exception>
        public async Task RenameUser(User user, string newName)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (string.IsNullOrEmpty(newName))
            {
                throw new ArgumentNullException(nameof(newName));
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
        /// <exception cref="ArgumentNullException">user</exception>
        /// <exception cref="ArgumentException"></exception>
        public void UpdateUser(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
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
        /// <exception cref="ArgumentNullException">name</exception>
        /// <exception cref="ArgumentException"></exception>
        public async Task<User> CreateUser(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!IsValidUsername(name))
            {
                throw new ArgumentException("Usernames can contain unicode symbols, numbers (0-9), dashes (-), underscores (_), apostrophes ('), and periods (.)");
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
        /// <exception cref="ArgumentNullException">user</exception>
        /// <exception cref="ArgumentException"></exception>
        public async Task DeleteUser(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
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
                throw new ArgumentNullException(nameof(user));
            }

            await GetAuthenticationProvider(user).ChangePassword(user, newPassword).ConfigureAwait(false);

            UpdateUser(user);

            UserPasswordChanged?.Invoke(this, new GenericEventArgs<User>(user));
        }

        public void ChangeEasyPassword(User user, string newPassword, string newPasswordHash)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            GetAuthenticationProvider(user).ChangeEasyPassword(user, newPassword, newPasswordHash);

            UpdateUser(user);

            UserPasswordChanged?.Invoke(this, new GenericEventArgs<User>(user));
        }

        /// <summary>
        /// Instantiates the new user.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>User.</returns>
        private static User InstantiateNewUser(string name)
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

        public async Task<ForgotPasswordResult> StartForgotPasswordProcess(string enteredUsername, bool isInNetwork)
        {
            var user = string.IsNullOrWhiteSpace(enteredUsername) ?
                null :
                GetUserByName(enteredUsername);

            var action = ForgotPasswordAction.InNetworkRequired;

            if (user != null && isInNetwork)
            {
                var passwordResetProvider = GetPasswordResetProvider(user);
                return await passwordResetProvider.StartForgotPasswordProcess(user, isInNetwork).ConfigureAwait(false);
            }
            else
            {
                return new ForgotPasswordResult
                {
                    Action = action,
                    PinFile = string.Empty
                };
            }
        }

        public async Task<PinRedeemResult> RedeemPasswordResetPin(string pin)
        {
            foreach (var provider in _passwordResetProviders)
            {
                var result = await provider.RedeemPasswordResetPin(pin).ConfigureAwait(false);
                if (result.Success)
                {
                    return result;
                }
            }

            return new PinRedeemResult
            {
                Success = false,
                UsersReset = Array.Empty<string>()
            };
        }

        public UserPolicy GetUserPolicy(User user)
        {
            var path = GetPolicyFilePath(user);

            if (!File.Exists(path))
            {
                return GetDefaultPolicy(user);
            }

            try
            {
                lock (_policySyncLock)
                {
                    return (UserPolicy)_xmlSerializer.DeserializeFromFile(typeof(UserPolicy), path);
                }
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

        private static UserPolicy GetDefaultPolicy(User user)
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

            Directory.CreateDirectory(Path.GetDirectoryName(path));

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

        private static string GetPolicyFilePath(User user)
        {
            return Path.Combine(user.ConfigurationDirectoryPath, "policy.xml");
        }

        private static string GetConfigurationFilePath(User user)
        {
            return Path.Combine(user.ConfigurationDirectoryPath, "config.xml");
        }

        public UserConfiguration GetUserConfiguration(User user)
        {
            var path = GetConfigurationFilePath(user);

            if (!File.Exists(path))
            {
                return new UserConfiguration();
            }

            try
            {
                lock (_configSyncLock)
                {
                    return (UserConfiguration)_xmlSerializer.DeserializeFromFile(typeof(UserConfiguration), path);
                }
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

            Directory.CreateDirectory(Path.GetDirectoryName(path));

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

        public Task RunAsync()
        {
            _userManager.UserPolicyUpdated += _userManager_UserPolicyUpdated;

            return Task.CompletedTask;
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
