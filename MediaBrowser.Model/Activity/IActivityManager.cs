#pragma warning disable CS1591

using System;
using System.Threading.Tasks;
using Jellyfin.Data.Events;
using Jellyfin.Data.Queries;
using MediaBrowser.Model.Dtos;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Model.Activity
{
    public interface IActivityManager
    {
        event EventHandler<GenericEventArgs<ActivityLogDto>> EntryCreated;

        Task CreateAsync(ActivityLogDto entry);

        Task<QueryResult<ActivityLogDto>> GetPagedResultAsync(ActivityLogQuery query);

        /// <summary>
        /// Remove all activity logs before the specified date.
        /// </summary>
        /// <param name="startDate">Activity log start date.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task CleanAsync(DateTime startDate);
    }
}
