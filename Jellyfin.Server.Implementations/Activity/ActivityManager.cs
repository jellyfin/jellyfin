#pragma warning disable CS1591

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Jellyfin.Data;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Querying;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Activity
{
    public class ActivityManager : IActivityManager
    {
        public event EventHandler<GenericEventArgs<ActivityLogEntry>> EntryCreated;

        private JellyfinDbProvider _provider;

        public ActivityManager(JellyfinDbProvider provider)
        {
            _provider = provider;
        }


        public async Task CreateAsync(ActivityLog entry)
        {
            using var dbContext = _provider.GetConnection();
            await dbContext.ActivityLogs.AddAsync(entry);
            await dbContext.SaveChangesAsync();

            EntryCreated?.Invoke(this, new GenericEventArgs<ActivityLogEntry>(ConvertToOldModel(entry)));
        }

        public QueryResult<ActivityLogEntry> GetActivityLogEntries(DateTime? minDate, bool? hasUserId, int? startIndex, int? limit)
        {
            using var dbConnection = _provider.GetConnection();

            var elements = dbConnection.ActivityLogs.ToImmutableList();
            

            var result = dbConnection.ActivityLogs.AsQueryable();

            if (minDate.HasValue)
            {
                result = result.Where(entry => entry.DateCreated >= minDate.Value);
            }

            if (hasUserId.HasValue)
            {
                result = result.Where(entry => !Equals(entry.UserId, Guid.Empty));
            }

            if (startIndex.HasValue)
            {
                result = result.Where(entry => entry.Id >= startIndex.Value);
            }

            // Old code had a check limit > 0, still needed?
            if (limit.HasValue)
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
