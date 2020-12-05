using System;
using Jellyfin.Api.Models.SyncPlay.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The time sync controller.
    /// </summary>
    [Route("")]
    public class TimeSyncController : BaseJellyfinApiController
    {
        /// <summary>
        /// Gets the current UTC time.
        /// </summary>
        /// <response code="200">Time returned.</response>
        /// <returns>An <see cref="TimeSyncDto"/> to sync the client and server time.</returns>
        [HttpGet("GetUtcTime")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        public ActionResult<TimeSyncDto> GetUtcTime()
        {
            // Important to keep the following line at the beginning
            var requestReceptionTime = DateTime.UtcNow.ToUniversalTime();

            // Important to keep the following line at the end
            var responseTransmissionTime = DateTime.UtcNow.ToUniversalTime();

            // Implementing NTP on such a high level results in this useless
            // information being sent. On the other hand it enables future additions.
            return new TimeSyncDto(requestReceptionTime, responseTransmissionTime);
        }
    }
}
