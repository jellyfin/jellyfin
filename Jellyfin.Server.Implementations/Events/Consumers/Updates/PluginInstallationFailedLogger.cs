using System;
using System.Globalization;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Common.Updates;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Notifications;
using Rebus.Handlers;

namespace Jellyfin.Server.Implementations.Events.Consumers.Updates
{
    /// <summary>
    /// Creates an entry in the activity log when a package installation fails.
    /// </summary>
    public class PluginInstallationFailedLogger : IHandleMessages<InstallationFailedEventArgs>
    {
        private readonly ILocalizationManager _localizationManager;
        private readonly IActivityManager _activityManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginInstallationFailedLogger"/> class.
        /// </summary>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="activityManager">The activity manager.</param>
        public PluginInstallationFailedLogger(ILocalizationManager localizationManager, IActivityManager activityManager)
        {
            _localizationManager = localizationManager;
            _activityManager = activityManager;
        }

        /// <inheritdoc />
        public async Task Handle(InstallationFailedEventArgs message)
        {
            await _activityManager.CreateAsync(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localizationManager.GetLocalizedString("NameInstallFailed"),
                    message.InstallationInfo.Name),
                NotificationType.InstallationFailed.ToString(),
                Guid.Empty)
            {
                ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localizationManager.GetLocalizedString("VersionNumber"),
                    message.InstallationInfo.Version),
                Overview = message.Exception.Message
            }).ConfigureAwait(false);
        }
    }
}
