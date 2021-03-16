using System.Globalization;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Events.Users;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using Rebus.Handlers;

namespace Jellyfin.Server.Implementations.Events.Consumers.Users
{
    /// <summary>
    /// Creates an entry in the activity log when a user is created.
    /// </summary>
    public class UserCreatedLogger : IHandleMessages<UserCreatedEventArgs>
    {
        private readonly ILocalizationManager _localizationManager;
        private readonly IActivityManager _activityManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserCreatedLogger"/> class.
        /// </summary>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="activityManager">The activity manager.</param>
        public UserCreatedLogger(ILocalizationManager localizationManager, IActivityManager activityManager)
        {
            _localizationManager = localizationManager;
            _activityManager = activityManager;
        }

        /// <inheritdoc />
        public async Task Handle(UserCreatedEventArgs message)
        {
            await _activityManager.CreateAsync(new ActivityLog(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        _localizationManager.GetLocalizedString("UserCreatedWithName"),
                        message.Argument.Username),
                    "UserCreated",
                    message.Argument.Id))
                .ConfigureAwait(false);
        }
    }
}
