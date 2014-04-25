using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Notifications;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Notifications
{
    public interface INotificationManager
    {
        /// <summary>
        /// Sends the notification.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendNotification(NotificationRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="services">The services.</param>
        void AddParts(IEnumerable<INotificationService> services);
    }

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
