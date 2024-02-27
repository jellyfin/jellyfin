namespace Jellyfin.Data.Enums
{
    /// <summary>
    /// The types of user permissions.
    /// </summary>
    public enum PermissionKind
    {
        /// <summary>
        /// Whether the user is an administrator.
        /// </summary>
        IsAdministrator = 0,

        /// <summary>
        /// Whether the user is hidden.
        /// </summary>
        IsHidden = 1,

        /// <summary>
        /// Whether the user is disabled.
        /// </summary>
        IsDisabled = 2,

        /// <summary>
        /// Whether the user can control shared devices.
        /// </summary>
        EnableSharedDeviceControl = 3,

        /// <summary>
        /// Whether the user can access the server remotely.
        /// </summary>
        EnableRemoteAccess = 4,

        /// <summary>
        /// Whether the user can manage live tv.
        /// </summary>
        EnableLiveTvManagement = 5,

        /// <summary>
        /// Whether the user can access live tv.
        /// </summary>
        EnableLiveTvAccess = 6,

        /// <summary>
        /// Whether the user can play media.
        /// </summary>
        EnableMediaPlayback = 7,

        /// <summary>
        /// Whether the server should transcode audio for the user if requested.
        /// </summary>
        EnableAudioPlaybackTranscoding = 8,

        /// <summary>
        /// Whether the server should transcode video for the user if requested.
        /// </summary>
        EnableVideoPlaybackTranscoding = 9,

        /// <summary>
        /// Whether the user can delete content.
        /// </summary>
        EnableContentDeletion = 10,

        /// <summary>
        /// Whether the user can download content.
        /// </summary>
        EnableContentDownloading = 11,

        /// <summary>
        /// Whether to enable sync transcoding for the user.
        /// </summary>
        EnableSyncTranscoding = 12,

        /// <summary>
        /// Whether the user can do media conversion.
        /// </summary>
        EnableMediaConversion = 13,

        /// <summary>
        /// Whether the user has access to all devices.
        /// </summary>
        EnableAllDevices = 14,

        /// <summary>
        /// Whether the user has access to all channels.
        /// </summary>
        EnableAllChannels = 15,

        /// <summary>
        /// Whether the user has access to all folders.
        /// </summary>
        EnableAllFolders = 16,

        /// <summary>
        /// Whether to enable public sharing for the user.
        /// </summary>
        EnablePublicSharing = 17,

        /// <summary>
        /// Whether the user can remotely control other users.
        /// </summary>
        EnableRemoteControlOfOtherUsers = 18,

        /// <summary>
        /// Whether the user is permitted to do playback remuxing.
        /// </summary>
        EnablePlaybackRemuxing = 19,

        /// <summary>
        /// Whether the server should force transcoding on remote connections for the user.
        /// </summary>
        ForceRemoteSourceTranscoding = 20,

        /// <summary>
        /// Whether the user can create, modify and delete collections.
        /// </summary>
        EnableCollectionManagement = 21,

        /// <summary>
        /// Whether the user can edit subtitles.
        /// </summary>
        EnableSubtitleManagement = 22,

        /// <summary>
        /// Whether the user can edit lyrics.
        /// </summary>
        EnableLyricManagement = 23,
    }
}
