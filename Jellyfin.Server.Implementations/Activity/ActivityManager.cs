using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Events;
using Jellyfin.Data.Queries;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Querying;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Activity;

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
        // TODO allow sorting and filtering by item id. Currently not possible because ActivityLog stores the item id as a string.

        var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            // TODO switch to LeftJoin in .NET 10.
            var entries = from a in dbContext.ActivityLogs
                join u in dbContext.Users on a.UserId equals u.Id into ugj
                from u in ugj.DefaultIfEmpty()
                select new ExpandedActivityLog { ActivityLog = a, Username = u.Username };

            if (query.HasUserId is not null)
            {
                entries = entries.Where(e => e.ActivityLog.UserId.Equals(default) != query.HasUserId.Value);
            }

            if (query.MinDate is not null)
            {
                entries = entries.Where(e => e.ActivityLog.DateCreated >= query.MinDate.Value);
            }

            if (!string.IsNullOrEmpty(query.Name))
            {
                entries = entries.Where(e => EF.Functions.Like(e.ActivityLog.Name, $"%{query.Name}%"));
            }

            if (!string.IsNullOrEmpty(query.Overview))
            {
                entries = entries.Where(e => EF.Functions.Like(e.ActivityLog.Overview, $"%{query.Overview}%"));
            }

            if (!string.IsNullOrEmpty(query.ShortOverview))
            {
                entries = entries.Where(e => EF.Functions.Like(e.ActivityLog.ShortOverview, $"%{query.ShortOverview}%"));
            }

            if (!string.IsNullOrEmpty(query.Type))
            {
                entries = entries.Where(e => EF.Functions.Like(e.ActivityLog.Type, $"%{query.Type}%"));
            }

            if (!query.ItemId.IsNullOrEmpty())
            {
                var itemId = query.ItemId.Value.ToString("N");
                entries = entries.Where(e => e.ActivityLog.ItemId == itemId);
            }

            if (!string.IsNullOrEmpty(query.Username))
            {
                entries = entries.Where(e => EF.Functions.Like(e.Username, $"%{query.Username}%"));
            }

            if (query.Severity is not null)
            {
                entries = entries.Where(e => e.ActivityLog.LogSeverity == query.Severity);
            }

            return new QueryResult<ActivityLogEntry>(
                query.Skip,
                await entries.CountAsync().ConfigureAwait(false),
                await ApplyOrdering(entries, query.OrderBy)
                    .Skip(query.Skip ?? 0)
                    .Take(query.Limit ?? 100)
                    .Select(entity => new ActivityLogEntry(entity.ActivityLog.Name, entity.ActivityLog.Type, entity.ActivityLog.UserId)
                    {
                        Id = entity.ActivityLog.Id,
                        Overview = entity.ActivityLog.Overview,
                        ShortOverview = entity.ActivityLog.ShortOverview,
                        ItemId = entity.ActivityLog.ItemId,
                        Date = entity.ActivityLog.DateCreated,
                        Severity = entity.ActivityLog.LogSeverity
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

    private IOrderedQueryable<ExpandedActivityLog> ApplyOrdering(IQueryable<ExpandedActivityLog> query, IReadOnlyCollection<(ActivityLogSortBy, SortOrder)>? sorting)
    {
        if (sorting is null || sorting.Count == 0)
        {
            return query.OrderByDescending(e => e.ActivityLog.DateCreated);
        }

        IOrderedQueryable<ExpandedActivityLog> ordered = null!;

        foreach (var (sortBy, sortOrder) in sorting)
        {
            var orderBy = MapOrderBy(sortBy);
            ordered = sortOrder == SortOrder.Ascending
                ? (ordered ?? query).OrderBy(orderBy)
                : (ordered ?? query).OrderByDescending(orderBy);
        }

        return ordered;
    }

    private Expression<Func<ExpandedActivityLog, object?>> MapOrderBy(ActivityLogSortBy sortBy)
    {
        return sortBy switch
        {
            ActivityLogSortBy.Name => e => e.ActivityLog.Name,
            ActivityLogSortBy.Overiew => e => e.ActivityLog.Overview,
            ActivityLogSortBy.ShortOverview => e => e.ActivityLog.ShortOverview,
            ActivityLogSortBy.Type => e => e.ActivityLog.Type,
            ActivityLogSortBy.DateCreated => e => e.ActivityLog.DateCreated,
            ActivityLogSortBy.Username => e => e.Username,
            ActivityLogSortBy.LogSeverity => e => e.ActivityLog.LogSeverity,
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Unhandled ActivityLogSortBy")
        };
    }

    private class ExpandedActivityLog
    {
        public ActivityLog ActivityLog { get; set; } = null!;

        public string? Username { get; set; }
    }
}
