using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Events.Updates;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using Rebus.Handlers;

namespace Jellyfin.Server.Implementations.Events.Consumers.Updates
{
    /// <summary>
    /// Notifies admin users when a plugin is being installed.
    /// </summary>
    public class PluginInstallingNotifier : IHandleMessages<PluginInstallingEventArgs>
    {
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginInstallingNotifier"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public PluginInstallingNotifier(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        /// <inheritdoc />
        public async Task Handle(PluginInstallingEventArgs message)
        {
            await _sessionManager.SendMessageToAdminSessions(SessionMessageType.PackageInstalling, message.Argument, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
