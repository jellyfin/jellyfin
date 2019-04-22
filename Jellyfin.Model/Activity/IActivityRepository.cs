using System;
using Jellyfin.Model.Querying;

namespace Jellyfin.Model.Activity
{
    public interface IActivityRepository
    {
        void Create(ActivityLogEntry entry);

        QueryResult<ActivityLogEntry> GetActivityLogEntries(DateTime? minDate, bool? z, int? startIndex, int? limit);
    }
}
