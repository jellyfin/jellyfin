using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Notifications
{
    public class Notification
    {
        public string Id { get; set; }

        public string UserId { get; set; }

        public DateTime Date { get; set; }

        public bool IsRead { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Url { get; set; }

        public NotificationLevel Level { get; set; }

        public Notification()
        {
            Date = DateTime.UtcNow;
        }
    }

    public class NotificationRequest
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Url { get; set; }

        public NotificationLevel Level { get; set; }

        public List<string> UserIds { get; set; }

        public DateTime Date { get; set; }

        /// <summary>
        /// The corresponding type name used in configuration. Not for display.
        /// </summary>
        public string NotificationType { get; set; }

        public Dictionary<string, string> Variables { get; set; }

        public NotificationRequest()
        {
            UserIds = new List<string>();
            Date = DateTime.UtcNow;

            Variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public class NotificationTypeInfo
    {
        public string Type { get; set; }

        public string Name { get; set; }

        public bool Enabled { get; set; }

        public string Category { get; set; }

        public bool IsBasedOnUserEvent { get; set; }

        public string DefaultTitle { get; set; }

        public List<string> Variables { get; set; }

        public NotificationTypeInfo()
        {
            Variables = new List<string>();
        }
    }

    public class NotificationServiceInfo
    {
        public string Name { get; set; }
        public string Id { get; set; }
    }
}
