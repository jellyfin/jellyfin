using System;
using System.Linq;
using Jellyfin.Api.Constants;
using Jellyfin.Data.Entities;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Activity log controller.
    /// </summary>
    [Route("System/ActivityLog")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public class ActivityLogController : BaseJellyfinApiController
    {
        private readonly IActivityManager _activityManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityLogController"/> class.
        /// </summary>
        /// <param name="activityManager">Instance of <see cref="IActivityManager"/> interface.</param>
        public ActivityLogController(IActivityManager activityManager)
        {
            _activityManager = activityManager;
        }

        /// <summary>
        /// Gets activity log entries.
        /// </summary>
        /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
        /// <param name="limit">Optional. The maximum number of records to return.</param>
        /// <param name="minDate">Optional. The minimum date. Format = ISO.</param>
        /// <param name="hasUserId">Optional. Filter log entries if it has user id, or not.</param>
        /// <response code="200">Activity log returned.</response>
        /// <returns>A <see cref="QueryResult{ActivityLogEntry}"/> containing the log entries.</returns>
        [HttpGet("Entries")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<QueryResult<ActivityLogEntry>> GetLogEntries(
            [FromQuery] int? startIndex,
            [FromQuery] int? limit,
            [FromQuery] DateTime? minDate,
            [FromQuery] bool? hasUserId)
        {
            var filterFunc = new Func<IQueryable<ActivityLog>, IQueryable<ActivityLog>>(
                entries => entries.Where(entry => entry.DateCreated >= minDate
                                                  && (!hasUserId.HasValue || (hasUserId.Value
                                                      ? entry.UserId != Guid.Empty
                                                      : entry.UserId == Guid.Empty))));

            return _activityManager.GetPagedResult(filterFunc, startIndex, limit);
        }
    }
}
