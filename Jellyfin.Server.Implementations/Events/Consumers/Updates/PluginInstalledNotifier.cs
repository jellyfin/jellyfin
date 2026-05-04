using MediaBrowser.Controller.Events.Updates;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Updates;

namespace Jellyfin.Server.Implementations.Events.Consumers.Updates
{
    /// <summary>
    /// Notifies admin users when a plugin is installed.
    /// </summary>
    public class PluginInstalledNotifier : PluginNotificationConsumer<PluginInstalledEventArgs, InstallationInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginInstalledNotifier"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public PluginInstalledNotifier(ISessionManager sessionManager)
            : base(sessionManager, SessionMessageType.PackageInstallationCompleted)
        {
        }

        /// <inheritdoc />
        protected override InstallationInfo GetMessageData(PluginInstalledEventArgs eventArgs) => eventArgs.Argument;
    }
}
