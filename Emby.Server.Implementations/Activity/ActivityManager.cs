using System;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Activity
{
    /// <summary>
    /// The activity log manager.
    /// </summary>
    public class ActivityManager : IActivityManager
    {
        /// <inheritdoc />
        public event EventHandler<GenericEventArgs<ActivityLogEntry>> EntryCreated;

        private readonly IActivityRepository _repo;
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityManager"/> class.
        /// </summary>
        /// <param name="repo">The activity repository.</param>
        /// <param name="userManager">The user manager.</param>
        public ActivityManager(
            IActivityRepository repo,
            IUserManager userManager)
        {
            _repo = repo;
            _userManager = userManager;
        }

        /// <inheritdoc />
        public void Create(ActivityLogEntry entry)
        {
            entry.Date = DateTime.UtcNow;

            _repo.Create(entry);

            EntryCreated?.Invoke(this, new GenericEventArgs<ActivityLogEntry>(entry));
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public QueryResult<ActivityLogEntry> GetActivityLogEntries(DateTime? minDate, int? startIndex, int? limit)
        {
            return GetActivityLogEntries(minDate, null, startIndex, limit);
        }
    }
}
