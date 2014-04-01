using System;

namespace MediaBrowser.Model.ApiClient
{
    /// <summary>
    /// Interface IServerEvents
    /// </summary>
    public interface IServerEvents
    {
        /// <summary>
        /// Occurs when [user deleted].
        /// </summary>
        event EventHandler<UserDeletedEventArgs> UserDeleted;
        /// <summary>
        /// Occurs when [scheduled task started].
        /// </summary>
        event EventHandler<ScheduledTaskStartedEventArgs> ScheduledTaskStarted;
        /// <summary>
        /// Occurs when [scheduled task ended].
        /// </summary>
        event EventHandler<ScheduledTaskEndedEventArgs> ScheduledTaskEnded;
        /// <summary>
        /// Occurs when [package installing].
        /// </summary>
        event EventHandler<PackageInstallationEventArgs> PackageInstalling;
        /// <summary>
        /// Occurs when [package installation failed].
        /// </summary>
        event EventHandler<PackageInstallationEventArgs> PackageInstallationFailed;
        /// <summary>
        /// Occurs when [package installation completed].
        /// </summary>
        event EventHandler<PackageInstallationEventArgs> PackageInstallationCompleted;
        /// <summary>
        /// Occurs when [package installation cancelled].
        /// </summary>
        event EventHandler<PackageInstallationEventArgs> PackageInstallationCancelled;
        /// <summary>
        /// Occurs when [user updated].
        /// </summary>
        event EventHandler<UserUpdatedEventArgs> UserUpdated;
        /// <summary>
        /// Occurs when [plugin uninstalled].
        /// </summary>
        event EventHandler<PluginUninstallEventArgs> PluginUninstalled;
        /// <summary>
        /// Occurs when [library changed].
        /// </summary>
        event EventHandler<LibraryChangedEventArgs> LibraryChanged;
        /// <summary>
        /// Occurs when [browse command].
        /// </summary>
        event EventHandler<BrowseRequestEventArgs> BrowseCommand;
        /// <summary>
        /// Occurs when [play command].
        /// </summary>
        event EventHandler<PlayRequestEventArgs> PlayCommand;
        /// <summary>
        /// Occurs when [playstate command].
        /// </summary>
        event EventHandler<PlaystateRequestEventArgs> PlaystateCommand;
        /// <summary>
        /// Occurs when [message command].
        /// </summary>
        event EventHandler<MessageCommandEventArgs> MessageCommand;
        /// <summary>
        /// Occurs when [system command].
        /// </summary>
        event EventHandler<GeneralCommandEventArgs> GeneralCommand;
        /// <summary>
        /// Occurs when [notification added].
        /// </summary>
        event EventHandler<EventArgs> NotificationAdded;
        /// <summary>
        /// Occurs when [notification updated].
        /// </summary>
        event EventHandler<EventArgs> NotificationUpdated;
        /// <summary>
        /// Occurs when [notifications marked read].
        /// </summary>
        event EventHandler<EventArgs> NotificationsMarkedRead;
        /// <summary>
        /// Occurs when [server restarting].
        /// </summary>
        event EventHandler<EventArgs> ServerRestarting;
        /// <summary>
        /// Occurs when [server shutting down].
        /// </summary>
        event EventHandler<EventArgs> ServerShuttingDown;
        /// <summary>
        /// Occurs when [sessions updated].
        /// </summary>
        event EventHandler<SessionUpdatesEventArgs> SessionsUpdated;
        /// <summary>
        /// Occurs when [restart required].
        /// </summary>
        event EventHandler<EventArgs> RestartRequired;
        /// <summary>
        /// Occurs when [user data changed].
        /// </summary>
        event EventHandler<UserDataChangedEventArgs> UserDataChanged;
        /// <summary>
        /// Occurs when [connected].
        /// </summary>
        event EventHandler<EventArgs> Connected;
        /// <summary>
        /// Gets a value indicating whether this instance is connected.
        /// </summary>
        /// <value><c>true</c> if this instance is connected; otherwise, <c>false</c>.</value>
        bool IsConnected { get; }
    }
}
