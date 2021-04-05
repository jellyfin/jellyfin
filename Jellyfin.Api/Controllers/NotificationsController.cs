using System;
using System.Collections.Generic;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Models.NotificationDtos;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The notification controller.
    /// </summary>
    [Authorize(Policy = Policies.DefaultAuthorization)]
    [Obsolete("Notifications are deprecated. Plugins should use the event system instead.")]
    public class NotificationsController : BaseJellyfinApiController
    {
        /// <summary>
        /// Gets a user's notifications.
        /// </summary>
        /// <response code="200">Notifications returned.</response>
        /// <returns>An <see cref="OkResult"/> containing a list of notifications.</returns>
        [HttpGet("{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<NotificationResultDto> GetNotifications()
        {
            return new NotificationResultDto();
        }

        /// <summary>
        /// Gets a user's notification summary.
        /// </summary>
        /// <response code="200">Summary of user's notifications returned.</response>
        /// <returns>An <cref see="OkResult"/> containing a summary of the users notifications.</returns>
        [HttpGet("{userId}/Summary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<NotificationsSummaryDto> GetNotificationsSummary()
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
        public IEnumerable<NotificationTypeInfo> GetNotificationTypes()
        {
            return Array.Empty<NotificationTypeInfo>();
        }

        /// <summary>
        /// Gets notification services.
        /// </summary>
        /// <response code="200">All notification services returned.</response>
        /// <returns>An <cref see="OkResult"/> containing a list of all notification services.</returns>
        [HttpGet("Services")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IEnumerable<NameIdPair> GetNotificationServices()
        {
            return Array.Empty<NameIdPair>();
        }

        /// <summary>
        /// Sends a notification to all admins.
        /// </summary>
        /// <response code="204">Notification sent.</response>
        /// <returns>A <cref see="NoContentResult"/>.</returns>
        [HttpPost("Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult CreateAdminNotification()
        {
            return NoContent();
        }

        /// <summary>
        /// Sets notifications as read.
        /// </summary>
        /// <response code="204">Notifications set as read.</response>
        /// <returns>A <cref see="NoContentResult"/>.</returns>
        [HttpPost("{userId}/Read")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult SetRead()
        {
            return NoContent();
        }

        /// <summary>
        /// Sets notifications as unread.
        /// </summary>
        /// <response code="204">Notifications set as unread.</response>
        /// <returns>A <cref see="NoContentResult"/>.</returns>
        [HttpPost("{userId}/Unread")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult SetUnread()
        {
            return NoContent();
        }
    }
}
