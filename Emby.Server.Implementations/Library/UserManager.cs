#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Cryptography;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Authentication;
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
    /// Class UserManager.
    /// </summary>
    public class UserManager : IUserManager
    {
        private readonly object _policySyncLock = new object();
        private readonly object _configSyncLock = new object();

        private readonly ILogger _logger;
        private readonly IUserRepository _userRepository;
        private readonly IXmlSerializer _xmlSerializer;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly INetworkManager _networkManager;
        private readonly IImageProcessor _imageProcessor;
        private readonly Lazy<IDtoService> _dtoServiceFactory;
        private readonly IServerApplicationHost _appHost;
        private readonly IFileSystem _fileSystem;
        private readonly ICryptoProvider _cryptoProvider;

        private ConcurrentDictionary<Guid, User> _users;

        private IAuthenticationProvider[] _authenticationProviders;
        private DefaultAuthenticationProvider _defaultAuthenticationProvider;

        private InvalidAuthProvider _invalidAuthProvider;

        private IPasswordResetProvider[] _passwordResetProviders;
        private DefaultPasswordResetProvider _defaultPasswordResetProvider;

        private IDtoService DtoService => _dtoServiceFactory.Value;

        public UserManager(
            ILogger<UserManager> logger,
            IUserRepository userRepository,
            IXmlSerializer xmlSerializer,
            INetworkManager networkManager,
            IImageProcessor imageProcessor,
            Lazy<IDtoService> dtoServiceFactory,
            IServerApplicationHost appHost,
            IJsonSerializer jsonSerializer,
            IFileSystem fileSystem,
            ICryptoProvider cryptoProvider)
        {
            _logger = logger;
            _userRepository = userRepository;
            _xmlSerializer = xmlSerializer;
            _networkManager = networkManager;
            _imageProcessor = imageProcessor;
            _dtoServiceFactory = dtoServiceFactory;
            _appHost = appHost;
            _jsonSerializer = jsonSerializer;
            _fileSystem = fileSystem;
            _cryptoProvider = cryptoProvider;
            _users = null;
        }

        public event EventHandler<GenericEventArgs<User>> UserPasswordChanged;

        /// <summary>
        /// Occurs when [user updated].
        /// </summary>
        public event EventHandler<GenericEventArgs<User>> UserUpdated;

        public event EventHandler<GenericEventArgs<User>> UserPolicyUpdated;

        public event EventHandler<GenericEventArgs<User>> UserConfigurationUpdated;

        public event EventHandler<GenericEventArgs<User>> UserLockedOut;

        public event EventHandler<GenericEventArgs<User>> UserCreated;

        /// <summary>
        /// Occurs when [user deleted].
        /// </summary>
        public event EventHandler<GenericEventArgs<User>> UserDeleted;

        /// <inheritdoc />
        public IEnumerable<User> Users => _users.Values;

        /// <inheritdoc />
        public IEnumerable<Guid> UsersIds => _users.Keys;

        /// <summary>
        /// Called when [user updated].
        /// </summary>
        /// <param name="user">The user.</param>
        private void OnUserUpdated(User user)
        {
            UserUpdated?.Invoke(this, new GenericEventArgs<User>(user));
        }

        /// <summary>
        /// Called when [user deleted].
        /// </summary>
        /// <param name="user">The user.</param>
        private void OnUserDeleted(User user)
        {
            UserDeleted?.Invoke(this, new GenericEventArgs<User>(user));
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

        public void AddParts(IEnumerable<IAuthenticationProvider> authenticationProviders, IEnumerable<IPasswordResetProvider> passwordResetProviders)
        {
            _authenticationProviders = authenticationProviders.ToArray();

            _defaultAuthenticationProvider = _authenticationProviders.OfType<DefaultAuthenticationProvider>().First();

            _invalidAuthProvider = _authenticationProviders.OfType<InvalidAuthProvider>().First();

            _passwordResetProviders = passwordResetProviders.ToArray();

            _defaultPasswordResetProvider = passwordResetProviders.OfType<DefaultPasswordResetProvider>().First();
        }

        /// <inheritdoc />
        public User GetUserById(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Guid can't be empty", nameof(id));
            }

            _users.TryGetValue(id, out User user);
            return user;
        }

        public User GetUserByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Invalid username", nameof(name));
            }

            return Users.FirstOrDefault(u => string.Equals(u.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public void Initialize()
        {
            LoadUsers();

            var users = Users;

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
            => IsValidUsername(i.ToString(CultureInfo.InvariantCulture));

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

        public async Task<User> AuthenticateUser(
            string username,
            string password,
            string hashedPassword,
            string remoteEndPoint,
            bool isUserSession)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                _logger.LogInformation("Authentication request without username has been denied (IP: {IP}).", remoteEndPoint);
                throw new ArgumentNullException(nameof(username));
            }

            var user = Users.FirstOrDefault(i => string.Equals(username, i.Name, StringComparison.OrdinalIgnoreCase));

            var success = false;
            IAuthenticationProvider authenticationProvider = null;

            if (user != null)
            {
                var authResult = await AuthenticateLocalUser(username, password, hashedPassword, user, remoteEndPoint).ConfigureAwait(false);
                authenticationProvider = authResult.authenticationProvider;
                success = authResult.success;
            }
            else
            {
                // user is null
                var authResult = await AuthenticateLocalUser(username, password, hashedPassword, null, remoteEndPoint).ConfigureAwait(false);
                authenticationProvider = authResult.authenticationProvider;
                string updatedUsername = authResult.username;
                success = authResult.success;

                if (success
                    && authenticationProvider != null
                    && !(authenticationProvider is DefaultAuthenticationProvider))
                {
                    // Trust the username returned by the authentication provider
                    username = updatedUsername;

                    // Search the database for the user again
                    // the authentication provider might have created it
                    user = Users
                        .FirstOrDefault(i => string.Equals(username, i.Name, StringComparison.OrdinalIgnoreCase));

                    if (authenticationProvider is IHasNewUserPolicy hasNewUserPolicy)
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
                _logger.LogInformation("Authentication request for {UserName} has been denied (IP: {IP}).", username, remoteEndPoint);
                throw new AuthenticationException("Invalid username or password entered.");
            }

            if (user.Policy.IsDisabled)
            {
                _logger.LogInformation("Authentication request for {UserName} has been denied because this account is currently disabled (IP: {IP}).", username, remoteEndPoint);
                throw new SecurityException($"The {user.Name} account is currently disabled. Please consult with your administrator.");
            }

            if (!user.Policy.EnableRemoteAccess && !_networkManager.IsInLocalNetwork(remoteEndPoint))
            {
                _logger.LogInformation("Authentication request for {UserName} forbidden: remote access disabled and user not in local network (IP: {IP}).", username, remoteEndPoint);
                throw new SecurityException("Forbidden.");
            }

            if (!user.IsParentalScheduleAllowed())
            {
                _logger.LogInformation("Authentication request for {UserName} is not allowed at this time due parental restrictions (IP: {IP}).", username, remoteEndPoint);
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

                ResetInvalidLoginAttemptCount(user);
                _logger.LogInformation("Authentication request for {UserName} has succeeded.", user.Name);
            }
            else
            {
                IncrementInvalidLoginAttemptCount(user);
                _logger.LogInformation("Authentication request for {UserName} has been denied (IP: {IP}).", user.Name, remoteEndPoint);
            }

            return success ? user : null;
        }

#nullable enable

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
            return GetAuthenticationProviders(user)[0];
        }

        private IPasswordResetProvider GetPasswordResetProvider(User user)
        {
            return GetPasswordResetProviders(user)[0];
        }

        private IAuthenticationProvider[] GetAuthenticationProviders(User? user)
        {
            var authenticationProviderId = user?.Policy.AuthenticationProviderId;

            var providers = _authenticationProviders.Where(i => i.IsEnabled).ToArray();

            if (!string.IsNullOrEmpty(authenticationProviderId))
            {
                providers = providers.Where(i => string.Equals(authenticationProviderId, GetAuthenticationProviderId(i), StringComparison.OrdinalIgnoreCase)).ToArray();
            }

            if (providers.Length == 0)
            {
                // Assign the user to the InvalidAuthProvider since no configured auth provider was valid/found
                _logger.LogWarning("User {UserName} was found with invalid/missing Authentication Provider {AuthenticationProviderId}. Assigning user to InvalidAuthProvider until this is corrected", user?.Name, user?.Policy.AuthenticationProviderId);
                providers = new IAuthenticationProvider[] { _invalidAuthProvider };
            }

            return providers;
        }

        private IPasswordResetProvider[] GetPasswordResetProviders(User? user)
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

        private async Task<(string username, bool success)> AuthenticateWithProvider(
            IAuthenticationProvider provider,
            string username,
            string password,
            User? resolvedUser)
        {
            try
            {
                var authenticationResult = provider is IRequiresResolvedUser requiresResolvedUser
                    ? await requiresResolvedUser.Authenticate(username, password, resolvedUser).ConfigureAwait(false)
                    : await provider.Authenticate(username, password).ConfigureAwait(false);

                if (authenticationResult.Username != username)
                {
                    _logger.LogDebug("Authentication provider provided updated username {1}", authenticationResult.Username);
                    username = authenticationResult.Username;
                }

                return (username, true);
            }
            catch (AuthenticationException ex)
            {
                _logger.LogError(ex, "Error authenticating with provider {Provider}", provider.Name);

                return (username, false);
            }
        }

        private async Task<(IAuthenticationProvider? authenticationProvider, string username, bool success)> AuthenticateLocalUser(
            string username,
            string password,
            string hashedPassword,
            User? user,
            string remoteEndPoint)
        {
            bool success = false;
            IAuthenticationProvider? authenticationProvider = null;

            foreach (var provider in GetAuthenticationProviders(user))
            {
                var providerAuthResult = await AuthenticateWithProvider(provider, username, password, user).ConfigureAwait(false);
                var updatedUsername = providerAuthResult.username;
                success = providerAuthResult.success;

                if (success)
                {
                    authenticationProvider = provider;
                    username = updatedUsername;
                    break;
                }
            }

            if (!success
                && _networkManager.IsInLocalNetwork(remoteEndPoint)
                && user?.Configuration.EnableLocalPassword == true
                && !string.IsNullOrEmpty(user.EasyPassword))
            {
                // Check easy password
                var passwordHash = PasswordHash.Parse(user.EasyPassword);
                var hash = _cryptoProvider.ComputeHash(
                    passwordHash.Id,
                    Encoding.UTF8.GetBytes(password),
                    passwordHash.Salt.ToArray());
                success = passwordHash.Hash.SequenceEqual(hash);
            }

            return (authenticationProvider, username, success);
        }

        private void ResetInvalidLoginAttemptCount(User user)
        {
            user.Policy.InvalidLoginAttemptCount = 0;
            UpdateUserPolicy(user, user.Policy, false);
        }

        private void IncrementInvalidLoginAttemptCount(User user)
        {
            int invalidLogins = ++user.Policy.InvalidLoginAttemptCount;
            int maxInvalidLogins = user.Policy.LoginAttemptsBeforeLockout;
            if (maxInvalidLogins > 0
                && invalidLogins >= maxInvalidLogins)
            {
                user.Policy.IsDisabled = true;
                UserLockedOut?.Invoke(this, new GenericEventArgs<User>(user));
                _logger.LogWarning(
                    "Disabling user {UserName} due to {Attempts} unsuccessful login attempts.",
                    user.Name,
                    invalidLogins);
            }

            UpdateUserPolicy(user, user.Policy, false);
        }

        /// <summary>
        /// Loads the users from the repository.
        /// </summary>
        private void LoadUsers()
        {
            var users = _userRepository.RetrieveAllUsers();

            // There always has to be at least one user.
            if (users.Count != 0)
            {
                _users = new ConcurrentDictionary<Guid, User>(
                    users.Select(x => new KeyValuePair<Guid, User>(x.Id, x)));
                return;
            }

            var defaultName = Environment.UserName;
            if (string.IsNullOrWhiteSpace(defaultName))
            {
                defaultName = "MyJellyfinUser";
            }

            _logger.LogWarning("No users, creating one with username {UserName}", defaultName);

            var name = MakeValidUsername(defaultName);

            var user = InstantiateNewUser(name);

            user.DateLastSaved = DateTime.UtcNow;

            _userRepository.CreateUser(user);

            user.Policy.IsAdministrator = true;
            user.Policy.EnableContentDeletion = true;
            user.Policy.EnableRemoteControlOfOtherUsers = true;
            UpdateUserPolicy(user, user.Policy, false);

            _users = new ConcurrentDictionary<Guid, User>();
            _users[user.Id] = user;
        }

#nullable restore

        public UserDto GetUserDto(User user, string remoteEndPoint = null)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            bool hasConfiguredPassword = GetAuthenticationProvider(user).HasPassword(user);
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

            if (!hasPassword && _users.Count == 1)
            {
                dto.EnableAutoLogin = true;
            }

            ItemImageInfo image = user.GetImageInfo(ImageType.Primary, 0);

            if (image != null)
            {
                dto.PrimaryImageTag = GetImageCacheTag(user, image);

                try
                {
                    DtoService.AttachPrimaryImageAspectRatio(dto, user);
                }
                catch (Exception ex)
                {
                    // Have to use a catch-all unfortunately because some .net image methods throw plain Exceptions
                    _logger.LogError(ex, "Error generating PrimaryImageAspectRatio for {User}", user.Name);
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
                return _imageProcessor.GetImageCacheTag(item, image);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting {ImageType} image info for {ImagePath}", image.Type, image.Path);
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
                await user.RefreshMetadata(new MetadataRefreshOptions(new DirectoryService(_fileSystem)), cancellationToken).ConfigureAwait(false);
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

            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentException("Invalid username", nameof(newName));
            }

            if (user.Name.Equals(newName, StringComparison.Ordinal))
            {
                throw new ArgumentException("The new and old names must be different.");
            }

            if (Users.Any(
                u => u.Id != user.Id && u.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(string.Format(
                    CultureInfo.InvariantCulture,
                    "A user with the name '{0}' already exists.",
                    newName));
            }

            await user.Rename(newName).ConfigureAwait(false);

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

            if (user.Id == Guid.Empty)
            {
                throw new ArgumentException("Id can't be empty.", nameof(user));
            }

            if (!_users.ContainsKey(user.Id))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "A user '{0}' with Id {1} does not exist.",
                        user.Name,
                        user.Id),
                    nameof(user));
            }

            user.DateModified = DateTime.UtcNow;
            user.DateLastSaved = DateTime.UtcNow;

            _userRepository.UpdateUser(user);

            OnUserUpdated(user);
        }

        /// <summary>
        /// Creates the user.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>User.</returns>
        /// <exception cref="ArgumentNullException">name</exception>
        /// <exception cref="ArgumentException"></exception>
        public User CreateUser(string name)
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

            var user = InstantiateNewUser(name);

            _users[user.Id] = user;

            user.DateLastSaved = DateTime.UtcNow;

            _userRepository.CreateUser(user);

            EventHelper.QueueEventIfNotNull(UserCreated, this, new GenericEventArgs<User>(user), _logger);

            return user;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">The <c>user</c> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The <c>user</c> doesn't exist, or is the last administrator.</exception>
        /// <exception cref="InvalidOperationException">The <c>user</c> can't be deleted; there are no other users.</exception>
        public void DeleteUser(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (!_users.ContainsKey(user.Id))
            {
                throw new ArgumentException(string.Format(
                    CultureInfo.InvariantCulture,
                    "The user cannot be deleted because there is no user with the Name {0} and Id {1}.",
                    user.Name,
                    user.Id));
            }

            if (_users.Count == 1)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "The user '{0}' cannot be deleted because there must be at least one user in the system.",
                    user.Name));
            }

            if (user.Policy.IsAdministrator
                && Users.Count(i => i.Policy.IsAdministrator) == 1)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The user '{0}' cannot be deleted because there must be at least one admin user in the system.",
                        user.Name),
                    nameof(user));
            }

            var configPath = GetConfigurationFilePath(user);

            _userRepository.DeleteUser(user);

            // Delete user config dir
            lock (_configSyncLock)
                lock (_policySyncLock)
                {
                    try
                    {
                        Directory.Delete(user.ConfigurationDirectoryPath, true);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError(ex, "Error deleting user config dir: {Path}", user.ConfigurationDirectoryPath);
                    }
                }

            _users.TryRemove(user.Id, out _);

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
                DateModified = DateTime.UtcNow
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
                return GetDefaultPolicy();
            }

            try
            {
                lock (_policySyncLock)
                {
                    return (UserPolicy)_xmlSerializer.DeserializeFromFile(typeof(UserPolicy), path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading policy file: {Path}", path);

                return GetDefaultPolicy();
            }
        }

        private static UserPolicy GetDefaultPolicy()
        {
            return new UserPolicy
            {
                EnableContentDownloading = true,
                EnableSyncTranscoding = true
            };
        }

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
                UserPolicyUpdated?.Invoke(this, new GenericEventArgs<User>(user));
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading policy file: {Path}", path);

                return new UserConfiguration();
            }
        }

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
                UserConfigurationUpdated?.Invoke(this, new GenericEventArgs<User>(user));
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
