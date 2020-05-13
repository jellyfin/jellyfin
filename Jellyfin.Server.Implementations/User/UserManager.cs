#pragma warning disable CS0067
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Cryptography;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Users;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.User
{
    public class UserManager : IUserManager
    {
        private readonly JellyfinDbProvider _dbProvider;
        private readonly ICryptoProvider _cryptoProvider;
        private readonly INetworkManager _networkManager;
        private readonly ILogger<IUserManager> _logger;

        private IAuthenticationProvider[] _authenticationProviders;
        private DefaultAuthenticationProvider _defaultAuthenticationProvider;
        private InvalidAuthProvider _invalidAuthProvider;
        private IPasswordResetProvider[] _passwordResetProviders;
        private DefaultPasswordResetProvider _defaultPasswordResetProvider;

        public UserManager(
            JellyfinDbProvider dbProvider,
            ICryptoProvider cryptoProvider,
            INetworkManager networkManager,
            ILogger<IUserManager> logger)
        {
            _dbProvider = dbProvider;
            _cryptoProvider = cryptoProvider;
            _networkManager = networkManager;
            _logger = logger;
        }

        public event EventHandler<GenericEventArgs<Data.Entities.User>> OnUserPasswordChanged;

        /// <inheritdoc/>
        public event EventHandler<GenericEventArgs<Data.Entities.User>> OnUserUpdated;

        /// <inheritdoc/>
        public event EventHandler<GenericEventArgs<Data.Entities.User>> OnUserCreated;

        /// <inheritdoc/>
        public event EventHandler<GenericEventArgs<Data.Entities.User>> OnUserDeleted;

        public event EventHandler<GenericEventArgs<Data.Entities.User>> OnUserLockedOut;

        public IEnumerable<Data.Entities.User> Users
        {
            get
            {
                using var dbContext = _dbProvider.CreateContext();
                return dbContext.Users;
            }
        }

        public IEnumerable<Guid> UsersIds
        {
            get
            {
                using var dbContext = _dbProvider.CreateContext();
                return dbContext.Users.Select(u => u.Id);
            }
        }

        public Data.Entities.User GetUserById(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Guid can't be empty", nameof(id));
            }

            using var dbContext = _dbProvider.CreateContext();

            return dbContext.Users.Find(id);
        }

        public Data.Entities.User GetUserByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Invalid username", nameof(name));
            }

            using var dbContext = _dbProvider.CreateContext();

            return dbContext.Users.FirstOrDefault(u =>
                string.Equals(u.Username, name, StringComparison.OrdinalIgnoreCase));
        }

        public async Task RenameUser(Data.Entities.User user, string newName)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentException("Invalid username", nameof(newName));
            }

            if (user.Username.Equals(newName, StringComparison.Ordinal))
            {
                throw new ArgumentException("The new and old names must be different.");
            }

            if (Users.Any(
                u => u.Id != user.Id && u.Username.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(string.Format(
                    CultureInfo.InvariantCulture,
                    "A user with the name '{0}' already exists.",
                    newName));
            }

            user.Username = newName;
            await UpdateUserAsync(user).ConfigureAwait(false);

            OnUserUpdated?.Invoke(this, new GenericEventArgs<Data.Entities.User>(user));
        }

        public void UpdateUser(Data.Entities.User user)
        {
            using var dbContext = _dbProvider.CreateContext();
            dbContext.Users.Update(user);
            dbContext.SaveChanges();
        }

        public async Task UpdateUserAsync(Data.Entities.User user)
        {
            await using var dbContext = _dbProvider.CreateContext();
            dbContext.Users.Update(user);

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public Data.Entities.User CreateUser(string name)
        {
            using var dbContext = _dbProvider.CreateContext();

            var newUser = CreateUserObject(name);
            dbContext.Users.Add(newUser);
            dbContext.SaveChanges();

            return newUser;
        }

        public void DeleteUser(Data.Entities.User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            using var dbContext = _dbProvider.CreateContext();

            if (!dbContext.Users.Contains(user))
            {
                throw new ArgumentException(string.Format(
                    CultureInfo.InvariantCulture,
                    "The user cannot be deleted because there is no user with the Name {0} and Id {1}.",
                    user.Username,
                    user.Id));
            }

            if (dbContext.Users.Count() == 1)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "The user '{0}' cannot be deleted because there must be at least one user in the system.",
                    user.Username));
            }

            if (user.HasPermission(PermissionKind.IsAdministrator)
                && Users.Count(i => i.HasPermission(PermissionKind.IsAdministrator)) == 1)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The user '{0}' cannot be deleted because there must be at least one admin user in the system.",
                        user.Username),
                    nameof(user));
            }

            dbContext.Users.Remove(user);
            dbContext.SaveChanges();
        }

        public Task ResetPassword(Data.Entities.User user)
        {
            return ChangePassword(user, string.Empty);
        }

        public void ResetEasyPassword(Data.Entities.User user)
        {
            ChangeEasyPassword(user, string.Empty, null);
        }

        public async Task ChangePassword(Data.Entities.User user, string newPassword)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            await GetAuthenticationProvider(user).ChangePassword(user, newPassword).ConfigureAwait(false);
            await UpdateUserAsync(user).ConfigureAwait(false);

            OnUserPasswordChanged?.Invoke(this, new GenericEventArgs<Data.Entities.User>(user));
        }

        public void ChangeEasyPassword(Data.Entities.User user, string newPassword, string newPasswordSha1)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            GetAuthenticationProvider(user).ChangeEasyPassword(user, newPassword, newPasswordSha1);

            UpdateUser(user);

            OnUserPasswordChanged?.Invoke(this, new GenericEventArgs<Data.Entities.User>(user));
        }

        public UserDto GetUserDto(Data.Entities.User user, string remoteEndPoint = null)
        {
            return new UserDto
            {
                Id = user.Id,
                HasPassword = user.Password == null,
                EnableAutoLogin = user.EnableAutoLogin,
                LastLoginDate = user.LastLoginDate,
                LastActivityDate = user.LastActivityDate,
                Configuration = new UserConfiguration
                {
                    SubtitleMode = user.SubtitleMode,
                    HidePlayedInLatest = user.HidePlayedInLatest,
                    EnableLocalPassword = user.EnableLocalPassword,
                    PlayDefaultAudioTrack = user.PlayDefaultAudioTrack,
                    DisplayCollectionsView = user.DisplayCollectionsView,
                    DisplayMissingEpisodes = user.DisplayMissingEpisodes,
                    AudioLanguagePreference = user.AudioLanguagePreference,
                    RememberAudioSelections = user.RememberAudioSelections,
                    EnableNextEpisodeAutoPlay = user.EnableNextEpisodeAutoPlay,
                    RememberSubtitleSelections = user.RememberSubtitleSelections,
                    SubtitleLanguagePreference = user.SubtitleLanguagePreference,
                    OrderedViews = user.GetPreference(PreferenceKind.OrderedViews),
                    GroupedFolders = user.GetPreference(PreferenceKind.GroupedFolders),
                    MyMediaExcludes = user.GetPreference(PreferenceKind.MyMediaExcludes),
                    LatestItemsExcludes = user.GetPreference(PreferenceKind.LatestItemExcludes)
                },
                Policy = new UserPolicy
                {
                    MaxParentalRating = user.MaxParentalAgeRating,
                    EnableUserPreferenceAccess = user.EnableUserPreferenceAccess,
                    RemoteClientBitrateLimit = user.RemoteClientBitrateLimit.GetValueOrDefault(),
                    AuthenticatioIsnProviderId = user.AuthenticationProviderId,
                    PasswordResetProviderId = user.PasswordResetProviderId,
                    InvalidLoginAttemptCount = user.InvalidLoginAttemptCount,
                    LoginAttemptsBeforeLockout = user.LoginAttemptsBeforeLockout.GetValueOrDefault(),
                    IsAdministrator = user.HasPermission(PermissionKind.IsAdministrator),
                    IsHidden = user.HasPermission(PermissionKind.IsHidden),
                    IsDisabled = user.HasPermission(PermissionKind.IsDisabled),
                    EnableSharedDeviceControl = user.HasPermission(PermissionKind.EnableSharedDeviceControl),
                    EnableRemoteAccess = user.HasPermission(PermissionKind.EnableRemoteAccess),
                    EnableLiveTvManagement = user.HasPermission(PermissionKind.EnableLiveTvManagement),
                    EnableLiveTvAccess = user.HasPermission(PermissionKind.EnableLiveTvAccess),
                    EnableMediaPlayback = user.HasPermission(PermissionKind.EnableMediaPlayback),
                    EnableAudioPlaybackTranscoding = user.HasPermission(PermissionKind.EnableAudioPlaybackTranscoding),
                    EnableVideoPlaybackTranscoding = user.HasPermission(PermissionKind.EnableVideoPlaybackTranscoding),
                    EnableContentDeletion = user.HasPermission(PermissionKind.EnableContentDeletion),
                    EnableContentDownloading = user.HasPermission(PermissionKind.EnableContentDownloading),
                    EnableSyncTranscoding = user.HasPermission(PermissionKind.EnableSyncTranscoding),
                    EnableMediaConversion = user.HasPermission(PermissionKind.EnableMediaConversion),
                    EnableAllChannels = user.HasPermission(PermissionKind.EnableAllChannels),
                    EnableAllDevices = user.HasPermission(PermissionKind.EnableAllDevices),
                    EnableAllFolders = user.HasPermission(PermissionKind.EnableAllFolders),
                    EnableRemoteControlOfOtherUsers = user.HasPermission(PermissionKind.EnableRemoteControlOfOtherUsers),
                    EnablePlaybackRemuxing = user.HasPermission(PermissionKind.EnablePlaybackRemuxing),
                    ForceRemoteSourceTranscoding = user.HasPermission(PermissionKind.ForceRemoteSourceTranscoding),
                    EnablePublicSharing = user.HasPermission(PermissionKind.EnablePublicSharing),
                    AccessSchedules = user.AccessSchedules.ToArray(),
                    BlockedTags = user.GetPreference(PreferenceKind.BlockedTags),
                    EnabledChannels = user.GetPreference(PreferenceKind.EnabledChannels),
                    EnabledDevices = user.GetPreference(PreferenceKind.EnabledDevices),
                    EnabledFolders = user.GetPreference(PreferenceKind.EnabledFolders),
                    EnableContentDeletionFromFolders = user.GetPreference(PreferenceKind.EnableContentDeletionFromFolders)
                }
            };
        }

        public async Task<Data.Entities.User> AuthenticateUser(
            string username,
            string password,
            string passwordSha1,
            string remoteEndPoint,
            bool isUserSession)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                _logger.LogInformation("Authentication request without username has been denied (IP: {IP}).", remoteEndPoint);
                throw new ArgumentNullException(nameof(username));
            }

            var user = Users.FirstOrDefault(i => string.Equals(username, i.Username, StringComparison.OrdinalIgnoreCase));
            bool success;
            IAuthenticationProvider authenticationProvider;

            if (user != null)
            {
                var authResult = await AuthenticateLocalUser(username, password, user, remoteEndPoint)
                    .ConfigureAwait(false);
                authenticationProvider = authResult.authenticationProvider;
                success = authResult.success;
            }
            else
            {
                var authResult = await AuthenticateLocalUser(username, password, null, remoteEndPoint)
                    .ConfigureAwait(false);
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
                        .FirstOrDefault(i => string.Equals(username, i.Username, StringComparison.OrdinalIgnoreCase));

                    if (authenticationProvider is IHasNewUserPolicy hasNewUserPolicy)
                    {
                        UpdatePolicy(user.Id, hasNewUserPolicy.GetNewUserPolicy());

                        await UpdateUserAsync(user).ConfigureAwait(false);
                    }
                }
            }

            if (success && user != null && authenticationProvider != null)
            {
                var providerId = authenticationProvider.GetType().FullName;

                if (!string.Equals(providerId, user.AuthenticationProviderId, StringComparison.OrdinalIgnoreCase))
                {
                    user.AuthenticationProviderId = providerId;
                    await UpdateUserAsync(user).ConfigureAwait(false);
                }
            }

            if (user == null)
            {
                _logger.LogInformation(
                    "Authentication request for {UserName} has been denied (IP: {IP}).",
                    username,
                    remoteEndPoint);
                throw new AuthenticationException("Invalid username or password entered.");
            }

            if (user.HasPermission(PermissionKind.IsDisabled))
            {
                _logger.LogInformation(
                    "Authentication request for {UserName} has been denied because this account is currently disabled (IP: {IP}).",
                    username,
                    remoteEndPoint);
                throw new SecurityException(
                    $"The {user.Username} account is currently disabled. Please consult with your administrator.");
            }

            if (!user.HasPermission(PermissionKind.EnableRemoteAccess) &&
                !_networkManager.IsInLocalNetwork(remoteEndPoint))
            {
                _logger.LogInformation(
                    "Authentication request for {UserName} forbidden: remote access disabled and user not in local network (IP: {IP}).",
                    username,
                    remoteEndPoint);
                throw new SecurityException("Forbidden.");
            }

            if (!user.IsParentalScheduleAllowed())
            {
                _logger.LogInformation(
                    "Authentication request for {UserName} is not allowed at this time due parental restrictions (IP: {IP}).",
                    username,
                    remoteEndPoint);
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
                _logger.LogInformation("Authentication request for {UserName} has succeeded.", user.Username);
            }
            else
            {
                IncrementInvalidLoginAttemptCount(user);
                _logger.LogInformation(
                    "Authentication request for {UserName} has been denied (IP: {IP}).",
                    user.Username,
                    remoteEndPoint);
            }

            return success ? user : null;
        }

        public async Task<ForgotPasswordResult> StartForgotPasswordProcess(string enteredUsername, bool isInNetwork)
        {
            var user = string.IsNullOrWhiteSpace(enteredUsername) ? null : GetUserByName(enteredUsername);

            var action = ForgotPasswordAction.InNetworkRequired;

            if (user != null && isInNetwork)
            {
                var passwordResetProvider = GetPasswordResetProvider(user);
                return await passwordResetProvider.StartForgotPasswordProcess(user, isInNetwork).ConfigureAwait(false);
            }

            return new ForgotPasswordResult
            {
                Action = action,
                PinFile = string.Empty
            };
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

        public void AddParts(IEnumerable<IAuthenticationProvider> authenticationProviders, IEnumerable<IPasswordResetProvider> passwordResetProviders)
        {
            _authenticationProviders = authenticationProviders.ToArray();

            _defaultAuthenticationProvider = _authenticationProviders.OfType<DefaultAuthenticationProvider>().First();

            _invalidAuthProvider = _authenticationProviders.OfType<InvalidAuthProvider>().First();

            _passwordResetProviders = passwordResetProviders.ToArray();

            _defaultPasswordResetProvider = passwordResetProviders.OfType<DefaultPasswordResetProvider>().First();
        }

        public NameIdPair[] GetAuthenticationProviders()
        {
            return _authenticationProviders
                .Where(provider => provider.IsEnabled)
                .OrderBy(i => i is DefaultAuthenticationProvider ? 0 : 1)
                .ThenBy(i => i.Name)
                .Select(i => new NameIdPair
                {
                    Name = i.Name,
                    Id = i.GetType().FullName
                })
                .ToArray();
        }

        public NameIdPair[] GetPasswordResetProviders()
        {
            return _passwordResetProviders
                .Where(provider => provider.IsEnabled)
                .OrderBy(i => i is DefaultPasswordResetProvider ? 0 : 1)
                .ThenBy(i => i.Name)
                .Select(i => new NameIdPair
                {
                    Name = i.Name,
                    Id = i.GetType().FullName
                })
                .ToArray();
        }

        public void UpdateConfiguration(Guid userId, UserConfiguration config)
        {
            var user = GetUserById(userId);
            user.SubtitleMode = config.SubtitleMode;
            user.HidePlayedInLatest = config.HidePlayedInLatest;
            user.EnableLocalPassword = config.EnableLocalPassword;
            user.PlayDefaultAudioTrack = config.PlayDefaultAudioTrack;
            user.DisplayCollectionsView = config.DisplayCollectionsView;
            user.DisplayMissingEpisodes = config.DisplayMissingEpisodes;
            user.AudioLanguagePreference = config.AudioLanguagePreference;
            user.RememberAudioSelections = config.RememberAudioSelections;
            user.EnableNextEpisodeAutoPlay = config.EnableNextEpisodeAutoPlay;
            user.RememberSubtitleSelections = config.RememberSubtitleSelections;
            user.SubtitleLanguagePreference = config.SubtitleLanguagePreference;

            user.SetPreference(PreferenceKind.OrderedViews, config.OrderedViews);
            user.SetPreference(PreferenceKind.GroupedFolders, config.GroupedFolders);
            user.SetPreference(PreferenceKind.MyMediaExcludes, config.MyMediaExcludes);
            user.SetPreference(PreferenceKind.LatestItemExcludes, config.LatestItemsExcludes);

            UpdateUser(user);
        }

        public void UpdatePolicy(Guid userId, UserPolicy policy)
        {
            var user = GetUserById(userId);

            user.MaxParentalAgeRating = policy.MaxParentalRating;
            user.EnableUserPreferenceAccess = policy.EnableUserPreferenceAccess;
            user.RemoteClientBitrateLimit = policy.RemoteClientBitrateLimit;
            user.AuthenticationProviderId = policy.AuthenticatioIsnProviderId;
            user.PasswordResetProviderId = policy.PasswordResetProviderId;
            user.InvalidLoginAttemptCount = policy.InvalidLoginAttemptCount;
            user.LoginAttemptsBeforeLockout = policy.LoginAttemptsBeforeLockout == -1
                ? null
                : new int?(policy.LoginAttemptsBeforeLockout);
            user.SetPermission(PermissionKind.IsAdministrator, policy.IsAdministrator);
            user.SetPermission(PermissionKind.IsHidden, policy.IsHidden);
            user.SetPermission(PermissionKind.IsDisabled, policy.IsDisabled);
            user.SetPermission(PermissionKind.EnableSharedDeviceControl, policy.EnableSharedDeviceControl);
            user.SetPermission(PermissionKind.EnableRemoteAccess, policy.EnableRemoteAccess);
            user.SetPermission(PermissionKind.EnableLiveTvManagement, policy.EnableLiveTvManagement);
            user.SetPermission(PermissionKind.EnableLiveTvAccess, policy.EnableLiveTvAccess);
            user.SetPermission(PermissionKind.EnableMediaPlayback, policy.EnableMediaPlayback);
            user.SetPermission(PermissionKind.EnableAudioPlaybackTranscoding, policy.EnableAudioPlaybackTranscoding);
            user.SetPermission(PermissionKind.EnableVideoPlaybackTranscoding, policy.EnableVideoPlaybackTranscoding);
            user.SetPermission(PermissionKind.EnableContentDeletion, policy.EnableContentDeletion);
            user.SetPermission(PermissionKind.EnableContentDownloading, policy.EnableContentDownloading);
            user.SetPermission(PermissionKind.EnableSyncTranscoding, policy.EnableSyncTranscoding);
            user.SetPermission(PermissionKind.EnableMediaConversion, policy.EnableMediaConversion);
            user.SetPermission(PermissionKind.EnableAllChannels, policy.EnableAllChannels);
            user.SetPermission(PermissionKind.EnableAllDevices, policy.EnableAllDevices);
            user.SetPermission(PermissionKind.EnableAllFolders, policy.EnableAllFolders);
            user.SetPermission(PermissionKind.EnableRemoteControlOfOtherUsers, policy.EnableRemoteControlOfOtherUsers);
            user.SetPermission(PermissionKind.EnablePlaybackRemuxing, policy.EnablePlaybackRemuxing);
            user.SetPermission(PermissionKind.ForceRemoteSourceTranscoding, policy.ForceRemoteSourceTranscoding);
            user.SetPermission(PermissionKind.EnablePublicSharing, policy.EnablePublicSharing);

            user.AccessSchedules.Clear();
            foreach (var policyAccessSchedule in policy.AccessSchedules)
            {
                user.AccessSchedules.Add(policyAccessSchedule);
            }

            user.SetPreference(PreferenceKind.BlockedTags, policy.BlockedTags);
            user.SetPreference(PreferenceKind.EnabledChannels, policy.EnabledChannels);
            user.SetPreference(PreferenceKind.EnabledDevices, policy.EnabledDevices);
            user.SetPreference(PreferenceKind.EnabledFolders, policy.EnabledFolders);
            user.SetPreference(PreferenceKind.EnableContentDeletionFromFolders, policy.EnableContentDeletionFromFolders);
        }

        private Data.Entities.User CreateUserObject(string name)
        {
            return new Data.Entities.User(
                username: name,
                mustUpdatePassword: false,
                authenticationProviderId: _defaultAuthenticationProvider.GetType().FullName,
                invalidLoginAttemptCount: -1,
                subtitleMode: SubtitlePlaybackMode.Default,
                playDefaultAudioTrack: true);
        }

        private IAuthenticationProvider GetAuthenticationProvider(Data.Entities.User user)
        {
            return GetAuthenticationProviders(user)[0];
        }

        private IPasswordResetProvider GetPasswordResetProvider(Data.Entities.User user)
        {
            return GetPasswordResetProviders(user)[0];
        }

        private IList<IAuthenticationProvider> GetAuthenticationProviders(Data.Entities.User user)
        {
            var authenticationProviderId = user?.AuthenticationProviderId;

            var providers = _authenticationProviders.Where(i => i.IsEnabled).ToList();

            if (!string.IsNullOrEmpty(authenticationProviderId))
            {
                providers = providers.Where(i => string.Equals(authenticationProviderId, i.GetType().FullName, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (providers.Count == 0)
            {
                // Assign the user to the InvalidAuthProvider since no configured auth provider was valid/found
                _logger.LogWarning(
                    "User {UserName} was found with invalid/missing Authentication Provider {AuthenticationProviderId}. Assigning user to InvalidAuthProvider until this is corrected",
                    user?.Username,
                    user?.AuthenticationProviderId);
                providers = new List<IAuthenticationProvider>
                {
                    _invalidAuthProvider
                };
            }

            return providers;
        }

        private IList<IPasswordResetProvider> GetPasswordResetProviders(Data.Entities.User user)
        {
            var passwordResetProviderId = user?.PasswordResetProviderId;
            var providers = _passwordResetProviders.Where(i => i.IsEnabled).ToArray();

            if (!string.IsNullOrEmpty(passwordResetProviderId))
            {
                providers = providers.Where(i =>
                        string.Equals(passwordResetProviderId, i.GetType().FullName, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }

            if (providers.Length == 0)
            {
                providers = new IPasswordResetProvider[]
                {
                    _defaultPasswordResetProvider
                };
            }

            return providers;
        }

        private async Task<(IAuthenticationProvider authenticationProvider, string username, bool success)>
            AuthenticateLocalUser(
                string username,
                string password,
                Jellyfin.Data.Entities.User user,
                string remoteEndPoint)
        {
            bool success = false;
            IAuthenticationProvider authenticationProvider = null;

            foreach (var provider in GetAuthenticationProviders(user))
            {
                var providerAuthResult =
                    await AuthenticateWithProvider(provider, username, password, user).ConfigureAwait(false);
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
                && user?.EnableLocalPassword == true
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

        private async Task<(string username, bool success)> AuthenticateWithProvider(
            IAuthenticationProvider provider,
            string username,
            string password,
            Data.Entities.User resolvedUser)
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

        private void IncrementInvalidLoginAttemptCount(Data.Entities.User user)
        {
            int invalidLogins = user.InvalidLoginAttemptCount;
            int? maxInvalidLogins = user.LoginAttemptsBeforeLockout;
            if (maxInvalidLogins.HasValue
                && invalidLogins >= maxInvalidLogins)
            {
                user.SetPermission(PermissionKind.IsDisabled, true);
                OnUserLockedOut?.Invoke(this, new GenericEventArgs<Data.Entities.User>(user));
                _logger.LogWarning(
                    "Disabling user {UserName} due to {Attempts} unsuccessful login attempts.",
                    user.Username,
                    invalidLogins);
            }

            UpdateUser(user);
        }

        private void ResetInvalidLoginAttemptCount(Data.Entities.User user)
        {
            user.InvalidLoginAttemptCount = 0;
        }
    }
}
