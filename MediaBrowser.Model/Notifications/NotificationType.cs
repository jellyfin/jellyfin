#pragma warning disable CS1591

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
