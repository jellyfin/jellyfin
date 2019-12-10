using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Tasks;

namespace Emby.Server.Implementations.EntryPoints
{
    /// <summary>
    /// Class WebSocketEvents.
    /// </summary>
    public class ServerEventNotifier : IServerEntryPoint
    {
        /// <summary>
        /// The user manager.
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// The installation manager.
        /// </summary>
        private readonly IInstallationManager _installationManager;

        /// <summary>
        /// The kernel.
        /// </summary>
        private readonly IServerApplicationHost _appHost;

        /// <summary>
        /// The task manager.
        /// </summary>
        private readonly ITaskManager _taskManager;

        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerEventNotifier"/> class.
        /// </summary>
        /// <param name="appHost">The application host.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="installationManager">The installation manager.</param>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="sessionManager">The session manager.</param>
        public ServerEventNotifier(
            IServerApplicationHost appHost,
            IUserManager userManager,
            IInstallationManager installationManager,
            ITaskManager taskManager,
            ISessionManager sessionManager)
        {
            _userManager = userManager;
            _installationManager = installationManager;
            _appHost = appHost;
            _taskManager = taskManager;
            _sessionManager = sessionManager;
        }

        /// <inheritdoc />
        public Task RunAsync()
        {
            _userManager.UserDeleted += OnUserDeleted;
            _userManager.UserUpdated += OnUserUpdated;
            _userManager.UserPolicyUpdated += OnUserPolicyUpdated;
            _userManager.UserConfigurationUpdated += OnUserConfigurationUpdated;

            _appHost.HasPendingRestartChanged += OnHasPendingRestartChanged;

            _installationManager.PluginUninstalled += OnPluginUninstalled;
            _installationManager.PackageInstalling += OnPackageInstalling;
            _installationManager.PackageInstallationCancelled += OnPackageInstallationCancelled;
            _installationManager.PackageInstallationCompleted += OnPackageInstallationCompleted;
            _installationManager.PackageInstallationFailed += OnPackageInstallationFailed;

            _taskManager.TaskCompleted += OnTaskCompleted;

            return Task.CompletedTask;
        }

        private void OnPackageInstalling(object sender, InstallationEventArgs e)
        {
            SendMessageToAdminSessions("PackageInstalling", e.InstallationInfo);
        }

        private void OnPackageInstallationCancelled(object sender, InstallationEventArgs e)
        {
            SendMessageToAdminSessions("PackageInstallationCancelled", e.InstallationInfo);
        }

        private void OnPackageInstallationCompleted(object sender, InstallationEventArgs e)
        {
            SendMessageToAdminSessions("PackageInstallationCompleted", e.InstallationInfo);
        }

        private void OnPackageInstallationFailed(object sender, InstallationFailedEventArgs e)
        {
            SendMessageToAdminSessions("PackageInstallationFailed", e.InstallationInfo);
        }

        private void OnTaskCompleted(object sender, TaskCompletionEventArgs e)
        {
            SendMessageToAdminSessions("ScheduledTaskEnded", e.Result);
        }

        /// <summary>
        /// Installations the manager_ plugin uninstalled.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void OnPluginUninstalled(object sender, GenericEventArgs<IPlugin> e)
        {
            SendMessageToAdminSessions("PluginUninstalled", e.Argument.GetPluginInfo());
        }

        /// <summary>
        /// Handles the HasPendingRestartChanged event of the kernel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void OnHasPendingRestartChanged(object sender, EventArgs e)
        {
            _sessionManager.SendRestartRequiredNotification(CancellationToken.None);
        }

        /// <summary>
        /// Users the manager_ user updated.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void OnUserUpdated(object sender, GenericEventArgs<User> e)
        {
            var dto = _userManager.GetUserDto(e.Argument);

            SendMessageToUserSession(e.Argument, "UserUpdated", dto);
        }

        /// <summary>
        /// Users the manager_ user deleted.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void OnUserDeleted(object sender, GenericEventArgs<User> e)
        {
            SendMessageToUserSession(e.Argument, "UserDeleted", e.Argument.Id.ToString("N", CultureInfo.InvariantCulture));
        }

        private void OnUserPolicyUpdated(object sender, GenericEventArgs<User> e)
        {
            var dto = _userManager.GetUserDto(e.Argument);

            SendMessageToUserSession(e.Argument, "UserPolicyUpdated", dto);
        }

        private void OnUserConfigurationUpdated(object sender, GenericEventArgs<User> e)
        {
            var dto = _userManager.GetUserDto(e.Argument);

            SendMessageToUserSession(e.Argument, "UserConfigurationUpdated", dto);
        }

        private async void SendMessageToAdminSessions<T>(string name, T data)
        {
            try
            {
                await _sessionManager.SendMessageToAdminSessions(name, data, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {

            }
        }

        private async void SendMessageToUserSession<T>(User user, string name, T data)
        {
            try
            {
                await _sessionManager.SendMessageToUserSessions(
                    new List<Guid> { user.Id },
                    name,
                    data,
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {

            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                _userManager.UserDeleted -= OnUserDeleted;
                _userManager.UserUpdated -= OnUserUpdated;
                _userManager.UserPolicyUpdated -= OnUserPolicyUpdated;
                _userManager.UserConfigurationUpdated -= OnUserConfigurationUpdated;

                _installationManager.PluginUninstalled -= OnPluginUninstalled;
                _installationManager.PackageInstalling -= OnPackageInstalling;
                _installationManager.PackageInstallationCancelled -= OnPackageInstallationCancelled;
                _installationManager.PackageInstallationCompleted -= OnPackageInstallationCompleted;
                _installationManager.PackageInstallationFailed -= OnPackageInstallationFailed;

                _appHost.HasPendingRestartChanged -= OnHasPendingRestartChanged;

                _taskManager.TaskCompleted -= OnTaskCompleted;
            }
        }
    }
}
