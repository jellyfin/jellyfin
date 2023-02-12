using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Mediator;
using Jellyfin.Data.Users.Notifications;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;

namespace Jellyfin.Server.Implementations.Activity.Handlers;

/// <summary>
/// Creates an entry in the activity log when a user is created.
/// </summary>
public class UserCreatedLogger : INotificationHandler<UserCreatedNotification>
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
    public async ValueTask Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _activityManager.CreateAsync(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localizationManager.GetLocalizedString("UserCreatedWithName"),
                    notification.User.Username),
                "UserCreated",
                notification.User.Id))
            .ConfigureAwait(false);
    }
}
