using System;
using System.Collections.Generic;

namespace Jellyfin.Api.Models.NotificationDtos
{
    /// <summary>
    /// A list of notifications with the total record count for pagination.
    /// </summary>
    public class NotificationResultDto
    {
        /// <summary>
        /// Gets or sets the current page of notifications.
        /// </summary>
        public IReadOnlyList<NotificationDto> Notifications { get; set; } = Array.Empty<NotificationDto>();

        /// <summary>
        /// Gets or sets the total number of notifications.
        /// </summary>
        public int TotalRecordCount { get; set; }
    }
}
