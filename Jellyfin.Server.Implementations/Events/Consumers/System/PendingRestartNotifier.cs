using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Events.System;
using MediaBrowser.Controller.Session;
using Rebus.Handlers;

namespace Jellyfin.Server.Implementations.Events.Consumers.System
{
    /// <summary>
    /// Notifies users when there is a pending restart.
    /// </summary>
    public class PendingRestartNotifier : IHandleMessages<PendingRestartEventArgs>
    {
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PendingRestartNotifier"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public PendingRestartNotifier(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        /// <inheritdoc />
        public async Task Handle(PendingRestartEventArgs message)
        {
            await _sessionManager.SendRestartRequiredNotification(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
