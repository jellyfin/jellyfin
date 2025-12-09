using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Data.Queries;

/// <summary>
/// A class representing a query to the activity logs.
/// </summary>
public class ActivityLogQuery : PaginatedQuery
{
    /// <summary>
    /// Gets or sets a value indicating whether to take entries with a user id.
    /// </summary>
    public bool? HasUserId { get; set; }

    /// <summary>
    /// Gets or sets the minimum date to query for.
    /// </summary>
    public DateTime? MinDate { get; set; }

    /// <summary>
    /// Gets or sets the name filter.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the overview filter.
    /// </summary>
    public string? Overview { get; set; }

    /// <summary>
    /// Gets or sets the short overview filter.
    /// </summary>
    public string? ShortOverview { get; set; }

    /// <summary>
    /// Gets or sets the type filter.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the item filter.
    /// </summary>
    public Guid? ItemId { get; set; }

    /// <summary>
    /// Gets or sets the username filter.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the log level filter.
    /// </summary>
    public LogLevel? Severity { get; set; }

    /// <summary>
    /// Gets or sets the result ordering.
    /// </summary>
    public IReadOnlyCollection<(ActivityLogSortBy, SortOrder)>? OrderBy { get; set; }
}
