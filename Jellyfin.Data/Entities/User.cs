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
    public partial class User
    {
        /// <summary>
        /// The values being delimited here are Guids, so commas work as they do not appear in Guids.
        /// </summary>
        private const char Delimiter = ',';

        partial void Init();

        /// <summary>
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
        /// Public constructor with required data
        /// </summary>
        /// <param name="username"></param>
        /// <param name="mustUpdatePassword"></param>
        /// <param name="authenticationProviderId"></param>
        /// <param name="invalidLoginAttemptCount"></param>
        /// <param name="subtitleMode"></param>
        /// <param name="playDefaultAudioTrack"></param>
        public User(
            string username,
            bool mustUpdatePassword,
            string authenticationProviderId,
            int invalidLoginAttemptCount,
            SubtitlePlaybackMode subtitleMode,
            bool playDefaultAudioTrack)
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
            MustUpdatePassword = mustUpdatePassword;
            AuthenticationProviderId = authenticationProviderId;
            InvalidLoginAttemptCount = invalidLoginAttemptCount;
            SubtitleMode = subtitleMode;
            PlayDefaultAudioTrack = playDefaultAudioTrack;

            Groups = new HashSet<Group>();
            Permissions = new HashSet<Permission>();
            ProviderMappings = new HashSet<ProviderMapping>();
            Preferences = new HashSet<Preference>();
            AccessSchedules = new HashSet<AccessSchedule>();

            // Set default values
            Id = Guid.NewGuid();
            DisplayMissingEpisodes = false;
            DisplayCollectionsView = false;
            HidePlayedInLatest = true;
            RememberAudioSelections = true;
            RememberSubtitleSelections = true;
            EnableNextEpisodeAutoPlay = true;
            EnableAutoLogin = false;

            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static User CreateUserUnsafe()
        {
            return new User();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="username"></param>
        /// <param name="mustUpdatePassword"></param>
        /// <param name="authenticationProviderId"></param>
        /// <param name="invalidLoginAttemptCount"></param>
        /// <param name="subtitleMode"></param>
        /// <param name="playDefaultAudioTrack"></param>
        public static User Create(
            string username,
            bool mustUpdatePassword,
            string authenticationProviderId,
            int invalidLoginAttemptCount,
            SubtitlePlaybackMode subtitleMode,
            bool playDefaultAudioTrack)
        {
            return new User(username, mustUpdatePassword, authenticationProviderId, invalidLoginAttemptCount, subtitleMode, playDefaultAudioTrack);
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
        [JsonPropertyName("Name")]
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
        /// This is a temporary stopgap for until the library db is migrated.
        /// This corresponds to the value of the index of this user in the library db.
        /// </summary>
        [Required]
        public long InternalId { get; set; }

        public ImageInfo ProfileImage { get; set; }

        /// <summary>
        /// Required, ConcurrenyToken
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
        [ForeignKey("Group_Groups_Id")]
        public ICollection<Group> Groups { get; protected set; }

        [ForeignKey("Permission_Permissions_Id")]
        public ICollection<Permission> Permissions { get; protected set; }

        [ForeignKey("ProviderMapping_ProviderMappings_Id")]
        public ICollection<ProviderMapping> ProviderMappings { get; protected set; }

        [ForeignKey("Preference_Preferences_Id")]
        public ICollection<Preference> Preferences { get; protected set; }

        public ICollection<AccessSchedule> AccessSchedules { get; protected set; }

        public bool HasPermission(PermissionKind permission)
        {
            return Permissions.Select(p => p.Kind).Contains(permission);
        }

        public void SetPermission(PermissionKind kind, bool value)
        {
            var permissionObj = Permissions.First(p => p.Kind == kind);
            permissionObj.Value = value;
        }

        public string[] GetPreference(PreferenceKind preference)
        {
            return Preferences
                .Where(p => p.Kind == preference)
                .Select(p => p.Value)
                .First()
                .Split(Delimiter);
        }

        public void SetPreference(PreferenceKind preference, string[] values)
        {
            var pref = Preferences.First(p => p.Kind == preference);

            pref.Value = string.Join(Delimiter.ToString(CultureInfo.InvariantCulture), values);
        }

        public bool IsParentalScheduleAllowed()
        {
            var schedules = this.AccessSchedules;

            return schedules.Count == 0 || schedules.Any(i => IsParentalScheduleAllowed(i, DateTime.Now));
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
    }
}
