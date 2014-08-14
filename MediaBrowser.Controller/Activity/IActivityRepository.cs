using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Querying;
using System;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Activity
{
    public interface IActivityRepository
    {
        Task Create(ActivityLogEntry entry);

        QueryResult<ActivityLogEntry> GetActivityLogEntries(DateTime? minDate, int? startIndex, int? limit);
    }
}
