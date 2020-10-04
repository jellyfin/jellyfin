using System;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Querying;

namespace Jellyfin.Server.Implementations.Activity
{
    /// <summary>
    /// Manages the storage and retrieval of <see cref="ActivityLog"/> instances.
    /// </summary>
    public class ActivityManager : IActivityManager
    {
        /// <inheritdoc/>
        public event EventHandler<GenericEventArgs<ActivityLogEntry>> EntryCreated;

        private JellyfinDbProvider _provider;

        /// <summary>
        /// Creates a new instance of the <see cref="ActivityManager"/> class.
        /// </summary>
        /// <param name="provider">The Jellyfin database provider.</param>
        public ActivityManager(JellyfinDbProvider provider)
        {
            _provider = provider;
        }

        /// <inheritdoc/>
        public void Create(ActivityLog entry)
        {
            using var dbContext = _provider.GetConnection();
            dbContext.ActivityLogs.Add(entry);
            dbContext.SaveChanges();

            EntryCreated?.Invoke(this, new GenericEventArgs<ActivityLogEntry>(ConvertToOldModel(entry)));
        }

        /// <inheritdoc/>
        public async Task CreateAsync(ActivityLog entry)
        {
            using var dbContext = _provider.GetConnection();
            await dbContext.ActivityLogs.AddAsync(entry);
            await dbContext.SaveChangesAsync();

            EntryCreated?.Invoke(this, new GenericEventArgs<ActivityLogEntry>(ConvertToOldModel(entry)));
        }

        /// <inheritdoc/>
        public QueryResult<ActivityLogEntry> GetActivityLogEntries(DateTime? minDate, bool? hasUserId, int? startIndex, int? limit)
        {
            using var dbConnection = _provider.GetConnection();
            var result = dbConnection.ActivityLogs.AsQueryable();

            if (minDate.HasValue)
            {
                result = result.Where(entry => entry.DateCreated >= minDate.Value);
            }

            if (hasUserId.HasValue)
            {
                result = result.Where(entry => hasUserId.Value != Equals(entry.UserId, Guid.Empty));
            }

            if (startIndex.HasValue)
            {
                result = result.Where(entry => entry.Id >= startIndex.Value);
            }

            if (limit.HasValue && limit.Value > 0)
            {
                result = result.OrderByDescending(entry => entry.DateCreated).Take(limit.Value);
            }

            // This converts the objects from the new database model to the old for compatibility with the existing API.
            var list = result.Select(entry => ConvertToOldModel(entry)).ToList();

            return new QueryResult<ActivityLogEntry>()
            {
                Items = list
            };
        }

        /// <inheritdoc/>
        public QueryResult<ActivityLogEntry> GetActivityLogEntries(DateTime? minDate, int? startIndex, int? limit)
        {
            return GetActivityLogEntries(minDate, null, startIndex, limit);
        }

        private static ActivityLogEntry ConvertToOldModel(ActivityLog entry)
        {
            return new ActivityLogEntry
            {
                Id = entry.Id,
                Name = entry.Name,
                Overview = entry.Overview,
                ShortOverview = entry.ShortOverview,
                Type = entry.Type.ToString(),
                ItemId = entry.ItemId,
                UserId = entry.UserId,
                Date = entry.DateCreated,
                Severity = entry.LogSeverity
            };
        }
    }
}
