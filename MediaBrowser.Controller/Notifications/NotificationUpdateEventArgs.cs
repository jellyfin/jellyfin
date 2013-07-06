using MediaBrowser.Model.Notifications;
using System;

namespace MediaBrowser.Controller.Notifications
{
    public class NotificationUpdateEventArgs : EventArgs
    {
        public Notification Notification { get; set; }
    }

    public class NotificationReadEventArgs : EventArgs
    {
        public Guid[] IdList { get; set; }
        public Guid UserId { get; set; }
        public bool IsRead { get; set; }
    }
}
