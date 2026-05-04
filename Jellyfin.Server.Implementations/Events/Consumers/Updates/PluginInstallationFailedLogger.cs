using MediaBrowser.Common.Updates;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Notifications;

namespace Jellyfin.Server.Implementations.Events.Consumers.Updates
{
    /// <summary>
    /// Creates an entry in the activity log when a package installation fails.
    /// </summary>
    public class PluginInstallationFailedLogger : PluginActivityLogConsumer<InstallationFailedEventArgs>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginInstallationFailedLogger"/> class.
        /// </summary>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="activityManager">The activity manager.</param>
        public PluginInstallationFailedLogger(ILocalizationManager localizationManager, IActivityManager activityManager)
            : base(
                localizationManager,
                activityManager,
                "NameInstallFailed",
                NotificationType.InstallationFailed,
                eventArgs => eventArgs.InstallationInfo.Name,
                eventArgs => eventArgs.InstallationInfo.Version,
                eventArgs => eventArgs.Exception.Message)
        {
        }
    }
}
