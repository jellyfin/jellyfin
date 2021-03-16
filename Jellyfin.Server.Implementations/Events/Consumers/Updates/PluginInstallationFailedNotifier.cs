using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using Rebus.Handlers;

namespace Jellyfin.Server.Implementations.Events.Consumers.Updates
{
    /// <summary>
    /// Notifies admin users when a plugin installation fails.
    /// </summary>
    public class PluginInstallationFailedNotifier : IHandleMessages<InstallationFailedEventArgs>
    {
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginInstallationFailedNotifier"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public PluginInstallationFailedNotifier(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        /// <inheritdoc />
        public async Task Handle(InstallationFailedEventArgs message)
        {
            await _sessionManager.SendMessageToAdminSessions(SessionMessageType.PackageInstallationFailed, message.InstallationInfo, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
