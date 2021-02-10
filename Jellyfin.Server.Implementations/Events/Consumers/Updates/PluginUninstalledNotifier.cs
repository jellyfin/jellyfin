using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Events.Updates;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using Rebus.Handlers;

namespace Jellyfin.Server.Implementations.Events.Consumers.Updates
{
    /// <summary>
    /// Notifies admin users when a plugin is uninstalled.
    /// </summary>
    public class PluginUninstalledNotifier : IHandleMessages<PluginUninstalledEventArgs>
    {
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginUninstalledNotifier"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public PluginUninstalledNotifier(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        /// <inheritdoc />
        public async Task Handle(PluginUninstalledEventArgs eventArgs)
        {
            await _sessionManager.SendMessageToAdminSessions(SessionMessageType.PackageUninstalled, eventArgs.Argument, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
