using MediaBrowser.Model.Notifications;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Notifications
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
