using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using Jellyfin.Data.Enums;

namespace Jellyfin.Data.Entities
{
    /// <summary>
    /// An entity representing a user.
    /// </summary>
    public partial class User : IHasPermissions, ISavingChanges
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
        /// <param name="authenticationProviderId">The authentication provider's Id</param>
        public User(string username, string authenticationProviderId, string passwordResetProviderId)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentNullException(nameof(username));
            }

            if (string.IsNullOrEmpty(authenticationProviderId))
            {
                throw new ArgumentNullException(nameof(authenticationProviderId));
            }

            Username = username;
            AuthenticationProviderId = authenticationProviderId;
            PasswordResetProviderId = passwordResetProviderId;

            Groups = new HashSet<Group>();
            Permissions = new HashSet<Permission>();
            ProviderMappings = new HashSet<ProviderMapping>();
            Preferences = new HashSet<Preference>();
            AccessSchedules = new HashSet<AccessSchedule>();

            // Set default values
            Id = Guid.NewGuid();
            InvalidLoginAttemptCount = 0;
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

            AddDefaultPermissions();
            AddDefaultPreferences();
            Init();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="User"/> class.
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected User()
        {
            Groups = new HashSet<Group>();
            Permissions = new HashSet<Permission>();
            ProviderMappings = new HashSet<ProviderMapping>();
            Preferences = new HashSet<Preference>();
            AccessSchedules = new HashSet<AccessSchedule>();

            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="username">The username for the created user.</param>
        /// <param name="authenticationProviderId">The Id of the user's authentication provider.</param>
        /// <param name="passwordResetProviderId">The Id of the user's password reset provider.</param>
        /// <returns>The created instance.</returns>
        public static User Create(string username, string authenticationProviderId, string passwordResetProviderId)
        {
            return new User(username, authenticationProviderId, passwordResetProviderId);
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /// <summary>
        /// Identity, Indexed, Required
        /// </summary>
        [Key]
        [Required]
        [JsonIgnore]
        public Guid Id { get; set; }

        /// <summary>
        /// Required, Max length = 255
        /// </summary>
        [Required]
        [MaxLength(255)]
        [StringLength(255)]
        public string Username { get; set; }

        /// <summary>
        /// Max length = 65535
        /// </summary>
        [MaxLength(65535)]
        [StringLength(65535)]
        public string Password { get; set; }

        /// <summary>
        /// Max length = 65535.
        /// </summary>
        [MaxLength(65535)]
        [StringLength(65535)]
        public string EasyPassword { get; set; }

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public bool MustUpdatePassword { get; set; }

        /// <summary>
        /// Max length = 255.
        /// </summary>
        [MaxLength(255)]
        [StringLength(255)]
        public string AudioLanguagePreference { get; set; }

        /// <summary>
        /// Required, Max length = 255
        /// </summary>
        [Required]
        [MaxLength(255)]
        [StringLength(255)]
        public string AuthenticationProviderId { get; set; }

        [Required]
        [MaxLength(255)]
        [StringLength(255)]
        public string PasswordResetProviderId { get; set; }

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public int InvalidLoginAttemptCount { get; set; }

        public DateTime LastActivityDate { get; set; }

        public DateTime LastLoginDate { get; set; }

        public int? LoginAttemptsBeforeLockout { get; set; }

        /// <summary>
        /// Required.
        /// </summary>
        [Required]
        public SubtitlePlaybackMode SubtitleMode { get; set; }

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public bool PlayDefaultAudioTrack { get; set; }

        /// <summary>
        /// Gets or sets the subtitle language preference.
        /// Max length = 255
        /// </summary>
        [MaxLength(255)]
        [StringLength(255)]
        public string SubtitleLanguagePreference { get; set; }

        [Required]
        public bool DisplayMissingEpisodes { get; set; }

        [Required]
        public bool DisplayCollectionsView { get; set; }

        [Required]
        public bool EnableLocalPassword { get; set; }

        [Required]
        public bool HidePlayedInLatest { get; set; }

        [Required]
        public bool RememberAudioSelections { get; set; }

        [Required]
        public bool RememberSubtitleSelections { get; set; }

        [Required]
        public bool EnableNextEpisodeAutoPlay { get; set; }

        [Required]
        public bool EnableAutoLogin { get; set; }

        [Required]
        public bool EnableUserPreferenceAccess { get; set; }

        public int? MaxParentalAgeRating { get; set; }

        public int? RemoteClientBitrateLimit { get; set; }

        /// <summary>
        /// Gets or sets the internal id.
        /// This is a temporary stopgap for until the library db is migrated.
        /// This corresponds to the value of the index of this user in the library db.
        /// </summary>
        [Required]
        public long InternalId { get; set; }

        public virtual ImageInfo ProfileImage { get; set; }

        /// <summary>
        /// Gets or sets the row version.
        /// Required, ConcurrenyToken.
        /// </summary>
        [ConcurrencyCheck]
        [Required]
        public uint RowVersion { get; set; }

        public void OnSavingChanges()
        {
            RowVersion++;
        }

        /*************************************************************************
         * Navigation properties
         *************************************************************************/
        [ForeignKey("Group_Groups_Guid")]
        public virtual ICollection<Group> Groups { get; protected set; }

        [ForeignKey("Permission_Permissions_Guid")]
        public virtual ICollection<Permission> Permissions { get; protected set; }

        [ForeignKey("ProviderMapping_ProviderMappings_Id")]
        public virtual ICollection<ProviderMapping> ProviderMappings { get; protected set; }

        [ForeignKey("Preference_Preferences_Guid")]
        public virtual ICollection<Preference> Preferences { get; protected set; }

        public virtual ICollection<AccessSchedule> AccessSchedules { get; protected set; }

        partial void Init();

        public bool HasPermission(PermissionKind permission)
        {
            return Permissions.First(p => p.Kind == permission).Value;
        }

        public void SetPermission(PermissionKind kind, bool value)
        {
            var permissionObj = Permissions.First(p => p.Kind == kind);
            permissionObj.Value = value;
        }

        public string[] GetPreference(PreferenceKind preference)
        {
            var val = Preferences
                .Where(p => p.Kind == preference)
                .Select(p => p.Value)
                .First();

            return Equals(val, string.Empty) ? Array.Empty<string>() : val.Split(Delimiter);
        }

        public void SetPreference(PreferenceKind preference, string[] values)
        {
            Preferences.First(p => p.Kind == preference).Value
                = string.Join(Delimiter.ToString(CultureInfo.InvariantCulture), values);
        }

        public bool IsParentalScheduleAllowed()
        {
            return AccessSchedules.Count == 0
                   || AccessSchedules.Any(i => IsParentalScheduleAllowed(i, DateTime.UtcNow));
        }

        public bool IsFolderGrouped(Guid id)
        {
            return GetPreference(PreferenceKind.GroupedFolders).Any(i => new Guid(i) == id);
        }

        private bool IsParentalScheduleAllowed(AccessSchedule schedule, DateTime date)
        {
            if (date.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Utc date expected");
            }

            var localTime = date.ToLocalTime();

            return DayOfWeekHelper.GetDaysOfWeek(schedule.DayOfWeek).Contains(localTime.DayOfWeek) &&
                   IsWithinTime(schedule, localTime);
        }

        private bool IsWithinTime(AccessSchedule schedule, DateTime localTime)
        {
            var hour = localTime.TimeOfDay.TotalHours;

            return hour >= schedule.StartHour && hour <= schedule.EndHour;
        }

        // TODO: make these user configurable?
        private void AddDefaultPermissions()
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
        }

        private void AddDefaultPreferences()
        {
            foreach (var val in Enum.GetValues(typeof(PreferenceKind)).Cast<PreferenceKind>())
            {
                Preferences.Add(new Preference(val, string.Empty));
            }
        }
    }
}
