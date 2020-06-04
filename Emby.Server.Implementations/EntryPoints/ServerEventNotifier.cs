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
using MediaBrowser.Model.Updates;

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

        private async void OnPackageInstalling(object sender, InstallationInfo e)
        {
            await SendMessageToAdminSessions("PackageInstalling", e).ConfigureAwait(false);
        }

        private async void OnPackageInstallationCancelled(object sender, InstallationInfo e)
        {
            await SendMessageToAdminSessions("PackageInstallationCancelled", e).ConfigureAwait(false);
        }

        private async void OnPackageInstallationCompleted(object sender, InstallationInfo e)
        {
            await SendMessageToAdminSessions("PackageInstallationCompleted", e).ConfigureAwait(false);
        }

        private async void OnPackageInstallationFailed(object sender, InstallationFailedEventArgs e)
        {
            await SendMessageToAdminSessions("PackageInstallationFailed", e.InstallationInfo).ConfigureAwait(false);
        }

        private async void OnTaskCompleted(object sender, TaskCompletionEventArgs e)
        {
            await SendMessageToAdminSessions("ScheduledTaskEnded", e.Result).ConfigureAwait(false);
        }

        /// <summary>
        /// Installations the manager_ plugin uninstalled.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private async void OnPluginUninstalled(object sender, IPlugin e)
        {
            await SendMessageToAdminSessions("PluginUninstalled", e).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles the HasPendingRestartChanged event of the kernel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private async void OnHasPendingRestartChanged(object sender, EventArgs e)
        {
            await _sessionManager.SendRestartRequiredNotification(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Users the manager_ user updated.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private async void OnUserUpdated(object sender, GenericEventArgs<User> e)
        {
            var dto = _userManager.GetUserDto(e.Argument);

            await SendMessageToUserSession(e.Argument, "UserUpdated", dto).ConfigureAwait(false);
        }

        /// <summary>
        /// Users the manager_ user deleted.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private async void OnUserDeleted(object sender, GenericEventArgs<User> e)
        {
            await SendMessageToUserSession(e.Argument, "UserDeleted", e.Argument.Id.ToString("N", CultureInfo.InvariantCulture)).ConfigureAwait(false);
        }

        private async void OnUserPolicyUpdated(object sender, GenericEventArgs<User> e)
        {
            var dto = _userManager.GetUserDto(e.Argument);

            await SendMessageToUserSession(e.Argument, "UserPolicyUpdated", dto).ConfigureAwait(false);
        }

        private async void OnUserConfigurationUpdated(object sender, GenericEventArgs<User> e)
        {
            var dto = _userManager.GetUserDto(e.Argument);

            await SendMessageToUserSession(e.Argument, "UserConfigurationUpdated", dto).ConfigureAwait(false);
        }

        private async Task SendMessageToAdminSessions<T>(string name, T data)
        {
            try
            {
                await _sessionManager.SendMessageToAdminSessions(name, data, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {

            }
        }

        private async Task SendMessageToUserSession<T>(User user, string name, T data)
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
