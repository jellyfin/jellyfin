#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Persistence;
using Microsoft.EntityFrameworkCore;
using DbLinkedChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType;
using LinkedChildType = MediaBrowser.Controller.Entities.LinkedChildType;

namespace Jellyfin.Server.Implementations.Item;

/// <summary>
/// Provides linked children query and manipulation operations.
/// </summary>
public class LinkedChildrenService : ILinkedChildrenService
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IItemTypeLookup _itemTypeLookup;
    private readonly IItemQueryHelpers _queryHelpers;

    /// <summary>
    /// Initializes a new instance of the <see cref="LinkedChildrenService"/> class.
    /// </summary>
    /// <param name="dbProvider">The database context factory.</param>
    /// <param name="itemTypeLookup">The item type lookup.</param>
    /// <param name="queryHelpers">The shared query helpers.</param>
    public LinkedChildrenService(
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IItemTypeLookup itemTypeLookup,
        IItemQueryHelpers queryHelpers)
    {
        _dbProvider = dbProvider;
        _itemTypeLookup = itemTypeLookup;
        _queryHelpers = queryHelpers;
    }

    /// <inheritdoc/>
    public IReadOnlyList<Guid> GetLinkedChildrenIds(Guid parentId, int? childType = null)
    {
        using var dbContext = _dbProvider.CreateDbContext();

        var query = dbContext.LinkedChildren
            .Where(lc => lc.ParentId.Equals(parentId));

        if (childType.HasValue)
        {
            query = query.Where(lc => (int)lc.ChildType == childType.Value);
        }

        return query
            .OrderBy(lc => lc.SortOrder)
            .Select(lc => lc.ChildId)
            .ToArray();
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, MusicArtist[]> FindArtists(IReadOnlyList<string> artistNames)
    {
        using var dbContext = _dbProvider.CreateDbContext();

        var artists = dbContext.BaseItems
            .AsNoTracking()
            .Where(e => e.Type == _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist]!)
            .Where(e => artistNames.Contains(e.Name))
            .ToArray();

        var lookup = artists
            .GroupBy(e => e.Name!)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => _queryHelpers.DeserializeBaseItem(f)).Where(dto => dto is not null).Cast<MusicArtist>().ToArray());

        var result = new Dictionary<string, MusicArtist[]>(artistNames.Count);
        foreach (var name in artistNames)
        {
            if (lookup.TryGetValue(name, out var artistArray))
            {
                result[name] = artistArray;
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public IReadOnlyList<Guid> GetManualLinkedParentIds(Guid childId)
    {
        using var context = _dbProvider.CreateDbContext();
        return context.LinkedChildren
            .Where(lc => lc.ChildId == childId && lc.ChildType == DbLinkedChildType.Manual)
            .Select(lc => lc.ParentId)
            .Distinct()
            .ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<Guid> RerouteLinkedChildren(Guid fromChildId, Guid toChildId)
    {
        using var context = _dbProvider.CreateDbContext();

        var affectedParentIds = context.LinkedChildren
            .Where(lc => lc.ChildId == fromChildId && lc.ChildType == DbLinkedChildType.Manual)
            .Select(lc => lc.ParentId)
            .Distinct()
            .ToList();

        if (affectedParentIds.Count == 0)
        {
            return affectedParentIds;
        }

        var parentsWithTarget = context.LinkedChildren
            .Where(lc => lc.ChildId == toChildId && lc.ChildType == DbLinkedChildType.Manual)
            .Select(lc => lc.ParentId)
            .ToHashSet();

        context.LinkedChildren
            .Where(lc => lc.ChildId == fromChildId
                && lc.ChildType == DbLinkedChildType.Manual
                && !parentsWithTarget.Contains(lc.ParentId))
            .ExecuteUpdate(s => s.SetProperty(e => e.ChildId, toChildId));

        context.LinkedChildren
            .Where(lc => lc.ChildId == fromChildId
                && lc.ChildType == DbLinkedChildType.Manual
                && parentsWithTarget.Contains(lc.ParentId))
            .ExecuteDelete();

        return affectedParentIds;
    }

    /// <inheritdoc/>
    public void UpsertLinkedChild(Guid parentId, Guid childId, LinkedChildType childType)
    {
        using var context = _dbProvider.CreateDbContext();

        var dbChildType = (DbLinkedChildType)childType;
        var existingLink = context.LinkedChildren
            .FirstOrDefault(lc => lc.ParentId == parentId && lc.ChildId == childId);

        if (existingLink is null)
        {
            context.LinkedChildren.Add(new Jellyfin.Database.Implementations.Entities.LinkedChildEntity
            {
                ParentId = parentId,
                ChildId = childId,
                ChildType = dbChildType,
                SortOrder = null
            });
        }
        else
        {
            existingLink.ChildType = dbChildType;
        }

        context.SaveChanges();
    }
}
