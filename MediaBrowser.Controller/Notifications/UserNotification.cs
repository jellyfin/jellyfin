#pragma warning disable CS1591

using System;
using Jellyfin.Data.Entities;
using MediaBrowser.Model.Notifications;

namespace MediaBrowser.Controller.Notifications
{
    public class UserNotification
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Url { get; set; }

        public NotificationLevel Level { get; set; }

        public DateTime Date { get; set; }

        public User User { get; set; }
    }
}
