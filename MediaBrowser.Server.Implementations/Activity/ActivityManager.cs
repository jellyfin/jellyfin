using MediaBrowser.Common.Events;
using MediaBrowser.Controller.Activity;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using System;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Activity
{
    public class ActivityManager : IActivityManager
    {
        public event EventHandler<GenericEventArgs<ActivityLogEntry>> EntryCreated;
        
        private readonly IActivityRepository _repo;
        private readonly ILogger _logger;

        public ActivityManager(ILogger logger, IActivityRepository repo)
        {
            _logger = logger;
            _repo = repo;
        }

        public async Task Create(ActivityLogEntry entry)
        {
            entry.Id = Guid.NewGuid().ToString("N");
            entry.Date = DateTime.UtcNow;

            await _repo.Create(entry).ConfigureAwait(false);

            EventHelper.FireEventIfNotNull(EntryCreated, this, new GenericEventArgs<ActivityLogEntry>(entry), _logger);
        }

        public QueryResult<ActivityLogEntry> GetActivityLogEntries(DateTime? minDate, int? startIndex, int? limit)
        {
            return _repo.GetActivityLogEntries(minDate, startIndex, limit);
        }
    }
}
