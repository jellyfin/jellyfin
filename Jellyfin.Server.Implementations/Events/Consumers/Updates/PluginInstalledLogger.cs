using MediaBrowser.Controller.Events.Updates;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Notifications;

namespace Jellyfin.Server.Implementations.Events.Consumers.Updates
{
    /// <summary>
    /// Creates an entry in the activity log when a plugin is installed.
    /// </summary>
    public class PluginInstalledLogger : PluginActivityLogConsumer<PluginInstalledEventArgs>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginInstalledLogger"/> class.
        /// </summary>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="activityManager">The activity manager.</param>
        public PluginInstalledLogger(ILocalizationManager localizationManager, IActivityManager activityManager)
            : base(
                localizationManager,
                activityManager,
                "PluginInstalledWithName",
                NotificationType.PluginInstalled,
                eventArgs => eventArgs.Argument.Name,
                eventArgs => eventArgs.Argument.Version)
        {
        }
    }
}
