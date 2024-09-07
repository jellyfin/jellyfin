using System;
using System.Globalization;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
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
            await _activityManager.CreateAsync(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localizationManager.GetLocalizedString("FailedLoginAttemptWithUserName"),
                    eventArgs.Username),
                "AuthenticationFailed",
                Guid.Empty)
            {
                LogSeverity = LogLevel.Error,
                ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localizationManager.GetLocalizedString("LabelIpAddressValue"),
                    eventArgs.RemoteEndPoint),
            }).ConfigureAwait(false);
        }
    }
}
