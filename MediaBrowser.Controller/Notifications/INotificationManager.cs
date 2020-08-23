#pragma warning disable CS1591

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Notifications;

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

        Task SendNotification(NotificationRequest request, BaseItem relatedItem, CancellationToken cancellationToken);

        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="notificationTypeFactories">The notification type factories.</param>
        void AddParts(IEnumerable<INotificationService> services, IEnumerable<INotificationTypeFactory> notificationTypeFactories);

        /// <summary>
        /// Gets the notification types.
        /// </summary>
        /// <returns>IEnumerable{NotificationTypeInfo}.</returns>
        List<NotificationTypeInfo> GetNotificationTypes();

        /// <summary>
        /// Gets the notification services.
        /// </summary>
        /// <returns>IEnumerable{NotificationServiceInfo}.</returns>
        IEnumerable<NameIdPair> GetNotificationServices();
    }
}
