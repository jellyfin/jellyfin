using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Updates;
using System;

namespace MediaBrowser.Model.ApiClient
{
    /// <summary>
    /// Class UserDeletedEventArgs
    /// </summary>
    public class UserDeletedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }
    }

    public class UserDataChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        /// <value>The user.</value>
        public UserDataChangeInfo ChangeInfo { get; set; }
    }
    
    /// <summary>
    /// Class UserUpdatedEventArgs
    /// </summary>
    public class UserUpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        /// <value>The user.</value>
        public UserDto User { get; set; }
    }

    /// <summary>
    /// Class ScheduledTaskStartedEventArgs
    /// </summary>
    public class ScheduledTaskStartedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
    }

    /// <summary>
    /// Class ScheduledTaskEndedEventArgs
    /// </summary>
    public class ScheduledTaskEndedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the result.
        /// </summary>
        /// <value>The result.</value>
        public TaskResult Result { get; set; }
    }

    /// <summary>
    /// Class PackageInstallationEventArgs
    /// </summary>
    public class PackageInstallationEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the installation info.
        /// </summary>
        /// <value>The installation info.</value>
        public InstallationInfo InstallationInfo { get; set; }
    }

    /// <summary>
    /// Class PluginUninstallEventArgs
    /// </summary>
    public class PluginUninstallEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the plugin info.
        /// </summary>
        /// <value>The plugin info.</value>
        public PluginInfo PluginInfo { get; set; }
    }

    /// <summary>
    /// Class LibraryChangedEventArgs
    /// </summary>
    public class LibraryChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the update info.
        /// </summary>
        /// <value>The update info.</value>
        public LibraryUpdateInfo UpdateInfo { get; set; }
    }

    /// <summary>
    /// Class BrowseRequestEventArgs
    /// </summary>
    public class BrowseRequestEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the request.
        /// </summary>
        /// <value>The request.</value>
        public BrowseRequest Request { get; set; }
    }

    /// <summary>
    /// Class PlayRequestEventArgs
    /// </summary>
    public class PlayRequestEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the request.
        /// </summary>
        /// <value>The request.</value>
        public PlayRequest Request { get; set; }
    }

    /// <summary>
    /// Class PlaystateRequestEventArgs
    /// </summary>
    public class PlaystateRequestEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the request.
        /// </summary>
        /// <value>The request.</value>
        public PlaystateRequest Request { get; set; }
    }

    /// <summary>
    /// Class MessageCommandEventArgs
    /// </summary>
    public class MessageCommandEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the request.
        /// </summary>
        /// <value>The request.</value>
        public MessageCommand Request { get; set; }
    }

    /// <summary>
    /// Class SystemCommandEventArgs
    /// </summary>
    public class GeneralCommandEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the command.
        /// </summary>
        /// <value>The command.</value>
        public GeneralCommand Command { get; set; }
    }

    /// <summary>
    /// Class SessionUpdatesEventArgs
    /// </summary>
    public class SessionUpdatesEventArgs : EventArgs
    {
        public SessionInfoDto[] Sessions { get; set; }
    }
}
