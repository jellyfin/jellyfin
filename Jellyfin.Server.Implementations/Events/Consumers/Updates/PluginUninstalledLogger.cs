using MediaBrowser.Controller.Events.Updates;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Notifications;

namespace Jellyfin.Server.Implementations.Events.Consumers.Updates
{
    /// <summary>
    /// Creates an entry in the activity log when a plugin is uninstalled.
    /// </summary>
    public class PluginUninstalledLogger : PluginActivityLogConsumer<PluginUninstalledEventArgs>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginUninstalledLogger"/> class.
        /// </summary>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="activityManager">The activity manager.</param>
        public PluginUninstalledLogger(ILocalizationManager localizationManager, IActivityManager activityManager)
            : base(
                localizationManager,
                activityManager,
                "PluginUninstalledWithName",
                NotificationType.PluginUninstalled,
                eventArgs => eventArgs.Argument.Name)
        {
        }
    }
}
