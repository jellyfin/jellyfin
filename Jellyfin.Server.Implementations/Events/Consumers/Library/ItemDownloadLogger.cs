using System.Globalization;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;

namespace Jellyfin.Server.Implementations.Events.Consumers.Library
{
    /// <summary>
    /// Creates an entry in the activity log whenever a library item is downloaded.
    /// </summary>
    public class ItemDownloadLogger : IEventConsumer<ItemDownloadEventArgs>
    {
        private readonly IActivityManager _activityManager;
        private readonly ILocalizationManager _localizationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemDownloadLogger"/> class.
        /// </summary>
        /// <param name="activityManager">The activity manager.</param>
        /// <param name="localizationManager">The localization manager.</param>
        public ItemDownloadLogger(IActivityManager activityManager, ILocalizationManager localizationManager)
        {
            _activityManager = activityManager;
            _localizationManager = localizationManager;
        }

        /// <inheritdoc/>
        public async Task OnEvent(ItemDownloadEventArgs eventArgs)
        {
            await _activityManager.CreateAsync(new ActivityLog(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        _localizationManager.GetLocalizedString("UserDownloadingItemWithValues"),
                        eventArgs.AuthInfo.User.Username,
                        eventArgs.Item.Name),
                    "UserDownloadingContent",
                    eventArgs.AuthInfo.UserId)
            {
                ShortOverview = string.Format(CultureInfo.InvariantCulture, _localizationManager.GetLocalizedString("AppDeviceValues"), eventArgs.AuthInfo.Client, eventArgs.AuthInfo.Device),
            }).ConfigureAwait(false);
        }
    }
}
