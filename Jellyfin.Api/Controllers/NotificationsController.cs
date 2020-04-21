#nullable enable
#pragma warning disable CA1801

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Jellyfin.Api.Models.NotificationDtos;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Notifications;
using Microsoft.AspNetCore.Http;
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
        /// <param name="userId">The user's ID.</param>
        /// <param name="isRead">An optional filter by notification read state.</param>
        /// <param name="startIndex">The optional index to start at. All notifications with a lower index will be dropped from the results.</param>
        /// <param name="limit">An optional limit on the number of notifications returned.</param>
        /// <returns>A read-only list of all of the user's notifications.</returns>
        [HttpGet("{UserID}")]
        [ProducesResponseType(typeof(NotificationResultDto), StatusCodes.Status200OK)]
        public NotificationResultDto GetNotifications(
            [FromRoute] string userId,
            [FromQuery] bool? isRead,
            [FromQuery] int? startIndex,
            [FromQuery] int? limit)
        {
            return new NotificationResultDto();
        }

        /// <summary>
        /// Endpoint for getting a user's notification summary.
        /// </summary>
        /// <param name="userId">The user's ID.</param>
        /// <returns>Notifications summary for the user.</returns>
        [HttpGet("{UserID}/Summary")]
        [ProducesResponseType(typeof(NotificationsSummaryDto), StatusCodes.Status200OK)]
        public NotificationsSummaryDto GetNotificationsSummary(
            [FromRoute] string userId)
        {
            return new NotificationsSummaryDto();
        }

        /// <summary>
        /// Endpoint for getting notification types.
        /// </summary>
        /// <returns>All notification types.</returns>
        [HttpGet("Types")]
        [ProducesResponseType(typeof(IEnumerable<NotificationTypeInfo>), StatusCodes.Status200OK)]
        public IEnumerable<NotificationTypeInfo> GetNotificationTypes()
        {
            return _notificationManager.GetNotificationTypes();
        }

        /// <summary>
        /// Endpoint for getting notification services.
        /// </summary>
        /// <returns>All notification services.</returns>
        [HttpGet("Services")]
        [ProducesResponseType(typeof(IEnumerable<NameIdPair>), StatusCodes.Status200OK)]
        public IEnumerable<NameIdPair> GetNotificationServices()
        {
            return _notificationManager.GetNotificationServices();
        }

        /// <summary>
        /// Endpoint to send a notification to all admins.
        /// </summary>
        /// <param name="name">The name of the notification.</param>
        /// <param name="description">The description of the notification.</param>
        /// <param name="url">The URL of the notification.</param>
        /// <param name="level">The level of the notification.</param>
        [HttpPost("Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public void CreateAdminNotification(
            [FromQuery] string name,
            [FromQuery] string description,
            [FromQuery] string? url,
            [FromQuery] NotificationLevel? level)
        {
            var notification = new NotificationRequest
            {
                Name = name,
                Description = description,
                Url = url,
                Level = level ?? NotificationLevel.Normal,
                UserIds = _userManager.Users.Where(i => i.Policy.IsAdministrator).Select(i => i.Id).ToArray(),
                Date = DateTime.UtcNow,
            };

            _notificationManager.SendNotification(notification, CancellationToken.None);
        }

        /// <summary>
        /// Endpoint to set notifications as read.
        /// </summary>
        /// <param name="userId">The userID.</param>
        /// <param name="ids">A comma-separated list of the IDs of notifications which should be set as read.</param>
        [HttpPost("{UserID}/Read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public void SetRead(
            [FromRoute] string userId,
            [FromQuery] string ids)
        {
        }

        /// <summary>
        /// Endpoint to set notifications as unread.
        /// </summary>
        /// <param name="userId">The userID.</param>
        /// <param name="ids">A comma-separated list of the IDs of notifications which should be set as unread.</param>
        [HttpPost("{UserID}/Unread")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public void SetUnread(
            [FromRoute] string userId,
            [FromQuery] string ids)
        {
        }
    }
}
