using System.Globalization;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Events.Session;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using Rebus.Handlers;

namespace Jellyfin.Server.Implementations.Events.Consumers.Session
{
    /// <summary>
    /// Creates an entry in the activity log whenever a session ends.
    /// </summary>
    public class SessionEndedLogger : IHandleMessages<SessionEndedEventArgs>
    {
        private readonly ILocalizationManager _localizationManager;
        private readonly IActivityManager _activityManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionEndedLogger"/> class.
        /// </summary>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="activityManager">The activity manager.</param>
        public SessionEndedLogger(ILocalizationManager localizationManager, IActivityManager activityManager)
        {
            _localizationManager = localizationManager;
            _activityManager = activityManager;
        }

        /// <inheritdoc />
        public async Task Handle(SessionEndedEventArgs eventArgs)
        {
            if (string.IsNullOrEmpty(eventArgs.Argument.UserName))
            {
                return;
            }

            await _activityManager.CreateAsync(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localizationManager.GetLocalizedString("UserOfflineFromDevice"),
                    eventArgs.Argument.UserName,
                    eventArgs.Argument.DeviceName),
                "SessionEnded",
                eventArgs.Argument.UserId)
            {
                ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localizationManager.GetLocalizedString("LabelIpAddressValue"),
                    eventArgs.Argument.RemoteEndPoint),
            }).ConfigureAwait(false);
        }
    }
}
