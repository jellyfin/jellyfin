using MediaBrowser.Model.Notifications;

namespace Jellyfin.Api.Models.NotificationDtos
{
    /// <summary>
    /// The notification summary DTO.
    /// </summary>
    public class NotificationsSummaryDto
    {
        /// <summary>
        /// Gets or sets the number of unread notifications.
        /// </summary>
        public int UnreadCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum unread notification level.
        /// </summary>
        public NotificationLevel? MaxUnreadNotificationLevel { get; set; }
    }
}
