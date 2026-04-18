using System;
using System.Threading.Tasks;
using Jellyfin.Data.Events;
using Jellyfin.Data.Queries;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Model.Activity;

/// <summary>
/// Interface for the activity manager.
/// </summary>
public interface IActivityManager
{
    /// <summary>
    /// The event that is triggered when an entity is created.
    /// </summary>
    event EventHandler<GenericEventArgs<ActivityLogEntry>> EntryCreated;

    /// <summary>
    /// Create a new activity log entry.
    /// </summary>
    /// <param name="entry">The entry to create.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task CreateAsync(ActivityLog entry);

    /// <summary>
    /// Get a paged list of activity log entries.
    /// </summary>
    /// <param name="query">The activity log query.</param>
    /// <returns>The page of entries.</returns>
    Task<QueryResult<ActivityLogEntry>> GetPagedResultAsync(ActivityLogQuery query);

    /// <summary>
    /// Remove all activity logs before the specified date.
    /// </summary>
    /// <param name="startDate">Activity log start date.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task CleanAsync(DateTime startDate);
}
