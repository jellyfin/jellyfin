﻿using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Events.Updates;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using Rebus.Handlers;

namespace Jellyfin.Server.Implementations.Events.Consumers.Updates
{
    /// <summary>
    /// Notifies admin users when a plugin is installed.
    /// </summary>
    public class PluginInstalledNotifier : IHandleMessages<PluginInstalledEventArgs>
    {
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginInstalledNotifier"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public PluginInstalledNotifier(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        /// <inheritdoc />
        public async Task Handle(PluginInstalledEventArgs eventArgs)
        {
            await _sessionManager.SendMessageToAdminSessions(SessionMessageType.PackageInstallationCompleted, eventArgs.Argument, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
