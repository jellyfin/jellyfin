using System;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Events;
using Jellyfin.Data.Queries;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Querying;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Activity
{
    /// <summary>
    /// Manages the storage and retrieval of <see cref="ActivityLog"/> instances.
    /// </summary>
    public class ActivityManager : IActivityManager
    {
        private readonly JellyfinDbProvider _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityManager"/> class.
        /// </summary>
        /// <param name="provider">The Jellyfin database provider.</param>
        public ActivityManager(JellyfinDbProvider provider)
        {
            _provider = provider;
        }

        /// <inheritdoc/>
        public event EventHandler<GenericEventArgs<ActivityLogEntry>>? EntryCreated;

        /// <inheritdoc/>
        public async Task CreateAsync(ActivityLog entry)
        {
            await using var dbContext = _provider.CreateContext();

            dbContext.ActivityLogs.Add(entry);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            EntryCreated?.Invoke(this, new GenericEventArgs<ActivityLogEntry>(ConvertToOldModel(entry)));
        }

        /// <inheritdoc/>
        public async Task<QueryResult<ActivityLogEntry>> GetPagedResultAsync(ActivityLogQuery query)
        {
            await using var dbContext = _provider.CreateContext();

            IQueryable<ActivityLog> entries = dbContext.ActivityLogs
                .AsQueryable()
                .OrderByDescending(entry => entry.DateCreated);

            if (query.MinDate.HasValue)
            {
                entries = entries.Where(entry => entry.DateCreated >= query.MinDate);
            }

            if (query.HasUserId.HasValue)
            {
                entries = entries.Where(entry => (!entry.UserId.Equals(default)) == query.HasUserId.Value);
            }

            return new QueryResult<ActivityLogEntry>(
                query.Skip,
                await entries.CountAsync().ConfigureAwait(false),
                await entries
                    .Skip(query.Skip ?? 0)
                    .Take(query.Limit ?? 100)
                    .AsAsyncEnumerable()
                    .Select(ConvertToOldModel)
                    .ToListAsync()
                    .ConfigureAwait(false));
        }

        /// <inheritdoc />
        public async Task CleanAsync(DateTime startDate)
        {
            await using var dbContext = _provider.CreateContext();
            var entries = dbContext.ActivityLogs
                .AsQueryable()
                .Where(entry => entry.DateCreated <= startDate);

            dbContext.RemoveRange(entries);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        private static ActivityLogEntry ConvertToOldModel(ActivityLog entry)
        {
            return new ActivityLogEntry(entry.Name, entry.Type, entry.UserId)
            {
                Id = entry.Id,
                Overview = entry.Overview,
                ShortOverview = entry.ShortOverview,
                ItemId = entry.ItemId,
                Date = entry.DateCreated,
                Severity = entry.LogSeverity
            };
        }
    }
}
