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
        public string[] IdList { get; set; }
        public string UserId { get; set; }
        public bool IsRead { get; set; }
    }
}
