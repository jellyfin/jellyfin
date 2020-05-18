#pragma warning disable CS1591
#pragma warning disable SA1602 // Enumeration items should be documented

namespace MediaBrowser.Model.Notifications
{
    public enum NotificationType
    {
        ApplicationUpdateAvailable,
        ApplicationUpdateInstalled,
        AudioPlayback,
        VideoPlayback,
        AudioPlaybackStopped,
        VideoPlaybackStopped,
        InstallationFailed,
        PluginError,
        PluginInstalled,
        PluginUpdateInstalled,
        PluginUninstalled,
        NewLibraryContent,
        ServerRestartRequired,
        TaskFailed,
        CameraImageUploaded,
        UserLockedOut
    }
}
