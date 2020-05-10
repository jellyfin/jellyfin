namespace Jellyfin.Data.Enums
{
    public enum PermissionKind
    {
        IsAdministrator,
        IsHidden,
        IsDisabled,
        BlockUnrateditems,
        EnbleSharedDeviceControl,
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
        AccessSchedules
    }
}
