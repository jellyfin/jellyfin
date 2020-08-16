using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Updates;

namespace Emby.Server.Implementations.EntryPoints
{
    /// <summary>
    /// Class WebSocketEvents.
    /// </summary>
    public class ServerEventNotifier : IServerEntryPoint
    {
        /// <summary>
        /// The installation manager.
        /// </summary>
        private readonly IInstallationManager _installationManager;

        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerEventNotifier"/> class.
        /// </summary>
        /// <param name="installationManager">The installation manager.</param>
        /// <param name="sessionManager">The session manager.</param>
        public ServerEventNotifier(
            IInstallationManager installationManager,
            ISessionManager sessionManager)
        {
            _installationManager = installationManager;
            _sessionManager = sessionManager;
        }

        /// <inheritdoc />
        public Task RunAsync()
        {
            _installationManager.PackageInstallationCancelled += OnPackageInstallationCancelled;
            _installationManager.PackageInstallationCompleted += OnPackageInstallationCompleted;
            _installationManager.PackageInstallationFailed += OnPackageInstallationFailed;

            return Task.CompletedTask;
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
                _installationManager.PackageInstallationCancelled -= OnPackageInstallationCancelled;
                _installationManager.PackageInstallationCompleted -= OnPackageInstallationCompleted;
                _installationManager.PackageInstallationFailed -= OnPackageInstallationFailed;
            }
        }
    }
}
