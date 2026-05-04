using MediaBrowser.Controller.Events.Updates;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Notifications;

namespace Jellyfin.Server.Implementations.Events.Consumers.Updates
{
    /// <summary>
    /// Creates an entry in the activity log when a plugin is updated.
    /// </summary>
    public class PluginUpdatedLogger : PluginActivityLogConsumer<PluginUpdatedEventArgs>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginUpdatedLogger"/> class.
        /// </summary>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="activityManager">The activity manager.</param>
        public PluginUpdatedLogger(ILocalizationManager localizationManager, IActivityManager activityManager)
            : base(
                localizationManager,
                activityManager,
                "PluginUpdatedWithName",
                NotificationType.PluginUpdateInstalled,
                eventArgs => eventArgs.Argument.Name,
                eventArgs => eventArgs.Argument.Version,
                eventArgs => eventArgs.Argument.Changelog)
        {
        }
    }
}
