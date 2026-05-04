using MediaBrowser.Common.Updates;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Updates;

namespace Jellyfin.Server.Implementations.Events.Consumers.Updates
{
    /// <summary>
    /// Notifies admin users when a plugin installation fails.
    /// </summary>
    public class PluginInstallationFailedNotifier : PluginNotificationConsumer<InstallationFailedEventArgs, InstallationInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginInstallationFailedNotifier"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public PluginInstallationFailedNotifier(ISessionManager sessionManager)
            : base(sessionManager, SessionMessageType.PackageInstallationFailed)
        {
        }

        /// <inheritdoc />
        protected override InstallationInfo GetMessageData(InstallationFailedEventArgs eventArgs) => eventArgs.InstallationInfo;
    }
}
