#nullable disable
#pragma warning disable CS1591

using System;
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
        public bool EnableRemoteAccess { get; set; }

        public bool EnableLiveTvManagement { get; set; }
        public bool EnableLiveTvAccess { get; set; }

        public bool EnableMediaPlayback { get; set; }
        public bool EnableAudioPlaybackTranscoding { get; set; }
        public bool EnableVideoPlaybackTranscoding { get; set; }
        public bool EnablePlaybackRemuxing { get; set; }
        public bool ForceRemoteSourceTranscoding { get; set; }

        public bool EnableContentDeletion { get; set; }
        public string[] EnableContentDeletionFromFolders { get; set; }
        public bool EnableContentDownloading { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable synchronize].
        /// </summary>
        /// <value><c>true</c> if [enable synchronize]; otherwise, <c>false</c>.</value>
        public bool EnableSyncTranscoding { get; set; }
        public bool EnableMediaConversion { get; set; }

        public string[] EnabledDevices { get; set; }
        public bool EnableAllDevices { get; set; }

        public string[] EnabledChannels { get; set; }
        public bool EnableAllChannels { get; set; }

        public string[] EnabledFolders { get; set; }
        public bool EnableAllFolders { get; set; }

        public int InvalidLoginAttemptCount { get; set; }
        public int LoginAttemptsBeforeLockout { get; set; }

        public bool EnablePublicSharing { get; set; }

        public string[] BlockedMediaFolders { get; set; }
        public string[] BlockedChannels { get; set; }

        public int RemoteClientBitrateLimit { get; set; }
        public string AuthenticationProviderId { get; set; }
        public string PasswordResetProviderId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating what SyncPlay features the user can access.
        /// </summary>
        /// <value>Access level to SyncPlay features.</value>
        public SyncPlayAccess SyncPlayAccess { get; set; }

        public UserPolicy()
        {
            IsHidden = true;

            EnableContentDeletion = false;
            EnableContentDeletionFromFolders = Array.Empty<string>();

            EnableSyncTranscoding = true;
            EnableMediaConversion = true;

            EnableMediaPlayback = true;
            EnableAudioPlaybackTranscoding = true;
            EnableVideoPlaybackTranscoding = true;
            EnablePlaybackRemuxing = true;
            ForceRemoteSourceTranscoding = false;
            EnableLiveTvManagement = true;
            EnableLiveTvAccess = true;

            // Without this on by default, admins won't be able to do this
            // Improve in the future
            EnableLiveTvManagement = true;

            EnableSharedDeviceControl = true;

            BlockedTags = Array.Empty<string>();
            BlockUnratedItems = Array.Empty<UnratedItem>();

            EnableUserPreferenceAccess = true;

            AccessSchedules = Array.Empty<AccessSchedule>();

            LoginAttemptsBeforeLockout = -1;

            EnableAllChannels = true;
            EnabledChannels = Array.Empty<string>();

            EnableAllFolders = true;
            EnabledFolders = Array.Empty<string>();

            EnabledDevices = Array.Empty<string>();
            EnableAllDevices = true;

            EnableContentDownloading = true;
            EnablePublicSharing = true;
            EnableRemoteAccess = true;
            SyncPlayAccess = SyncPlayAccess.CreateAndJoinGroups;
        }
    }
}
