#pragma warning disable CA1307

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Events;
using Jellyfin.Data.Events.Users;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Users
{
    /// <summary>
    /// Manages the creation and retrieval of <see cref="User"/> instances.
    /// </summary>
    public partial class UserManager : IUserManager
    {
        private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
        private readonly IEventManager _eventManager;
        private readonly INetworkManager _networkManager;
        private readonly IApplicationHost _appHost;
        private readonly IImageProcessor _imageProcessor;
        private readonly ILogger<UserManager> _logger;
        private readonly IReadOnlyCollection<IPasswordResetProvider> _passwordResetProviders;
        private readonly DefaultPasswordResetProvider _defaultPasswordResetProvider;
        private readonly IServerConfigurationManager _serverConfigurationManager;

        private readonly IDictionary<Guid, User> _users;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserManager"/> class.
        /// </summary>
        /// <param name="dbProvider">The database provider.</param>
        /// <param name="eventManager">The event manager.</param>
        /// <param name="networkManager">The network manager.</param>
        /// <param name="appHost">The application host.</param>
        /// <param name="imageProcessor">The image processor.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="serverConfigurationManager">The system config manager.</param>
        /// <param name="passwordResetProviders">The password reset providers.</param>
        public UserManager(
            IDbContextFactory<JellyfinDbContext> dbProvider,
            IEventManager eventManager,
            INetworkManager networkManager,
            IApplicationHost appHost,
            IImageProcessor imageProcessor,
            ILogger<UserManager> logger,
            IServerConfigurationManager serverConfigurationManager,
            IEnumerable<IPasswordResetProvider> passwordResetProviders)
        {
            _dbProvider = dbProvider;
            _eventManager = eventManager;
            _networkManager = networkManager;
            _appHost = appHost;
            _imageProcessor = imageProcessor;
            _logger = logger;
            _serverConfigurationManager = serverConfigurationManager;

            _passwordResetProviders = passwordResetProviders.ToList();

            _defaultPasswordResetProvider = _passwordResetProviders.OfType<DefaultPasswordResetProvider>().First();

            _users = new ConcurrentDictionary<Guid, User>();
            using var dbContext = _dbProvider.CreateDbContext();
            foreach (var user in dbContext.Users
                .AsSplitQuery()
                .Include(user => user.Permissions)
                .Include(user => user.Preferences)
                .Include(user => user.AccessSchedules)
                .Include(user => user.ProfileImage)
                .AsEnumerable())
            {
                _users.Add(user.Id, user);
            }
        }

        /// <inheritdoc/>
        public event EventHandler<GenericEventArgs<User>>? OnUserUpdated;

        /// <inheritdoc/>
        public IEnumerable<User> Users => _users.Values;

        /// <inheritdoc/>
        public IEnumerable<Guid> UsersIds => _users.Keys;

        // This is some regex that matches only on unicode "word" characters, as well as -, _ and @
        // In theory this will cut out most if not all 'control' characters which should help minimize any weirdness
        // Usernames can contain letters (a-z + whatever else unicode is cool with), numbers (0-9), at-signs (@), dashes (-), underscores (_), apostrophes ('), periods (.) and spaces ( )
        [GeneratedRegex(@"^(?!\s)[\w\ \-'._@+]+(?<!\s)$")]
        private static partial Regex ValidUsernameRegex();

        /// <inheritdoc/>
        public User? GetUserById(Guid id)
        {
            if (id.IsEmpty())
            {
                throw new ArgumentException("Guid can't be empty", nameof(id));
            }

            _users.TryGetValue(id, out var user);
            return user;
        }

        /// <inheritdoc/>
        public User? GetUserByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Invalid username", nameof(name));
            }

            return _users.Values.FirstOrDefault(u => string.Equals(u.Username, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc/>
        public async Task RenameUser(User user, string newName)
        {
            ArgumentNullException.ThrowIfNull(user);

            ThrowIfInvalidUsername(newName);

            if (user.Username.Equals(newName, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The new and old names must be different.");
            }

            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
#pragma warning disable CA1311 // Specify a culture or use an invariant version to avoid implicit dependency on current culture
#pragma warning disable CA1304 // The behavior of 'string.ToUpper()' could vary based on the current user's locale settings
                if (await dbContext.Users
                        .AnyAsync(u => u.Username.ToUpper() == newName.ToUpper() && !u.Id.Equals(user.Id))
                        .ConfigureAwait(false))
                {
                    throw new ArgumentException(string.Format(
                        CultureInfo.InvariantCulture,
                        "A user with the name '{0}' already exists.",
                        newName));
                }
#pragma warning restore CA1304 // The behavior of 'string.ToUpper()' could vary based on the current user's locale settings
#pragma warning restore CA1311 // Specify a culture or use an invariant version to avoid implicit dependency on current culture
#pragma warning restore CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons

                user.Username = newName;
                await UpdateUserInternalAsync(dbContext, user).ConfigureAwait(false);
            }

            var eventArgs = new UserUpdatedEventArgs(user);
            await _eventManager.PublishAsync(eventArgs).ConfigureAwait(false);
            OnUserUpdated?.Invoke(this, eventArgs);
        }

        /// <inheritdoc/>
        public async Task UpdateUserAsync(User user)
        {
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                await UpdateUserInternalAsync(dbContext, user).ConfigureAwait(false);
            }
        }

        internal async Task<User> CreateUserInternalAsync(string name, JellyfinDbContext dbContext)
        {
            // TODO: Remove after user item data is migrated.
            var max = await dbContext.Users.AsQueryable().AnyAsync().ConfigureAwait(false)
                ? await dbContext.Users.AsQueryable().Select(u => u.InternalId).MaxAsync().ConfigureAwait(false)
                : 0;

            var user = new User(
                name,
                _defaultPasswordResetProvider.GetType().FullName!)
            {
                InternalId = max + 1
            };

            user.AddDefaultPermissions();
            user.AddDefaultPreferences();

            return user;
        }

        /// <inheritdoc/>
        public async Task<User> CreateUserAsync(string name)
        {
            ThrowIfInvalidUsername(name);

            if (Users.Any(u => u.Username.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(string.Format(
                    CultureInfo.InvariantCulture,
                    "A user with the name '{0}' already exists.",
                    name));
            }

            User newUser;
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                newUser = await CreateUserInternalAsync(name, dbContext).ConfigureAwait(false);

                dbContext.Users.Add(newUser);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
                _users.Add(newUser.Id, newUser);
            }

            await _eventManager.PublishAsync(new UserCreatedEventArgs(newUser)).ConfigureAwait(false);

            return newUser;
        }

        /// <inheritdoc/>
        public async Task DeleteUserAsync(Guid userId)
        {
            if (!_users.TryGetValue(userId, out var user))
            {
                throw new ResourceNotFoundException(nameof(userId));
            }

            if (_users.Count == 1)
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
                    nameof(userId));
            }

            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                dbContext.Users.Remove(user);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            _users.Remove(userId);

            await _eventManager.PublishAsync(new UserDeletedEventArgs(user)).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public UserDto GetUserDto(User user, string? remoteEndPoint = null)
        {
            var castReceiverApplications = _serverConfigurationManager.Configuration.CastReceiverApplications;
            return new UserDto
            {
                Name = user.Username,
                Id = user.Id,
                ServerId = _appHost.SystemId,
                EnableAutoLogin = user.EnableAutoLogin,
                LastLoginDate = user.LastLoginDate,
                LastActivityDate = user.LastActivityDate,
                PrimaryImageTag = user.ProfileImage is not null ? _imageProcessor.GetImageCacheTag(user) : null,
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
                    SubtitleLanguagePreference = user.SubtitleLanguagePreference ?? string.Empty,
                    OrderedViews = user.GetPreferenceValues<Guid>(PreferenceKind.OrderedViews),
                    GroupedFolders = user.GetPreferenceValues<Guid>(PreferenceKind.GroupedFolders),
                    MyMediaExcludes = user.GetPreferenceValues<Guid>(PreferenceKind.MyMediaExcludes),
                    LatestItemsExcludes = user.GetPreferenceValues<Guid>(PreferenceKind.LatestItemExcludes),
                    CastReceiverId = string.IsNullOrEmpty(user.CastReceiverId)
                        ? castReceiverApplications.FirstOrDefault()?.Id
                        : castReceiverApplications.FirstOrDefault(c => string.Equals(c.Id, user.CastReceiverId, StringComparison.Ordinal))?.Id
                          ?? castReceiverApplications.FirstOrDefault()?.Id
                },
                Policy = new UserPolicy
                {
                    MaxParentalRating = user.MaxParentalRatingScore,
                    MaxParentalSubRating = user.MaxParentalRatingSubScore,
                    EnableUserPreferenceAccess = user.EnableUserPreferenceAccess,
                    RemoteClientBitrateLimit = user.RemoteClientBitrateLimit ?? 0,
                    PasswordResetProviderId = user.PasswordResetProviderId,
                    InvalidLoginAttemptCount = user.InvalidLoginAttemptCount,
                    LoginAttemptsBeforeLockout = user.LoginAttemptsBeforeLockout ?? -1,
                    MaxActiveSessions = user.MaxActiveSessions,
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
                    EnableCollectionManagement = user.HasPermission(PermissionKind.EnableCollectionManagement),
                    EnableSubtitleManagement = user.HasPermission(PermissionKind.EnableSubtitleManagement),
                    AccessSchedules = user.AccessSchedules.ToArray(),
                    BlockedTags = user.GetPreference(PreferenceKind.BlockedTags),
                    AllowedTags = user.GetPreference(PreferenceKind.AllowedTags),
                    EnabledChannels = user.GetPreferenceValues<Guid>(PreferenceKind.EnabledChannels),
                    EnabledDevices = user.GetPreference(PreferenceKind.EnabledDevices),
                    EnabledFolders = user.GetPreferenceValues<Guid>(PreferenceKind.EnabledFolders),
                    EnableContentDeletionFromFolders = user.GetPreference(PreferenceKind.EnableContentDeletionFromFolders),
                    SyncPlayAccess = user.SyncPlayAccess,
                    BlockedChannels = user.GetPreferenceValues<Guid>(PreferenceKind.BlockedChannels),
                    BlockedMediaFolders = user.GetPreferenceValues<Guid>(PreferenceKind.BlockedMediaFolders),
                    BlockUnratedItems = user.GetPreferenceValues<UnratedItem>(PreferenceKind.BlockUnratedItems)
                }
            };
        }

        /// <inheritdoc/>
        public async Task<ForgotPasswordResult> StartForgotPasswordProcess(string enteredUsername, bool isInNetwork)
        {
            var user = string.IsNullOrWhiteSpace(enteredUsername) ? null : GetUserByName(enteredUsername);

            if (user is not null && isInNetwork)
            {
                var passwordResetProvider = GetPasswordResetProvider(user);
                var result = await passwordResetProvider
                    .StartForgotPasswordProcess(user, isInNetwork)
                    .ConfigureAwait(false);

                await UpdateUserAsync(user).ConfigureAwait(false);
                return result;
            }

            return new ForgotPasswordResult
            {
                Action = ForgotPasswordAction.InNetworkRequired,
                PinFile = string.Empty
            };
        }

        /// <inheritdoc/>
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

            return new PinRedeemResult();
        }

        /// <inheritdoc />
        public async Task InitializeAsync()
        {
            // TODO: Refactor the startup wizard so that it doesn't require a user to already exist.
            if (_users.Any())
            {
                return;
            }

            var defaultName = Environment.UserName;
            if (string.IsNullOrWhiteSpace(defaultName) || !ValidUsernameRegex().IsMatch(defaultName))
            {
                defaultName = "MyJellyfinUser";
            }

            _logger.LogWarning("No users, creating one with username {UserName}", defaultName);

            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                var newUser = await CreateUserInternalAsync(defaultName, dbContext).ConfigureAwait(false);
                newUser.SetPermission(PermissionKind.IsAdministrator, true);
                newUser.SetPermission(PermissionKind.EnableContentDeletion, true);
                newUser.SetPermission(PermissionKind.EnableRemoteControlOfOtherUsers, true);

                dbContext.Users.Add(newUser);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
                _users.Add(newUser.Id, newUser);
            }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public async Task UpdateConfigurationAsync(Guid userId, UserConfiguration config)
        {
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                var user = dbContext.Users
                               .Include(u => u.Permissions)
                               .Include(u => u.Preferences)
                               .Include(u => u.AccessSchedules)
                               .Include(u => u.ProfileImage)
                               .FirstOrDefault(u => u.Id.Equals(userId))
                           ?? throw new ArgumentException("No user exists with given Id!");

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

                // Only set cast receiver id if it is passed in and it exists in the server config.
                if (!string.IsNullOrEmpty(config.CastReceiverId)
                    && _serverConfigurationManager.Configuration.CastReceiverApplications.Any(c => string.Equals(c.Id, config.CastReceiverId, StringComparison.Ordinal)))
                {
                    user.CastReceiverId = config.CastReceiverId;
                }

                user.SetPreference(PreferenceKind.OrderedViews, config.OrderedViews);
                user.SetPreference(PreferenceKind.GroupedFolders, config.GroupedFolders);
                user.SetPreference(PreferenceKind.MyMediaExcludes, config.MyMediaExcludes);
                user.SetPreference(PreferenceKind.LatestItemExcludes, config.LatestItemsExcludes);

                dbContext.Update(user);
                _users[user.Id] = user;
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task UpdatePolicyAsync(Guid userId, UserPolicy policy)
        {
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                var user = dbContext.Users
                               .Include(u => u.Permissions)
                               .Include(u => u.Preferences)
                               .Include(u => u.AccessSchedules)
                               .Include(u => u.ProfileImage)
                               .FirstOrDefault(u => u.Id.Equals(userId))
                           ?? throw new ArgumentException("No user exists with given Id!");

                // The default number of login attempts is 3, but for some god forsaken reason it's sent to the server as "0"
                int? maxLoginAttempts = policy.LoginAttemptsBeforeLockout switch
                {
                    -1 => null,
                    0 => 3,
                    _ => policy.LoginAttemptsBeforeLockout
                };

                user.MaxParentalRatingScore = policy.MaxParentalRating;
                user.MaxParentalRatingSubScore = policy.MaxParentalSubRating;
                user.EnableUserPreferenceAccess = policy.EnableUserPreferenceAccess;
                user.RemoteClientBitrateLimit = policy.RemoteClientBitrateLimit;
                user.PasswordResetProviderId = policy.PasswordResetProviderId;
                user.InvalidLoginAttemptCount = policy.InvalidLoginAttemptCount;
                user.LoginAttemptsBeforeLockout = maxLoginAttempts;
                user.MaxActiveSessions = policy.MaxActiveSessions;
                user.SyncPlayAccess = policy.SyncPlayAccess;
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
                user.SetPermission(PermissionKind.EnableCollectionManagement, policy.EnableCollectionManagement);
                user.SetPermission(PermissionKind.EnableSubtitleManagement, policy.EnableSubtitleManagement);
                user.SetPermission(PermissionKind.EnableLyricManagement, policy.EnableLyricManagement);
                user.SetPermission(PermissionKind.ForceRemoteSourceTranscoding, policy.ForceRemoteSourceTranscoding);
                user.SetPermission(PermissionKind.EnablePublicSharing, policy.EnablePublicSharing);

                user.AccessSchedules.Clear();
                foreach (var policyAccessSchedule in policy.AccessSchedules)
                {
                    user.AccessSchedules.Add(policyAccessSchedule);
                }

                // TODO: fix this at some point
                user.SetPreference(PreferenceKind.BlockUnratedItems, policy.BlockUnratedItems ?? Array.Empty<UnratedItem>());
                user.SetPreference(PreferenceKind.BlockedTags, policy.BlockedTags);
                user.SetPreference(PreferenceKind.AllowedTags, policy.AllowedTags);
                user.SetPreference(PreferenceKind.EnabledChannels, policy.EnabledChannels);
                user.SetPreference(PreferenceKind.EnabledDevices, policy.EnabledDevices);
                user.SetPreference(PreferenceKind.EnabledFolders, policy.EnabledFolders);
                user.SetPreference(PreferenceKind.EnableContentDeletionFromFolders, policy.EnableContentDeletionFromFolders);

                dbContext.Update(user);
                _users[user.Id] = user;
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task ClearProfileImageAsync(User user)
        {
            if (user.ProfileImage is null)
            {
                return;
            }

            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                dbContext.Remove(user.ProfileImage);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            user.ProfileImage = null;
            _users[user.Id] = user;
        }

        internal static void ThrowIfInvalidUsername(string name)
        {
            if (!string.IsNullOrWhiteSpace(name) && ValidUsernameRegex().IsMatch(name))
            {
                return;
            }

            throw new ArgumentException("Usernames can contain unicode symbols, numbers (0-9), dashes (-), underscores (_), apostrophes ('), and periods (.)", nameof(name));
        }

        private IPasswordResetProvider GetPasswordResetProvider(User user)
        {
            return GetPasswordResetProviders(user)[0];
        }

        private IPasswordResetProvider[] GetPasswordResetProviders(User user)
        {
            var passwordResetProviderId = user.PasswordResetProviderId;
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

        private async Task UpdateUserInternalAsync(JellyfinDbContext dbContext, User user)
        {
            dbContext.Users.Update(user);
            _users[user.Id] = user;
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
