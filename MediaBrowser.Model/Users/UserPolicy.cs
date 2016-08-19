using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Model.Users
{
    public class UserPolicy
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance is administrator.
        /// </summary>
        /// <value><c>true</c> if this instance is administrator; otherwise, <c>false</c>.</value>
        public bool IsAdministrator { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is hidden.
        /// </summary>
        /// <value><c>true</c> if this instance is hidden; otherwise, <c>false</c>.</value>
        public bool IsHidden { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is disabled.
        /// </summary>
        /// <value><c>true</c> if this instance is disabled; otherwise, <c>false</c>.</value>
        public bool IsDisabled { get; set; }

        /// <summary>
        /// Gets or sets the max parental rating.
        /// </summary>
        /// <value>The max parental rating.</value>
        public int? MaxParentalRating { get; set; }

        public string[] BlockedTags { get; set; }
        public bool EnableUserPreferenceAccess { get; set; }
        public AccessSchedule[] AccessSchedules { get; set; }
        public UnratedItem[] BlockUnratedItems { get; set; }
        public bool EnableRemoteControlOfOtherUsers { get; set; }
        public bool EnableSharedDeviceControl { get; set; }

        public bool EnableLiveTvManagement { get; set; }
        public bool EnableLiveTvAccess { get; set; }

        public bool EnableMediaPlayback { get; set; }
        public bool EnableAudioPlaybackTranscoding { get; set; }
        public bool EnableVideoPlaybackTranscoding { get; set; }
        public bool EnablePlaybackRemuxing { get; set; }

        public bool EnableContentDeletion { get; set; }
        public bool EnableContentDownloading { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable synchronize].
        /// </summary>
        /// <value><c>true</c> if [enable synchronize]; otherwise, <c>false</c>.</value>
        public bool EnableSync { get; set; }
        public bool EnableSyncTranscoding { get; set; }

        public string[] EnabledDevices { get; set; }
        public bool EnableAllDevices { get; set; }

        public string[] EnabledChannels { get; set; }
        public bool EnableAllChannels { get; set; }

        public string[] EnabledFolders { get; set; }
        public bool EnableAllFolders { get; set; }

        public int InvalidLoginAttemptCount { get; set; }

        public bool EnablePublicSharing { get; set; }

        public string[] BlockedMediaFolders { get; set; }
        public string[] BlockedChannels { get; set; }

        public UserPolicy()
        {
            EnableSync = true;
            EnableSyncTranscoding = true;

            EnableMediaPlayback = true;
            EnableAudioPlaybackTranscoding = true;
            EnableVideoPlaybackTranscoding = true;
            EnablePlaybackRemuxing = true;

            EnableLiveTvManagement = true;
            EnableLiveTvAccess = true;

            // Without this on by default, admins won't be able to do this
            // Improve in the future
            EnableLiveTvManagement = true;

            EnableSharedDeviceControl = true;

            BlockedTags = new string[] { };
            BlockUnratedItems = new UnratedItem[] { };

            EnableUserPreferenceAccess = true;

            AccessSchedules = new AccessSchedule[] { };

            EnableAllChannels = true;
            EnabledChannels = new string[] { };

            EnableAllFolders = true;
            EnabledFolders = new string[] { };

            EnabledDevices = new string[] { };
            EnableAllDevices = true;

            EnableContentDownloading = true;
            EnablePublicSharing = true;
        }
    }
}