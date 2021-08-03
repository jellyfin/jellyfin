#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Jellyfin.Data.Enums;
using AccessSchedule = Jellyfin.Data.Entities.AccessSchedule;

namespace MediaBrowser.Model.Users
{
    public class UserPolicy
    {
        public UserPolicy()
        {
            IsHidden = true;

            EnableContentDeletion = false;
            EnableContentDeletionFromFolders = new List<string>();

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

            BlockedTags = new List<string>();
            BlockUnratedItems = new List<UnratedItem>();

            EnableUserPreferenceAccess = true;

            AccessSchedules = new List<AccessSchedule>();

            LoginAttemptsBeforeLockout = -1;

            MaxActiveSessions = 0;

            EnableAllChannels = true;
            EnabledChannels = new List<Guid>();

            EnableAllFolders = true;
            EnabledFolders = new List<Guid>();

            EnabledDevices = new List<string>();
            EnableAllDevices = true;

            EnableContentDownloading = true;
            EnablePublicSharing = true;
            EnableRemoteAccess = true;
            SyncPlayAccess = SyncPlayUserAccessType.CreateAndJoinGroups;
        }

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

        public IEnumerable<string> BlockedTags { get; set; }

        public bool EnableUserPreferenceAccess { get; set; }

        public IEnumerable<AccessSchedule> AccessSchedules { get; set; }

        public IEnumerable<UnratedItem> BlockUnratedItems { get; set; }

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

        public IEnumerable<string> EnableContentDeletionFromFolders { get; set; }

        public bool EnableContentDownloading { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable synchronize].
        /// </summary>
        /// <value><c>true</c> if [enable synchronize]; otherwise, <c>false</c>.</value>
        public bool EnableSyncTranscoding { get; set; }

        public bool EnableMediaConversion { get; set; }

        public IEnumerable<string> EnabledDevices { get; set; }

        public bool EnableAllDevices { get; set; }

        public IEnumerable<Guid> EnabledChannels { get; set; }

        public bool EnableAllChannels { get; set; }

        public IEnumerable<Guid> EnabledFolders { get; set; }

        public bool EnableAllFolders { get; set; }

        public int InvalidLoginAttemptCount { get; set; }

        public int LoginAttemptsBeforeLockout { get; set; }

        public int MaxActiveSessions { get; set; }

        public bool EnablePublicSharing { get; set; }

        public Guid[] BlockedMediaFolders { get; set; }

        public Guid[] BlockedChannels { get; set; }

        public int RemoteClientBitrateLimit { get; set; }

        [XmlElement(ElementName = "AuthenticationProviderId")]
        public string AuthenticationProviderId { get; set; }

        public string PasswordResetProviderId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating what SyncPlay features the user can access.
        /// </summary>
        /// <value>Access level to SyncPlay features.</value>
        public SyncPlayUserAccessType SyncPlayAccess { get; set; }
    }
}
