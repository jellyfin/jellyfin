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
        IsAdministrator,

        /// <summary>
        /// Whether the user is hidden.
        /// </summary>
        IsHidden,

        /// <summary>
        /// Whether the user is disabled.
        /// </summary>
        IsDisabled,

        /// <summary>
        /// Whether the user can control shared devices.
        /// </summary>
        EnableSharedDeviceControl,

        /// <summary>
        /// Whether the user can access the server remotely.
        /// </summary>
        EnableRemoteAccess,

        /// <summary>
        /// Whether the user can manage live tv.
        /// </summary>
        EnableLiveTvManagement,

        /// <summary>
        /// Whether the user can access live tv.
        /// </summary>
        EnableLiveTvAccess,

        /// <summary>
        /// Whether the user can play media.
        /// </summary>
        EnableMediaPlayback,

        /// <summary>
        /// Whether the server should transcode audio for the user if requested.
        /// </summary>
        EnableAudioPlaybackTranscoding,

        /// <summary>
        /// Whether the server should transcode video for the user if requested.
        /// </summary>
        EnableVideoPlaybackTranscoding,

        /// <summary>
        /// Whether the user can delete content.
        /// </summary>
        EnableContentDeletion,

        /// <summary>
        /// Whether the user can download content.
        /// </summary>
        EnableContentDownloading,

        /// <summary>
        /// Whether to enable sync transcoding for the user.
        /// </summary>
        EnableSyncTranscoding,

        /// <summary>
        /// Whether the user can do media conversion.
        /// </summary>
        EnableMediaConversion,

        /// <summary>
        /// Whether the user has access to all devices.
        /// </summary>
        EnableAllDevices,

        /// <summary>
        /// Whether the user has access to all channels.
        /// </summary>
        EnableAllChannels,

        /// <summary>
        /// Whether the user has access to all folders.
        /// </summary>
        EnableAllFolders,

        /// <summary>
        /// Whether to enable public sharing for the user.
        /// </summary>
        EnablePublicSharing,

        /// <summary>
        /// Whether the user can remotely control other users.
        /// </summary>
        EnableRemoteControlOfOtherUsers,

        /// <summary>
        /// Whether the user is permitted to do playback remuxing.
        /// </summary>
        EnablePlaybackRemuxing,

        /// <summary>
        /// Whether the server should force transcoding on remote connections for the user.
        /// </summary>
        ForceRemoteSourceTranscoding
    }
}
