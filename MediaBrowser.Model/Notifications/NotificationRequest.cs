#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Notifications
{
    public class NotificationRequest
    {
        public NotificationRequest()
        {
            UserIds = Array.Empty<Guid>();
            Date = DateTime.UtcNow;
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Url { get; set; }

        public NotificationLevel Level { get; set; }

        public Guid[] UserIds { get; set; }

        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the corresponding type name used in configuration. Not for display.
        /// </summary>
        public string NotificationType { get; set; }

        public SendToUserType? SendToUserMode { get; set; }
    }
}
