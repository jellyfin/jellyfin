#pragma warning disable CS1591
#pragma warning disable SA1600

using System;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Model.Activity
{
    public interface IActivityRepository
    {
        void Create(ActivityLogEntry entry);

        QueryResult<ActivityLogEntry> GetActivityLogEntries(DateTime? minDate, bool? z, int? startIndex, int? limit);
    }
}
