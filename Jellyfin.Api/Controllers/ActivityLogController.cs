using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Queries;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Common.Api;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers;

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
    /// <param name="startIndex">The record index to start at. All items with a lower index will be dropped from the results.</param>
    /// <param name="limit">The maximum number of records to return.</param>
    /// <param name="minDate">The minimum date.</param>
    /// <param name="hasUserId">Filter log entries if it has user id, or not.</param>
    /// <param name="name">Filter by name.</param>
    /// <param name="overview">Filter by overview.</param>
    /// <param name="shortOverview">Filter by short overview.</param>
    /// <param name="type">Filter by type.</param>
    /// <param name="itemId">Filter by item id.</param>
    /// <param name="username">Filter by username.</param>
    /// <param name="severity">Filter by log severity.</param>
    /// <param name="sortBy">Specify one or more sort orders. Format: SortBy=Name,Type.</param>
    /// <param name="sortOrder">Sort Order..</param>
    /// <response code="200">Activity log returned.</response>
    /// <returns>A <see cref="QueryResult{ActivityLogEntry}"/> containing the log entries.</returns>
    [HttpGet("Entries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<QueryResult<ActivityLogEntry>>> GetLogEntries(
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery] DateTime? minDate,
        [FromQuery] bool? hasUserId,
        [FromQuery] string? name,
        [FromQuery] string? overview,
        [FromQuery] string? shortOverview,
        [FromQuery] string? type,
        [FromQuery] Guid? itemId,
        [FromQuery] string? username,
        [FromQuery] LogLevel? severity,
        [FromQuery] ActivityLogSortBy[]? sortBy,
        [FromQuery] SortOrder[]? sortOrder)
    {
        var query = new ActivityLogQuery
        {
            Skip = startIndex,
            Limit = limit,
            MinDate = minDate,
            HasUserId = hasUserId,
            Name = name,
            Overview = overview,
            ShortOverview = shortOverview,
            Type = type,
            ItemId = itemId,
            Username = username,
            Severity = severity,
            OrderBy = GetOrderBy(sortBy ?? [], sortOrder ?? []),
        };

        return await _activityManager.GetPagedResultAsync(query).ConfigureAwait(false);
    }

    private static (ActivityLogSortBy SortBy, SortOrder SortOrder)[] GetOrderBy(
        IReadOnlyList<ActivityLogSortBy> sortBy,
        IReadOnlyList<SortOrder> requestedSortOrder)
    {
        if (sortBy.Count == 0)
        {
            return [];
        }

        var result = new (ActivityLogSortBy, SortOrder)[sortBy.Count];
        var i = 0;
        for (; i < requestedSortOrder.Count; i++)
        {
            result[i] = (sortBy[i], requestedSortOrder[i]);
        }

        // Add remaining elements with the first specified SortOrder
        // or the default one if no SortOrders are specified
        var order = requestedSortOrder.Count > 0 ? requestedSortOrder[0] : SortOrder.Ascending;
        for (; i < sortBy.Count; i++)
        {
            result[i] = (sortBy[i], order);
        }

        return result;
    }
}
