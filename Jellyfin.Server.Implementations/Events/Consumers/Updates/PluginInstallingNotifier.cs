using MediaBrowser.Controller.Events.Updates;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Updates;

namespace Jellyfin.Server.Implementations.Events.Consumers.Updates
{
    /// <summary>
    /// Notifies admin users when a plugin is being installed.
    /// </summary>
    public class PluginInstallingNotifier : PluginNotificationConsumer<PluginInstallingEventArgs, InstallationInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginInstallingNotifier"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public PluginInstallingNotifier(ISessionManager sessionManager)
            : base(sessionManager, SessionMessageType.PackageInstalling)
        {
        }

        /// <inheritdoc />
        protected override InstallationInfo GetMessageData(PluginInstallingEventArgs eventArgs) => eventArgs.Argument;
    }
}
