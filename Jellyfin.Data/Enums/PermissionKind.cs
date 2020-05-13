namespace Jellyfin.Data.Enums
{
    public enum PermissionKind
    {
        IsAdministrator,
        IsHidden,
        IsDisabled,
        EnableSharedDeviceControl,
        EnableRemoteAccess,
        EnableLiveTvManagement,
        EnableLiveTvAccess,
        EnableMediaPlayback,
        EnableAudioPlaybackTranscoding,
        EnableVideoPlaybackTranscoding,
        EnableContentDeletion,
        EnableContentDownloading,
        EnableSyncTranscoding,
        EnableMediaConversion,
        EnableAllDevices,
        EnableAllChannels,
        EnableAllFolders,
        EnablePublicSharing,
        EnableRemoteControlOfOtherUsers,
        EnablePlaybackRemuxing,
        ForceRemoteSourceTranscoding
    }
}
