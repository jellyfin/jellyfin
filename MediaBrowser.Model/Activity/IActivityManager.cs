#pragma warning disable CS1591

using System;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Events;
using Jellyfin.Data.Queries;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Model.Activity
{
    public interface IActivityManager
    {
        event EventHandler<GenericEventArgs<ActivityLogEntry>> EntryCreated;

        Task CreateAsync(ActivityLog entry);

        Task<QueryResult<ActivityLogEntry>> GetPagedResultAsync(ActivityLogQuery query);

        /// <summary>
        /// Remove all activity logs before the specified date.
        /// </summary>
        /// <param name="startDate">Activity log start date.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task CleanAsync(DateTime startDate);
    }
}
