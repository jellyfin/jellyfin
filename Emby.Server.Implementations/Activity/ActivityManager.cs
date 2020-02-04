#pragma warning disable CS1591
#pragma warning disable SA1600

using System;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Querying;
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

        public void Create(ActivityLogEntry entry)
        {
            entry.Date = DateTime.UtcNow;

            _repo.Create(entry);

            EntryCreated?.Invoke(this, new GenericEventArgs<ActivityLogEntry>(entry));
        }

        public QueryResult<ActivityLogEntry> GetActivityLogEntries(DateTime? minDate, bool? hasUserId, int? startIndex, int? limit)
        {
            var result = _repo.GetActivityLogEntries(minDate, hasUserId, startIndex, limit);

            foreach (var item in result.Items)
            {
                if (item.UserId == Guid.Empty)
                {
                    continue;
                }

                var user = _userManager.GetUserById(item.UserId);

                if (user != null)
                {
                    var dto = _userManager.GetUserDto(user);
                    item.UserPrimaryImageTag = dto.PrimaryImageTag;
                }
            }

            return result;
        }

        public QueryResult<ActivityLogEntry> GetActivityLogEntries(DateTime? minDate, int? startIndex, int? limit)
        {
            return GetActivityLogEntries(minDate, null, startIndex, limit);
        }
    }
}
