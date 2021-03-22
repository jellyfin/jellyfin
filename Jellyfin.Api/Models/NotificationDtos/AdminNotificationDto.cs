using MediaBrowser.Model.Notifications;

namespace Jellyfin.Api.Models.NotificationDtos
{
    /// <summary>
    /// The admin notification dto.
    /// </summary>
    public class AdminNotificationDto
    {
        /// <summary>
        /// Gets or sets the notification name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the notification description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the notification level.
        /// </summary>
        public NotificationLevel? NotificationLevel { get; set; }

        /// <summary>
        /// Gets or sets the notification url.
        /// </summary>
        public string? Url { get; set; }
    }
}
