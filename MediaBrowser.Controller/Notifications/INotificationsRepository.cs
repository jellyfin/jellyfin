using MediaBrowser.Model.Notifications;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Notifications
{
    /// <summary>
    /// Interface INotificationsRepository
    /// </summary>
    public interface INotificationsRepository
    {
        /// <summary>
        /// Occurs when [notification added].
        /// </summary>
        event EventHandler<NotificationUpdateEventArgs> NotificationAdded;
        /// <summary>
        /// Occurs when [notifications marked read].
        /// </summary>
        event EventHandler<NotificationReadEventArgs> NotificationsMarkedRead;
        
        /// <summary>
        /// Gets the notifications.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>NotificationResult.</returns>
        NotificationResult GetNotifications(NotificationQuery query);

        /// <summary>
        /// Adds the notification.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task AddNotification(Notification notification, CancellationToken cancellationToken);

        /// <summary>
        /// Marks the read.
        /// </summary>
        /// <param name="notificationIdList">The notification id list.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="isRead">if set to <c>true</c> [is read].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task MarkRead(IEnumerable<string> notificationIdList, string userId, bool isRead, CancellationToken cancellationToken);

        /// <summary>
        /// Marks all read.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="isRead">if set to <c>true</c> [is read].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task MarkAllRead(string userId, bool isRead, CancellationToken cancellationToken);
        
        /// <summary>
        /// Gets the notifications summary.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns>NotificationsSummary.</returns>
        NotificationsSummary GetNotificationsSummary(string userId);
    }
}
