using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Model.Events;

namespace MediaBrowser.Model.Activity
{
    public interface IActivityManager
    {
        event EventHandler<GenericEventArgs<ActivityLogEntry>> EntryCreated;

        Task Create(ActivityLogEntry entry);

        IEnumerable<ActivityLogEntry> GetActivityLogEntries(DateTime? minDate, bool? hasUserId, int? x, int? y);
    }
}
