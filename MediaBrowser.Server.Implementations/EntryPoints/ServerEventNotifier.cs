using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Sync;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MediaBrowser.Server.Implementations.EntryPoints
{
    /// <summary>
    /// Class WebSocketEvents
    /// </summary>
    public class ServerEventNotifier : IServerEntryPoint
    {
        /// <summary>
        /// The _server manager
        /// </summary>
        private readonly IServerManager _serverManager;

        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// The _installation manager
        /// </summary>
        private readonly IInstallationManager _installationManager;

        /// <summary>
        /// The _kernel
        /// </summary>
        private readonly IServerApplicationHost _appHost;

        /// <summary>
        /// The _task manager
        /// </summary>
        private readonly ITaskManager _taskManager;

        private readonly ISessionManager _sessionManager;
        private readonly ISyncManager _syncManager;

        public ServerEventNotifier(IServerManager serverManager, IServerApplicationHost appHost, IUserManager userManager, IInstallationManager installationManager, ITaskManager taskManager, ISessionManager sessionManager, ISyncManager syncManager)
        {
            _serverManager = serverManager;
            _userManager = userManager;
            _installationManager = installationManager;
            _appHost = appHost;
            _taskManager = taskManager;
            _sessionManager = sessionManager;
            _syncManager = syncManager;
        }

        public void Run()
        {
            _userManager.UserDeleted += userManager_UserDeleted;
            _userManager.UserUpdated += userManager_UserUpdated;
            _userManager.UserConfigurationUpdated += _userManager_UserConfigurationUpdated;

            _appHost.HasPendingRestartChanged += kernel_HasPendingRestartChanged;

            _installationManager.PluginUninstalled += InstallationManager_PluginUninstalled;
            _installationManager.PackageInstalling += _installationManager_PackageInstalling;
            _installationManager.PackageInstallationCancelled += _installationManager_PackageInstallationCancelled;
            _installationManager.PackageInstallationCompleted += _installationManager_PackageInstallationCompleted;
            _installationManager.PackageInstallationFailed += _installationManager_PackageInstallationFailed;

            _taskManager.TaskCompleted += _taskManager_TaskCompleted;
            _syncManager.SyncJobCreated += _syncManager_SyncJobCreated;
            _syncManager.SyncJobCancelled += _syncManager_SyncJobCancelled;
        }

        void _syncManager_SyncJobCancelled(object sender, GenericEventArgs<SyncJob> e)
        {
            _sessionManager.SendMessageToUserDeviceSessions(e.Argument.TargetId, "SyncJobCancelled", e.Argument, CancellationToken.None);
        }

        void _syncManager_SyncJobCreated(object sender, GenericEventArgs<SyncJobCreationResult> e)
        {
            _sessionManager.SendMessageToUserDeviceSessions(e.Argument.Job.TargetId, "SyncJobCreated", e.Argument, CancellationToken.None);
        }

        void _installationManager_PackageInstalling(object sender, InstallationEventArgs e)
        {
            _serverManager.SendWebSocketMessage("PackageInstalling", e.InstallationInfo);
        }

        void _installationManager_PackageInstallationCancelled(object sender, InstallationEventArgs e)
        {
            _serverManager.SendWebSocketMessage("PackageInstallationCancelled", e.InstallationInfo);
        }

        void _installationManager_PackageInstallationCompleted(object sender, InstallationEventArgs e)
        {
            _serverManager.SendWebSocketMessage("PackageInstallationCompleted", e.InstallationInfo);
        }

        void _installationManager_PackageInstallationFailed(object sender, InstallationFailedEventArgs e)
        {
            _serverManager.SendWebSocketMessage("PackageInstallationFailed", e.InstallationInfo);
        }

        void _taskManager_TaskCompleted(object sender, TaskCompletionEventArgs e)
        {
            _serverManager.SendWebSocketMessage("ScheduledTaskEnded", e.Result);
        }

        /// <summary>
        /// Installations the manager_ plugin uninstalled.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        void InstallationManager_PluginUninstalled(object sender, GenericEventArgs<IPlugin> e)
        {
            _serverManager.SendWebSocketMessage("PluginUninstalled", e.Argument.GetPluginInfo());
        }

        /// <summary>
        /// Handles the HasPendingRestartChanged event of the kernel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void kernel_HasPendingRestartChanged(object sender, EventArgs e)
        {
            _sessionManager.SendRestartRequiredNotification(CancellationToken.None);
        }

        /// <summary>
        /// Users the manager_ user updated.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        void userManager_UserUpdated(object sender, GenericEventArgs<User> e)
        {
            var dto = _userManager.GetUserDto(e.Argument);

            SendMessageToUserSession(e.Argument, "UserUpdated", dto);
        }

        /// <summary>
        /// Users the manager_ user deleted.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        void userManager_UserDeleted(object sender, GenericEventArgs<User> e)
        {
            SendMessageToUserSession(e.Argument, "UserDeleted", e.Argument.Id.ToString("N"));
        }

        void _userManager_UserConfigurationUpdated(object sender, GenericEventArgs<User> e)
        {
            var dto = _userManager.GetUserDto(e.Argument);

            SendMessageToUserSession(e.Argument, "UserConfigurationUpdated", dto);
        }

        private async void SendMessageToUserSession<T>(User user, string name, T data)
        {
            await _sessionManager.SendMessageToUserSessions(new List<string> { user.Id.ToString("N") }, name, data, CancellationToken.None);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                _userManager.UserDeleted -= userManager_UserDeleted;
                _userManager.UserUpdated -= userManager_UserUpdated;
                _userManager.UserConfigurationUpdated -= _userManager_UserConfigurationUpdated;

                _installationManager.PluginUninstalled -= InstallationManager_PluginUninstalled;
                _installationManager.PackageInstalling -= _installationManager_PackageInstalling;
                _installationManager.PackageInstallationCancelled -= _installationManager_PackageInstallationCancelled;
                _installationManager.PackageInstallationCompleted -= _installationManager_PackageInstallationCompleted;
                _installationManager.PackageInstallationFailed -= _installationManager_PackageInstallationFailed;

                _appHost.HasPendingRestartChanged -= kernel_HasPendingRestartChanged;
                _syncManager.SyncJobCreated -= _syncManager_SyncJobCreated;
                _syncManager.SyncJobCancelled -= _syncManager_SyncJobCancelled;
            }
        }
    }
}
