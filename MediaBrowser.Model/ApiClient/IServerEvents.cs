using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Updates;
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
        event EventHandler<GenericEventArgs<string>> UserDeleted;
        /// <summary>
        /// Occurs when [scheduled task started].
        /// </summary>
        event EventHandler<GenericEventArgs<string>> ScheduledTaskStarted;
        /// <summary>
        /// Occurs when [scheduled task ended].
        /// </summary>
        event EventHandler<GenericEventArgs<TaskResult>> ScheduledTaskEnded;
        /// <summary>
        /// Occurs when [package installing].
        /// </summary>
        event EventHandler<GenericEventArgs<InstallationInfo>> PackageInstalling;
        /// <summary>
        /// Occurs when [package installation failed].
        /// </summary>
        event EventHandler<GenericEventArgs<InstallationInfo>> PackageInstallationFailed;
        /// <summary>
        /// Occurs when [package installation completed].
        /// </summary>
        event EventHandler<GenericEventArgs<InstallationInfo>> PackageInstallationCompleted;
        /// <summary>
        /// Occurs when [package installation cancelled].
        /// </summary>
        event EventHandler<GenericEventArgs<InstallationInfo>> PackageInstallationCancelled;
        /// <summary>
        /// Occurs when [user updated].
        /// </summary>
        event EventHandler<GenericEventArgs<UserDto>> UserUpdated;
        /// <summary>
        /// Occurs when [plugin uninstalled].
        /// </summary>
        event EventHandler<GenericEventArgs<PluginInfo>> PluginUninstalled;
        /// <summary>
        /// Occurs when [library changed].
        /// </summary>
        event EventHandler<GenericEventArgs<LibraryUpdateInfo>> LibraryChanged;
        /// <summary>
        /// Occurs when [browse command].
        /// </summary>
        event EventHandler<GenericEventArgs<BrowseRequest>> BrowseCommand;
        /// <summary>
        /// Occurs when [play command].
        /// </summary>
        event EventHandler<GenericEventArgs<PlayRequest>> PlayCommand;
        /// <summary>
        /// Occurs when [playstate command].
        /// </summary>
        event EventHandler<GenericEventArgs<PlaystateRequest>> PlaystateCommand;
        /// <summary>
        /// Occurs when [message command].
        /// </summary>
        event EventHandler<GenericEventArgs<MessageCommand>> MessageCommand;
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
        /// Occurs when [send text command].
        /// </summary>
        event EventHandler<GenericEventArgs<string>> SendTextCommand;
        /// <summary>
        /// Occurs when [set volume command].
        /// </summary>
        event EventHandler<GenericEventArgs<int>> SetVolumeCommand;
        /// <summary>
        /// Occurs when [set audio stream index command].
        /// </summary>
        event EventHandler<GenericEventArgs<int>> SetAudioStreamIndexCommand;
        /// <summary>
        /// Occurs when [set video stream index command].
        /// </summary>
        event EventHandler<GenericEventArgs<int>> SetVideoStreamIndexCommand;
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
        event EventHandler<GenericEventArgs<UserDataChangeInfo>> UserDataChanged;
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
