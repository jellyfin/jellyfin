using System;
using System.Globalization;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Events.Updates;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Notifications;

namespace Jellyfin.Server.Implementations.Events.Consumers.Updates
{
    /// <summary>
    /// Creates an entry in the activity log when a plugin is updated.
    /// </summary>
    public class PluginUpdatedLogger : IEventConsumer<PluginUpdatedEventArgs>
    {
        private readonly ILocalizationManager _localizationManager;
        private readonly IActivityManager _activityManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginUpdatedLogger"/> class.
        /// </summary>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="activityManager">The activity manager.</param>
        public PluginUpdatedLogger(ILocalizationManager localizationManager, IActivityManager activityManager)
        {
            _localizationManager = localizationManager;
            _activityManager = activityManager;
        }

        /// <inheritdoc />
        public async Task OnEvent(PluginUpdatedEventArgs eventArgs)
        {
            await _activityManager.CreateAsync(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localizationManager.GetLocalizedString("PluginUpdatedWithName"),
                    eventArgs.Argument.Name),
                NotificationType.PluginUpdateInstalled.ToString(),
                Guid.Empty)
            {
                ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localizationManager.GetLocalizedString("VersionNumber"),
                    eventArgs.Argument.Version),
                Overview = eventArgs.Argument.Changelog
            }).ConfigureAwait(false);
        }
    }
}
