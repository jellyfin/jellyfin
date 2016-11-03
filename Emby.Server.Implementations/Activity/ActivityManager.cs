using MediaBrowser.Common.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Emby.Server.Implementations.Activity
{
    public class ActivityManager : IActivityManager
    {
        public event EventHandler<GenericEventArgs<ActivityLogEntry>> EntryCreated;
        
        private readonly IActivityRepository _repo;
        private readonly ILogger _logger;
        private readonly IUserManager _userManager;

        public ActivityManager(ILogger logger, IActivityRepository repo, IUserManager userManager)
        {
            _logger = logger;
            _repo = repo;
            _userManager = userManager;
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
            var result = _repo.GetActivityLogEntries(minDate, startIndex, limit);

            foreach (var item in result.Items.Where(i => !string.IsNullOrWhiteSpace(i.UserId)))
            {
                var user = _userManager.GetUserById(item.UserId);

                if (user != null)
                {
                    var dto = _userManager.GetUserDto(user);
                    item.UserPrimaryImageTag = dto.PrimaryImageTag;
                }
            }

            return result;
        }
    }
}
