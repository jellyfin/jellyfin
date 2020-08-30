using System;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Events;
using MediaBrowser.Controller.Events;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Querying;

namespace Jellyfin.Server.Implementations.Activity
{
    /// <summary>
    /// Manages the storage and retrieval of <see cref="ActivityLog"/> instances.
    /// </summary>
    public class ActivityManager : IActivityManager
    {
        private readonly JellyfinDbProvider _provider;
        private readonly IEventManager _eventManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityManager"/> class.
        /// </summary>
        /// <param name="provider">The Jellyfin database provider.</param>
        /// <param name="eventManager">Instance of the <see cref="IEventManager"/> interface.</param>
        public ActivityManager(JellyfinDbProvider provider, IEventManager eventManager)
        {
            _provider = provider;
            _eventManager = eventManager;
        }

        /// <inheritdoc/>
        public async Task CreateAsync(ActivityLog entry)
        {
            await using var dbContext = _provider.CreateContext();

            dbContext.ActivityLogs.Add(entry);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            await _eventManager.PublishAsync(new GenericEventArgs<ActivityLogEntry>(ConvertToOldModel(entry))).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public QueryResult<ActivityLogEntry> GetPagedResult(
            Func<IQueryable<ActivityLog>, IQueryable<ActivityLog>> func,
            int? startIndex,
            int? limit)
        {
            using var dbContext = _provider.CreateContext();

            var query = func(dbContext.ActivityLogs.OrderByDescending(entry => entry.DateCreated));

            if (startIndex.HasValue)
            {
                query = query.Skip(startIndex.Value);
            }

            if (limit.HasValue)
            {
                query = query.Take(limit.Value);
            }

            // This converts the objects from the new database model to the old for compatibility with the existing API.
            var list = query.Select(ConvertToOldModel).ToList();

            return new QueryResult<ActivityLogEntry>
            {
                Items = list,
                TotalRecordCount = func(dbContext.ActivityLogs).Count()
            };
        }

        /// <inheritdoc/>
        public QueryResult<ActivityLogEntry> GetPagedResult(int? startIndex, int? limit)
        {
            return GetPagedResult(logs => logs, startIndex, limit);
        }

        private static ActivityLogEntry ConvertToOldModel(ActivityLog entry)
        {
            return new ActivityLogEntry
            {
                Id = entry.Id,
                Name = entry.Name,
                Overview = entry.Overview,
                ShortOverview = entry.ShortOverview,
                Type = entry.Type,
                ItemId = entry.ItemId,
                UserId = entry.UserId,
                Date = entry.DateCreated,
                Severity = entry.LogSeverity
            };
        }
    }
}
