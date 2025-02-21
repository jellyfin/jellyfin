using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities
{
    /// <summary>
    /// An entity representing a user.
    /// </summary>
    public class User : IHasPermissions, IHasConcurrencyToken
    {
        /// <summary>
        /// The values being delimited here are Guids, so commas work as they do not appear in Guids.
        /// </summary>
        private const char Delimiter = ',';

        /// <summary>
        /// Initializes a new instance of the <see cref="User"/> class.
        /// Public constructor with required data.
        /// </summary>
        /// <param name="username">The username for the new user.</param>
        /// <param name="authenticationProviderId">The Id of the user's authentication provider.</param>
        /// <param name="passwordResetProviderId">The Id of the user's password reset provider.</param>
        public User(string username, string authenticationProviderId, string passwordResetProviderId)
        {
            ArgumentException.ThrowIfNullOrEmpty(username);
            ArgumentException.ThrowIfNullOrEmpty(authenticationProviderId);
            ArgumentException.ThrowIfNullOrEmpty(passwordResetProviderId);

            Username = username;
            AuthenticationProviderId = authenticationProviderId;
            PasswordResetProviderId = passwordResetProviderId;

            AccessSchedules = new HashSet<AccessSchedule>();
            DisplayPreferences = new HashSet<DisplayPreferences>();
            ItemDisplayPreferences = new HashSet<ItemDisplayPreferences>();
            // Groups = new HashSet<Group>();
            Permissions = new HashSet<Permission>();
            Preferences = new HashSet<Preference>();
            // ProviderMappings = new HashSet<ProviderMapping>();

            // Set default values
            Id = Guid.NewGuid();
            InvalidLoginAttemptCount = 0;
            EnableUserPreferenceAccess = true;
            MustUpdatePassword = false;
            DisplayMissingEpisodes = false;
            DisplayCollectionsView = false;
            HidePlayedInLatest = true;
            RememberAudioSelections = true;
            RememberSubtitleSelections = true;
            EnableNextEpisodeAutoPlay = true;
            EnableAutoLogin = false;
            PlayDefaultAudioTrack = true;
            SubtitleMode = SubtitlePlaybackMode.Default;
            SyncPlayAccess = SyncPlayUserAccessType.CreateAndJoinGroups;
        }

        /// <summary>
        /// Gets or sets the Id of the user.
        /// </summary>
        /// <remarks>
        /// Identity, Indexed, Required.
        /// </remarks>
        [JsonIgnore]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the user's name.
        /// </summary>
        /// <remarks>
        /// Required, Max length = 255.
        /// </remarks>
        [MaxLength(255)]
        [StringLength(255)]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the user's password, or <c>null</c> if none is set.
        /// </summary>
        /// <remarks>
        /// Max length = 65535.
        /// </remarks>
        [MaxLength(65535)]
        [StringLength(65535)]
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user must update their password.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public bool MustUpdatePassword { get; set; }

        /// <summary>
        /// Gets or sets the audio language preference.
        /// </summary>
        /// <remarks>
        /// Max length = 255.
        /// </remarks>
        [MaxLength(255)]
        [StringLength(255)]
        public string? AudioLanguagePreference { get; set; }

        /// <summary>
        /// Gets or sets the authentication provider id.
        /// </summary>
        /// <remarks>
        /// Required, Max length = 255.
        /// </remarks>
        [MaxLength(255)]
        [StringLength(255)]
        public string AuthenticationProviderId { get; set; }

        /// <summary>
        /// Gets or sets the password reset provider id.
        /// </summary>
        /// <remarks>
        /// Required, Max length = 255.
        /// </remarks>
        [MaxLength(255)]
        [StringLength(255)]
        public string PasswordResetProviderId { get; set; }

        /// <summary>
        /// Gets or sets the invalid login attempt count.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public int InvalidLoginAttemptCount { get; set; }

        /// <summary>
        /// Gets or sets the last activity date.
        /// </summary>
        public DateTime? LastActivityDate { get; set; }

        /// <summary>
        /// Gets or sets the last login date.
        /// </summary>
        public DateTime? LastLoginDate { get; set; }

        /// <summary>
        /// Gets or sets the number of login attempts the user can make before they are locked out.
        /// </summary>
        public int? LoginAttemptsBeforeLockout { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of active sessions the user can have at once.
        /// </summary>
        public int MaxActiveSessions { get; set; }

        /// <summary>
        /// Gets or sets the subtitle mode.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public SubtitlePlaybackMode SubtitleMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the default audio track should be played.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public bool PlayDefaultAudioTrack { get; set; }

        /// <summary>
        /// Gets or sets the subtitle language preference.
        /// </summary>
        /// <remarks>
        /// Max length = 255.
        /// </remarks>
        [MaxLength(255)]
        [StringLength(255)]
        public string? SubtitleLanguagePreference { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether missing episodes should be displayed.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public bool DisplayMissingEpisodes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to display the collections view.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public bool DisplayCollectionsView { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user has a local password.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public bool EnableLocalPassword { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the server should hide played content in "Latest".
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public bool HidePlayedInLatest { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to remember audio selections on played content.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public bool RememberAudioSelections { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to remember subtitle selections on played content.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public bool RememberSubtitleSelections { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable auto-play for the next episode.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public bool EnableNextEpisodeAutoPlay { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user should auto-login.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public bool EnableAutoLogin { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user can change their preferences.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public bool EnableUserPreferenceAccess { get; set; }

        /// <summary>
        /// Gets or sets the maximum parental age rating.
        /// </summary>
        public int? MaxParentalAgeRating { get; set; }

        /// <summary>
        /// Gets or sets the remote client bitrate limit.
        /// </summary>
        public int? RemoteClientBitrateLimit { get; set; }

        /// <summary>
        /// Gets or sets the internal id.
        /// This is a temporary stopgap for until the library db is migrated.
        /// This corresponds to the value of the index of this user in the library db.
        /// </summary>
        public long InternalId { get; set; }

        /// <summary>
        /// Gets or sets the user's profile image. Can be <c>null</c>.
        /// </summary>
        // [ForeignKey("UserId")]
        public virtual ImageInfo? ProfileImage { get; set; }

        /// <summary>
        /// Gets the user's display preferences.
        /// </summary>
        public virtual ICollection<DisplayPreferences> DisplayPreferences { get; private set; }

        /// <summary>
        /// Gets or sets the level of sync play permissions this user has.
        /// </summary>
        public SyncPlayUserAccessType SyncPlayAccess { get; set; }

        /// <summary>
        /// Gets or sets the cast receiver id.
        /// </summary>
        [StringLength(32)]
        public string? CastReceiverId { get; set; }

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; private set; }

        /// <summary>
        /// Gets the list of access schedules this user has.
        /// </summary>
        public virtual ICollection<AccessSchedule> AccessSchedules { get; private set; }

        /// <summary>
        /// Gets the list of item display preferences.
        /// </summary>
        public virtual ICollection<ItemDisplayPreferences> ItemDisplayPreferences { get; private set; }

        /*
        /// <summary>
        /// Gets the list of groups this user is a member of.
        /// </summary>
        public virtual ICollection<Group> Groups { get; private set; }
        */

        /// <summary>
        /// Gets the list of permissions this user has.
        /// </summary>
        [ForeignKey("Permission_Permissions_Guid")]
        public virtual ICollection<Permission> Permissions { get; private set; }

        /*
        /// <summary>
        /// Gets the list of provider mappings this user has.
        /// </summary>
        public virtual ICollection<ProviderMapping> ProviderMappings { get; private set; }
        */

        /// <summary>
        /// Gets the list of preferences this user has.
        /// </summary>
        [ForeignKey("Preference_Preferences_Guid")]
        public virtual ICollection<Preference> Preferences { get; private set; }

        /// <inheritdoc/>
        public void OnSavingChanges()
        {
            RowVersion++;
        }

        /// <summary>
        /// Checks whether the user has the specified permission.
        /// </summary>
        /// <param name="kind">The permission kind.</param>
        /// <returns><c>True</c> if the user has the specified permission.</returns>
        public bool HasPermission(PermissionKind kind)
        {
            return Permissions.FirstOrDefault(p => p.Kind == kind)?.Value ?? false;
        }

        /// <summary>
        /// Sets the given permission kind to the provided value.
        /// </summary>
        /// <param name="kind">The permission kind.</param>
        /// <param name="value">The value to set.</param>
        public void SetPermission(PermissionKind kind, bool value)
        {
            var currentPermission = Permissions.FirstOrDefault(p => p.Kind == kind);
            if (currentPermission is null)
            {
                Permissions.Add(new Permission(kind, value));
            }
            else
            {
                currentPermission.Value = value;
            }
        }

        /// <summary>
        /// Gets the user's preferences for the given preference kind.
        /// </summary>
        /// <param name="preference">The preference kind.</param>
        /// <returns>A string array containing the user's preferences.</returns>
        public string[] GetPreference(PreferenceKind preference)
        {
            var val = Preferences.FirstOrDefault(p => p.Kind == preference)?.Value;

            return string.IsNullOrEmpty(val) ? Array.Empty<string>() : val.Split(Delimiter);
        }

        /// <summary>
        /// Gets the user's preferences for the given preference kind.
        /// </summary>
        /// <param name="preference">The preference kind.</param>
        /// <typeparam name="T">Type of preference.</typeparam>
        /// <returns>A {T} array containing the user's preference.</returns>
        public T[] GetPreferenceValues<T>(PreferenceKind preference)
        {
            var val = Preferences.FirstOrDefault(p => p.Kind == preference)?.Value;
            if (string.IsNullOrEmpty(val))
            {
                return Array.Empty<T>();
            }

            // Convert array of {string} to array of {T}
            var converter = TypeDescriptor.GetConverter(typeof(T));
            var stringValues = val.Split(Delimiter);
            var convertedCount = 0;
            var parsedValues = new T[stringValues.Length];
            for (var i = 0; i < stringValues.Length; i++)
            {
                try
                {
                    var parsedValue = converter.ConvertFromString(stringValues[i].Trim());
                    if (parsedValue is not null)
                    {
                        parsedValues[convertedCount++] = (T)parsedValue;
                    }
                }
                catch (FormatException)
                {
                    // Unable to convert value
                }
            }

            return parsedValues[..convertedCount];
        }

        /// <summary>
        /// Sets the specified preference to the given value.
        /// </summary>
        /// <param name="preference">The preference kind.</param>
        /// <param name="values">The values.</param>
        public void SetPreference(PreferenceKind preference, string[] values)
        {
            var value = string.Join(Delimiter, values);
            var currentPreference = Preferences.FirstOrDefault(p => p.Kind == preference);
            if (currentPreference is null)
            {
                Preferences.Add(new Preference(preference, value));
            }
            else
            {
                currentPreference.Value = value;
            }
        }

        /// <summary>
        /// Sets the specified preference to the given value.
        /// </summary>
        /// <param name="preference">The preference kind.</param>
        /// <param name="values">The values.</param>
        /// <typeparam name="T">The type of value.</typeparam>
        public void SetPreference<T>(PreferenceKind preference, T[] values)
        {
            var value = string.Join(Delimiter, values);
            var currentPreference = Preferences.FirstOrDefault(p => p.Kind == preference);
            if (currentPreference is null)
            {
                Preferences.Add(new Preference(preference, value));
            }
            else
            {
                currentPreference.Value = value;
            }
        }

        /// <summary>
        /// Checks whether this user is currently allowed to use the server.
        /// </summary>
        /// <returns><c>True</c> if the current time is within an access schedule, or there are no access schedules.</returns>
        public bool IsParentalScheduleAllowed()
        {
            return AccessSchedules.Count == 0
                   || AccessSchedules.Any(i => IsParentalScheduleAllowed(i, DateTime.UtcNow));
        }

        /// <summary>
        /// Checks whether the provided folder is in this user's grouped folders.
        /// </summary>
        /// <param name="id">The Guid of the folder.</param>
        /// <returns><c>True</c> if the folder is in the user's grouped folders.</returns>
        public bool IsFolderGrouped(Guid id)
        {
            return Array.IndexOf(GetPreferenceValues<Guid>(PreferenceKind.GroupedFolders), id) != -1;
        }

        /// <summary>
        /// Initializes the default permissions for a user. Should only be called on user creation.
        /// </summary>
        // TODO: make these user configurable?
        public void AddDefaultPermissions()
        {
            Permissions.Add(new Permission(PermissionKind.IsAdministrator, false));
            Permissions.Add(new Permission(PermissionKind.IsDisabled, false));
            Permissions.Add(new Permission(PermissionKind.IsHidden, true));
            Permissions.Add(new Permission(PermissionKind.EnableAllChannels, true));
            Permissions.Add(new Permission(PermissionKind.EnableAllDevices, true));
            Permissions.Add(new Permission(PermissionKind.EnableAllFolders, true));
            Permissions.Add(new Permission(PermissionKind.EnableContentDeletion, false));
            Permissions.Add(new Permission(PermissionKind.EnableContentDownloading, true));
            Permissions.Add(new Permission(PermissionKind.EnableMediaConversion, true));
            Permissions.Add(new Permission(PermissionKind.EnableMediaPlayback, true));
            Permissions.Add(new Permission(PermissionKind.EnablePlaybackRemuxing, true));
            Permissions.Add(new Permission(PermissionKind.EnablePublicSharing, true));
            Permissions.Add(new Permission(PermissionKind.EnableRemoteAccess, true));
            Permissions.Add(new Permission(PermissionKind.EnableSyncTranscoding, true));
            Permissions.Add(new Permission(PermissionKind.EnableAudioPlaybackTranscoding, true));
            Permissions.Add(new Permission(PermissionKind.EnableLiveTvAccess, true));
            Permissions.Add(new Permission(PermissionKind.EnableLiveTvManagement, true));
            Permissions.Add(new Permission(PermissionKind.EnableSharedDeviceControl, true));
            Permissions.Add(new Permission(PermissionKind.EnableVideoPlaybackTranscoding, true));
            Permissions.Add(new Permission(PermissionKind.ForceRemoteSourceTranscoding, false));
            Permissions.Add(new Permission(PermissionKind.EnableRemoteControlOfOtherUsers, false));
            Permissions.Add(new Permission(PermissionKind.EnableCollectionManagement, false));
            Permissions.Add(new Permission(PermissionKind.EnableSubtitleManagement, false));
            Permissions.Add(new Permission(PermissionKind.EnableLyricManagement, false));
        }

        /// <summary>
        /// Initializes the default preferences. Should only be called on user creation.
        /// </summary>
        public void AddDefaultPreferences()
        {
            foreach (var val in Enum.GetValues<PreferenceKind>())
            {
                Preferences.Add(new Preference(val, string.Empty));
            }
        }

        private static bool IsParentalScheduleAllowed(AccessSchedule schedule, DateTime date)
        {
            var localTime = date.ToLocalTime();
            var hour = localTime.TimeOfDay.TotalHours;
            var currentDayOfWeek = localTime.DayOfWeek;

            return schedule.DayOfWeek.Contains(currentDayOfWeek)
                   && hour >= schedule.StartHour
                   && hour <= schedule.EndHour;
        }
    }
}
