#nullable enable
#pragma warning disable CA1801
#pragma warning disable SA1313

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Jellyfin.Api.Models.NotificationDtos;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The notification controller.
    /// </summary>
    public class NotificationsController : BaseJellyfinApiController
    {
        private readonly INotificationManager _notificationManager;
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationsController" /> class.
        /// </summary>
        /// <param name="notificationManager">The notification manager.</param>
        /// <param name="userManager">The user manager.</param>
        public NotificationsController(INotificationManager notificationManager, IUserManager userManager)
        {
            _notificationManager = notificationManager;
            _userManager = userManager;
        }

        /// <summary>
        /// Endpoint for getting a user's notifications.
        /// </summary>
        /// <param name="UserID">The UserID.</param>
        /// <param name="IsRead">An optional filter by IsRead.</param>
        /// <param name="StartIndex">The optional index to start at. All notifications with a lower index will be dropped from the results.</param>
        /// <param name="Limit">An optional limit on the number of notifications returned.</param>
        /// <returns>A read-only list of all of the user's notifications.</returns>
        [HttpGet("{UserID}")]
        public IReadOnlyList<NotificationDto> GetNotifications(
            [FromRoute] string UserID,
            [FromQuery] bool? IsRead,
            [FromQuery] int? StartIndex,
            [FromQuery] int? Limit)
        {
            return new List<NotificationDto>();
        }

        /// <summary>
        /// Endpoint for getting a user's notification summary.
        /// </summary>
        /// <param name="UserID">The UserID.</param>
        /// <returns>Notifications summary for the user.</returns>
        [HttpGet("{UserId}/Summary")]
        public NotificationsSummaryDto GetNotificationsSummary(
            [FromRoute] string UserID)
        {
            return new NotificationsSummaryDto();
        }

        /// <summary>
        /// Endpoint for getting notification types.
        /// </summary>
        /// <returns>All notification types.</returns>
        [HttpGet("Types")]
        public IEnumerable<NotificationTypeInfo> GetNotificationTypes()
        {
            return _notificationManager.GetNotificationTypes();
        }

        /// <summary>
        /// Endpoint for getting notification services.
        /// </summary>
        /// <returns>All notification services.</returns>
        [HttpGet("Services")]
        public IEnumerable<NameIdPair> GetNotificationServices()
        {
            return _notificationManager.GetNotificationServices();
        }

        /// <summary>
        /// Endpoint to send a notification to all admins.
        /// </summary>
        /// <param name="Name">The name of the notification.</param>
        /// <param name="Description">The description of the notification.</param>
        /// <param name="URL">The URL of the notification.</param>
        /// <param name="Level">The level of the notification.</param>
        [HttpPost("Admin")]
        public void CreateAdminNotification(
            [FromForm] string Name,
            [FromForm] string Description,
            [FromForm] string? URL,
            [FromForm] NotificationLevel Level)
        {
            var notification = new NotificationRequest
            {
                Name = Name,
                Description = Description,
                Url = URL,
                Level = Level,
                UserIds = _userManager.Users.Where(i => i.Policy.IsAdministrator).Select(i => i.Id).ToArray(),
                Date = DateTime.UtcNow,
            };

            _notificationManager.SendNotification(notification, CancellationToken.None);
        }

        /// <summary>
        /// Endpoint to set notifications as read.
        /// </summary>
        /// <param name="UserID">The UserID.</param>
        /// <param name="IDs">The IDs of notifications which should be set as read.</param>
        [HttpPost("{UserID}/Read")]
        public void SetRead(
            [FromRoute] string UserID,
            [FromForm] List<string> IDs)
        {
        }

        /// <summary>
        /// Endpoint to set notifications as unread.
        /// </summary>
        /// <param name="UserID">The UserID.</param>
        /// <param name="IDs">The IDs of notifications which should be set as unread.</param>
        [HttpPost("{UserID}/Unread")]
        public void SetUnread(
            [FromRoute] string UserID,
            [FromForm] List<string> IDs)
        {
        }
    }
}
