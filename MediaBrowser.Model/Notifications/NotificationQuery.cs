using System;

namespace MediaBrowser.Model.Notifications
{
    public class NotificationQuery
    {
        public Guid UserId { get; set; }

        public bool? IsRead { get; set; }

        public int? StartIndex { get; set; }

        public int? Limit { get; set; }
    }
}
