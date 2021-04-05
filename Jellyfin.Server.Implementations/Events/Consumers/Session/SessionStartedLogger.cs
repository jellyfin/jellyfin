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
    /// Creates an entry in the activity log when a session is started.
    /// </summary>
    public class SessionStartedLogger : IHandleMessages<SessionStartedEventArgs>
    {
        private readonly ILocalizationManager _localizationManager;
        private readonly IActivityManager _activityManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionStartedLogger"/> class.
        /// </summary>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="activityManager">The activity manager.</param>
        public SessionStartedLogger(ILocalizationManager localizationManager, IActivityManager activityManager)
        {
            _localizationManager = localizationManager;
            _activityManager = activityManager;
        }

        /// <inheritdoc />
        public async Task Handle(SessionStartedEventArgs message)
        {
            if (string.IsNullOrEmpty(message.UserName))
            {
                return;
            }

            await _activityManager.CreateAsync(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localizationManager.GetLocalizedString("UserOnlineFromDevice"),
                    message.UserName,
                    message.DeviceName),
                "SessionStarted",
                message.UserId)
            {
                ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localizationManager.GetLocalizedString("LabelIpAddressValue"),
                    message.RemoteEndPoint)
            }).ConfigureAwait(false);
        }
    }
}
