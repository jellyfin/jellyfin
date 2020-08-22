#pragma warning disable CS1591

using System.Collections.Generic;
using MediaBrowser.Model.Notifications;

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
