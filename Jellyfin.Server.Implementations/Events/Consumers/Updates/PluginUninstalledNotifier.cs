using MediaBrowser.Controller.Events.Updates;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Session;

namespace Jellyfin.Server.Implementations.Events.Consumers.Updates
{
    /// <summary>
    /// Notifies admin users when a plugin is uninstalled.
    /// </summary>
    public class PluginUninstalledNotifier : PluginNotificationConsumer<PluginUninstalledEventArgs, PluginInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginUninstalledNotifier"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public PluginUninstalledNotifier(ISessionManager sessionManager)
            : base(sessionManager, SessionMessageType.PackageUninstalled)
        {
        }

        /// <inheritdoc />
        protected override PluginInfo GetMessageData(PluginUninstalledEventArgs eventArgs) => eventArgs.Argument;
    }
}
