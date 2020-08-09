using System;
using MediaBrowser.Model.Notifications;

namespace Jellyfin.Api.Models.NotificationDtos
{
    /// <summary>
    /// The notification DTO.
    /// </summary>
    public class NotificationDto
    {
        /// <summary>
        /// Gets or sets the notification ID. Defaults to an empty string.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the notification's user ID. Defaults to an empty string.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the notification date.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the notification has been read. Defaults to false.
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// Gets or sets the notification's name. Defaults to an empty string.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the notification's description. Defaults to an empty string.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the notification's URL. Defaults to an empty string.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the notification level.
        /// </summary>
        public NotificationLevel Level { get; set; }
    }
}
