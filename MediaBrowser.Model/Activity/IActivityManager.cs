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
    }
}
