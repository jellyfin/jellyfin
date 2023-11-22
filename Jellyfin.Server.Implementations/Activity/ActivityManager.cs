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
        private readonly IDbContextFactory<JellyfinDbContext> _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityManager"/> class.
        /// </summary>
        /// <param name="provider">The Jellyfin database provider.</param>
        public ActivityManager(IDbContextFactory<JellyfinDbContext> provider)
        {
            _provider = provider;
        }

        /// <inheritdoc/>
        public event EventHandler<GenericEventArgs<ActivityLogEntry>>? EntryCreated;

        /// <inheritdoc/>
        public async Task CreateAsync(ActivityLog entry)
        {
            var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                dbContext.ActivityLogs.Add(entry);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            EntryCreated?.Invoke(this, new GenericEventArgs<ActivityLogEntry>(ConvertToOldModel(entry)));
        }

        /// <inheritdoc/>
        public async Task<QueryResult<ActivityLogEntry>> GetPagedResultAsync(ActivityLogQuery query)
        {
            var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                var entries = dbContext.ActivityLogs
                    .OrderByDescending(entry => entry.DateCreated)
                    .Where(entry => query.MinDate == null || entry.DateCreated >= query.MinDate)
                    .Where(entry => !query.HasUserId.HasValue || entry.UserId.Equals(default) != query.HasUserId.Value);

                return new QueryResult<ActivityLogEntry>(
                    query.Skip,
                    await entries.CountAsync().ConfigureAwait(false),
                    await entries
                        .Skip(query.Skip ?? 0)
                        .Take(query.Limit ?? 100)
                        .Select(entity => new ActivityLogEntry(entity.Name, entity.Type, entity.UserId)
                        {
                            Id = entity.Id,
                            Overview = entity.Overview,
                            ShortOverview = entity.ShortOverview,
                            ItemId = entity.ItemId,
                            Date = entity.DateCreated,
                            Severity = entity.LogSeverity
                        })
                        .ToListAsync()
                        .ConfigureAwait(false));
            }
        }

        /// <inheritdoc />
        public async Task CleanAsync(DateTime startDate)
        {
            var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                await dbContext.ActivityLogs
                    .Where(entry => entry.DateCreated <= startDate)
                    .ExecuteDeleteAsync()
                    .ConfigureAwait(false);
            }
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
