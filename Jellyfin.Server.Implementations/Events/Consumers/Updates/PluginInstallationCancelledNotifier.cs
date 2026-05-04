using MediaBrowser.Controller.Events.Updates;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Updates;

namespace Jellyfin.Server.Implementations.Events.Consumers.Updates
{
    /// <summary>
    /// Notifies admin users when a plugin installation is cancelled.
    /// </summary>
    public class PluginInstallationCancelledNotifier : PluginNotificationConsumer<PluginInstallationCancelledEventArgs, InstallationInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginInstallationCancelledNotifier"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public PluginInstallationCancelledNotifier(ISessionManager sessionManager)
            : base(sessionManager, SessionMessageType.PackageInstallationCancelled)
        {
        }

        /// <inheritdoc />
        protected override InstallationInfo GetMessageData(PluginInstallationCancelledEventArgs eventArgs) => eventArgs.Argument;
    }
}
