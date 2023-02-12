using Jellyfin.Data.Entities;
using Jellyfin.Data.Mediator;

namespace Jellyfin.Data.Users.Notifications;

/// <summary>
/// An event that occurs when a user is created.
/// </summary>
public class UserCreatedNotification : INotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserCreatedNotification"/> class.
    /// </summary>
    /// <param name="user">The user.</param>
    public UserCreatedNotification(User user)
    {
        User = user;
    }

    /// <summary>
    /// Gets the created user.
    /// </summary>
    public User User { get; }
}
