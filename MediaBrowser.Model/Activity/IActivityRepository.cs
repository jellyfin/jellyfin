using System;
using System.Threading.Tasks;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Model.Activity
{
    public interface IActivityRepository
    {
        Task Create(ActivityLogEntry entry);

        QueryResult<ActivityLogEntry> GetActivityLogEntries(DateTime? minDate, int? startIndex, int? limit);
    }
}
