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
        
        public NotificationRequest()
        {
            UserIds = new List<string>();
            Date = DateTime.UtcNow;
        }
    }
}
