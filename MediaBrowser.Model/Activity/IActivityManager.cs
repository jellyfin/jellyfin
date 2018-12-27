using System;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Model.Activity
{
    public interface IActivityManager
    {
        event EventHandler<GenericEventArgs<ActivityLogEntry>> EntryCreated;

        void Create(ActivityLogEntry entry);

        QueryResult<ActivityLogEntry> GetActivityLogEntries(DateTime? minDate, int? startIndex, int? limit);

        QueryResult<ActivityLogEntry> GetActivityLogEntries(DateTime? minDate, bool? hasUserId, int? x, int? y);
    }
}
