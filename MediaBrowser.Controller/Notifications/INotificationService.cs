#nullable disable

#pragma warning disable CS1591

using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;

namespace MediaBrowser.Controller.Notifications
{
    public interface INotificationService
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Sends the notification.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendNotification(UserNotification request, CancellationToken cancellationToken);

        /// <summary>
        /// Determines whether [is enabled for user] [the specified user identifier].
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if [is enabled for user] [the specified user identifier]; otherwise, <c>false</c>.</returns>
        bool IsEnabledForUser(User user);
    }
}
