namespace MediaBrowser.Model.Configuration
{
    public enum NotificationType
    {
        ApplicationUpdateAvailable,
        ApplicationUpdateInstalled,
        AudioPlayback,
        GamePlayback,
        VideoPlayback,
        AudioPlaybackStopped,
        GamePlaybackStopped,
        VideoPlaybackStopped,
        InstallationFailed,
        PluginError,
        PluginInstalled,
        PluginUpdateInstalled,
        PluginUninstalled,
        NewLibraryContent,
        NewLibraryContentMultiple,
        ServerRestartRequired,
        TaskFailed
    }
}