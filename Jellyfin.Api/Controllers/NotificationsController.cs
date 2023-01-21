using System;
using System.Collections.Generic;
using Jellyfin.Api.Constants;
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
    public class NotificationsController : BaseJellyfinApiController
    {
        /// <summary>
        /// Gets notification types.
        /// </summary>
        /// <response code="200">All notification types returned.</response>
        /// <returns>An <cref see="OkResult"/> containing a list of all notification types.</returns>
        [HttpGet("Types")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Obsolete("Notifications API is a no-op.")]
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
        [Obsolete("Notifications API is a no-op.")]
        public IEnumerable<NameIdPair> GetNotificationServices()
        {
            return Array.Empty<NameIdPair>();
        }
    }
}
