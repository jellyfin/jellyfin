namespace MediaBrowser.Model.Configuration
{
    public enum NotificationType
    {
        ApplicationUpdateAvailable,
        ApplicationUpdateInstalled,
        AudioPlayback,
        GamePlayback,
        InstallationFailed,
        PluginError,
        PluginInstalled,
        PluginUpdateInstalled,
        PluginUninstalled,
        NewLibraryContent,
        ServerRestartRequired,
        TaskFailed,
        VideoPlayback
    }
}