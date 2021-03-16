using System.Globalization;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Events.Security;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using Rebus.Handlers;

namespace Jellyfin.Server.Implementations.Events.Consumers.Security
{
    /// <summary>
    /// Creates an entry in the activity log when there is a successful login attempt.
    /// </summary>
    public class AuthenticationSucceededLogger : IHandleMessages<AuthenticationSucceededEventArgs>
    {
        private readonly ILocalizationManager _localizationManager;
        private readonly IActivityManager _activityManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationSucceededLogger"/> class.
        /// </summary>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="activityManager">The activity manager.</param>
        public AuthenticationSucceededLogger(ILocalizationManager localizationManager, IActivityManager activityManager)
        {
            _localizationManager = localizationManager;
            _activityManager = activityManager;
        }

        /// <inheritdoc />
        public async Task Handle(AuthenticationSucceededEventArgs message)
        {
            await _activityManager.CreateAsync(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localizationManager.GetLocalizedString("AuthenticationSucceededWithUserName"),
                    message.Argument.User.Name),
                "AuthenticationSucceeded",
                message.Argument.User.Id)
            {
                ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localizationManager.GetLocalizedString("LabelIpAddressValue"),
                    message.Argument.SessionInfo.RemoteEndPoint),
            }).ConfigureAwait(false);
        }
    }
}
