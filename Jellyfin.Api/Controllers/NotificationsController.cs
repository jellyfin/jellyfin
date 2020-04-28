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
        /// Gets a user's notifications.
        /// </summary>
        /// <param name="userId">The user's ID.</param>
        /// <param name="isRead">An optional filter by notification read state.</param>
        /// <param name="startIndex">The optional index to start at. All notifications with a lower index will be omitted from the results.</param>
        /// <param name="limit">An optional limit on the number of notifications returned.</param>
        /// <response code="200">Notifications returned.</response>
        /// <returns>An <see cref="OkResult"/> containing a list of notifications.</returns>
        [HttpGet("{UserID}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<NotificationResultDto> GetNotifications(
            [FromRoute] string userId,
            [FromQuery] bool? isRead,
            [FromQuery] int? startIndex,
            [FromQuery] int? limit)
        {
            return new NotificationResultDto();
        }

        /// <summary>
        /// Gets a user's notification summary.
        /// </summary>
        /// <param name="userId">The user's ID.</param>
        /// <response code="200">Summary of user's notifications returned.</response>
        /// <returns>An <cref see="OkResult"/> containing a summary of the users notifications.</returns>
        [HttpGet("{UserID}/Summary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<NotificationsSummaryDto> GetNotificationsSummary(
            [FromRoute] string userId)
        {
            return new NotificationsSummaryDto();
        }

        /// <summary>
        /// Gets notification types.
        /// </summary>
        /// <response code="200">All notification types returned.</response>
        /// <returns>An <cref see="OkResult"/> containing a list of all notification types.</returns>
        [HttpGet("Types")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<NotificationTypeInfo>> GetNotificationTypes()
        {
            return _notificationManager.GetNotificationTypes();
        }

        /// <summary>
        /// Gets notification services.
        /// </summary>
        /// <response>All notification services returned.</response>
        /// <returns>An <cref see="OkResult"/> containing a list of all notification services.</returns>
        [HttpGet("Services")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<NameIdPair>> GetNotificationServices()
        {
            return _notificationManager.GetNotificationServices().ToList();
        }

        /// <summary>
        /// Sends a notification to all admins.
        /// </summary>
        /// <param name="name">The name of the notification.</param>
        /// <param name="description">The description of the notification.</param>
        /// <param name="url">The URL of the notification.</param>
        /// <param name="level">The level of the notification.</param>
        /// <response code="200">Notification sent.</response>
        /// <returns>An <cref see="OkResult"/>.</returns>
        [HttpPost("Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult CreateAdminNotification(
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

            return Ok();
        }

        /// <summary>
        /// Sets notifications as read.
        /// </summary>
        /// <param name="userId">The userID.</param>
        /// <param name="ids">A comma-separated list of the IDs of notifications which should be set as read.</param>
        /// <response code="200">Notifications set as read.</response>
        /// <returns>An <cref see="OkResult"/>.</returns>
        [HttpPost("{UserID}/Read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult SetRead(
            [FromRoute] string userId,
            [FromQuery] string ids)
        {
            return Ok();
        }

        /// <summary>
        /// Sets notifications as unread.
        /// </summary>
        /// <param name="userId">The userID.</param>
        /// <param name="ids">A comma-separated list of the IDs of notifications which should be set as unread.</param>
        /// <response code="200">Notifications set as unread.</response>
        /// <returns>An <cref see="OkResult"/>.</returns>
        [HttpPost("{UserID}/Unread")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult SetUnread(
            [FromRoute] string userId,
            [FromQuery] string ids)
        {
            return Ok();
        }
    }
}
