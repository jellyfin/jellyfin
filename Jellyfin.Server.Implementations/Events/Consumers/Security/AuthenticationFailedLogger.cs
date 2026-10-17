using System;
using System.Globalization;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Events.Authentication;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Events.Consumers.Security
{
    /// <summary>
    /// Creates an entry in the activity log when there is a failed login attempt.
    /// </summary>
    public class AuthenticationFailedLogger : IEventConsumer<AuthenticationRequestEventArgs>
    {
        private readonly ILocalizationManager _localizationManager;
        private readonly IActivityManager _activityManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationFailedLogger"/> class.
        /// </summary>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="activityManager">The activity manager.</param>
        public AuthenticationFailedLogger(ILocalizationManager localizationManager, IActivityManager activityManager)
        {
            _localizationManager = localizationManager;
            _activityManager = activityManager;
        }

        /// <inheritdoc />
        public async Task OnEvent(AuthenticationRequestEventArgs eventArgs)
        {
            // Leave out a username that does not exist, so the activity log can't be filled with arbitrary text.
            var name = eventArgs.IsUnknownUser
                ? _localizationManager.GetServerLocalizedString("FailedLoginAttemptWithUnknownUser")
                : string.Format(
                    CultureInfo.InvariantCulture,
                    _localizationManager.GetServerLocalizedString("FailedLoginAttemptWithUserName"),
                    eventArgs.Username);

            await _activityManager.CreateAsync(new ActivityLog(
                name,
                "AuthenticationFailed",
                Guid.Empty)
            {
                LogSeverity = LogLevel.Error,
                ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localizationManager.GetServerLocalizedString("LabelIpAddressValue"),
                    eventArgs.RemoteEndPoint),
            }).ConfigureAwait(false);
        }
    }
}
