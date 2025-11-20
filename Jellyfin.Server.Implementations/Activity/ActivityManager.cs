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
        var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var entries = dbContext.ActivityLogs.AsQueryable();

            if (query.HasUserId is not null)
            {
                entries = entries.Where(e => e.UserId.Equals(default) != query.HasUserId.Value);
            }

            if (query.MinDate is not null)
            {
                entries = entries.Where(e => e.DateCreated >= query.MinDate.Value);
            }

            if (!string.IsNullOrEmpty(query.Name))
            {
                entries = entries.Where(e => EF.Functions.Like(e.Name, $"%{query.Name}%"));
            }

            if (!string.IsNullOrEmpty(query.Overview))
            {
                entries = entries.Where(e => EF.Functions.Like(e.Overview, $"%{query.Overview}%"));
            }

            if (!string.IsNullOrEmpty(query.ShortOverview))
            {
                entries = entries.Where(e => EF.Functions.Like(e.ShortOverview, $"%{query.ShortOverview}%"));
            }

            if (!string.IsNullOrEmpty(query.Type))
            {
                entries = entries.Where(e => EF.Functions.Like(e.Type, $"%{query.Type}%"));
            }

            if (!string.IsNullOrEmpty(query.ItemId))
            {
                entries = entries.Where(e => e.ItemId == query.ItemId);
            }

            if (!query.UserId.IsNullOrEmpty())
            {
                entries = entries.Where(e => e.UserId.Equals(query.UserId.Value));
            }

            if (query.Severity is not null)
            {
                entries = entries.Where(e => e.LogSeverity == query.Severity);
            }

            return new QueryResult<ActivityLogEntry>(
                query.Skip,
                await entries.CountAsync().ConfigureAwait(false),
                await ApplyOrdering(entries, query.OrderBy)
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

    private IOrderedQueryable<ActivityLog> ApplyOrdering(IQueryable<ActivityLog> query, IReadOnlyCollection<(ActivityLogSortBy, SortOrder)>? sorting)
    {
        if (sorting is null || sorting.Count == 0)
        {
            return query.OrderByDescending(e => e.DateCreated);
        }

        IOrderedQueryable<ActivityLog> ordered = null!;

        foreach (var (sortBy, sortOrder) in sorting)
        {
            var orderBy = MapOrderBy(sortBy);
            ordered = sortOrder == SortOrder.Ascending
                ? (ordered ?? query).OrderBy(orderBy)
                : (ordered ?? query).OrderByDescending(orderBy);
        }

        return ordered;
    }

    private Expression<Func<ActivityLog, object?>> MapOrderBy(ActivityLogSortBy sortBy)
    {
        return sortBy switch
        {
            ActivityLogSortBy.Name => e => e.Name,
            ActivityLogSortBy.Overiew => e => e.Overview,
            ActivityLogSortBy.ShortOverview => e => e.ShortOverview,
            ActivityLogSortBy.Type => e => e.Type,
            ActivityLogSortBy.ItemId => e => e.ItemId,
            ActivityLogSortBy.DateCreated => e => e.DateCreated,
            ActivityLogSortBy.UserId => e => e.UserId,
            ActivityLogSortBy.LogSeverity => e => e.LogSeverity,
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Unhandled ActivityLogSortBy")
        };
    }
}
