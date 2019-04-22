using System.Collections.Generic;
using Jellyfin.Model.Notifications;

namespace Jellyfin.Controller.Notifications
{
    public interface INotificationTypeFactory
    {
        /// <summary>
        /// Gets the notification types.
        /// </summary>
        /// <returns>IEnumerable{NotificationTypeInfo}.</returns>
        IEnumerable<NotificationTypeInfo> GetNotificationTypes();
    }
}
