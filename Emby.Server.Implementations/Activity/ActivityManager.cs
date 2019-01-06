using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Events;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Activity
{
    public class ActivityManager : IActivityManager
    {
        public event EventHandler<GenericEventArgs<ActivityLogEntry>> EntryCreated;

        private readonly IActivityRepository _repo;
        private readonly ILogger _logger;
        private readonly IUserManager _userManager;

        public ActivityManager(
            ILoggerFactory loggerFactory,
            IActivityRepository repo,
            IUserManager userManager)
        {
            _logger = loggerFactory.CreateLogger(nameof(ActivityManager));
            _repo = repo;
            _userManager = userManager;
        }

        public async Task Create(ActivityLogEntry entry)
        {
            entry.Date = DateTime.UtcNow;

            await _repo.CreateAsync(entry);

            EntryCreated?.Invoke(this, new GenericEventArgs<ActivityLogEntry>(entry));
        }

        public IEnumerable<ActivityLogEntry> GetActivityLogEntries(DateTime? minDate, bool? hasUserId, int? startIndex, int? limit)
        {
            var result = _repo.GetActivityLogEntries();

            if (minDate.HasValue)
            {
                result = result.Where(x => x.Date >= minDate.Value);
            }
            if (hasUserId.HasValue)
            {
                result = result.Where(x => x.UserId != null && x.UserId != Guid.Empty);
            }
            if (startIndex.HasValue)
            {
                result = result.Where(x => x.Id >= startIndex.Value);
            }
            if (limit.HasValue)
            {
                result = result.Take(limit.Value);
            }

            // Add images for each user
            foreach (var item in result)
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
