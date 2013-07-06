using System;

namespace MediaBrowser.Model.Notifications
{
    public class Notification
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTime Date { get; set; }

        public bool IsRead { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Url { get; set; }

        public string Category { get; set; }

        public string RelatedId { get; set; }
        
        public NotificationLevel Level { get; set; }

        public Notification()
        {
            Id = Guid.NewGuid();
            Date = DateTime.UtcNow;
        }
    }
}
