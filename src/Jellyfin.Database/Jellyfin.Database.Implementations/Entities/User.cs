using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Database.Implementations.Interfaces;

namespace Jellyfin.Database.Implementations.Entities
{
    /// <summary>
    /// An entity representing a user.
    /// </summary>
    public class User : IHasPermissions, IHasConcurrencyToken
    {
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
        /// Gets or sets the maximum parental rating score.
        /// </summary>
        public int? MaxParentalRatingScore { get; set; }

        /// <summary>
        /// Gets or sets the maximum parental rating sub score.
        /// </summary>
        public int? MaxParentalRatingSubScore { get; set; }

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
    }
}
