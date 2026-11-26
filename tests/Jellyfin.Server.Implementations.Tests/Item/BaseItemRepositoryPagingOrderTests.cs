using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;
using ItemSortBy = Jellyfin.Data.Enums.ItemSortBy;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers paging over items that tie on every requested sort key. Without a unique final key the
/// database may order tied rows differently on each page, so a client paging through them can see
/// one item twice and another not at all.
/// </summary>
public sealed class BaseItemRepositoryPagingOrderTests : SqliteDbTestFixture
{
    private const string MusicAlbumType = "MediaBrowser.Controller.Entities.Audio.MusicAlbum";

    // Ids that sort the same whether compared as text, as bytes or as a .NET Guid.
    private static readonly Guid[] _albumIds = Enumerable.Range(1, 5)
        .Select(i => Guid.Parse($"a0000000-0000-0000-0000-{i:D12}"))
        .ToArray();

    private readonly BaseItemRepository _repository;

    public BaseItemRepositoryPagingOrderTests()
    {
        using (var context = CreateDbContext())
        {
            // Saved one at a time in reverse, so storage order disagrees with id order
            // (one SaveChanges would insert them sorted by key).
            foreach (var id in _albumIds.Reverse())
            {
                context.BaseItems.Add(new BaseItemEntity
                {
                    Id = id,
                    Type = MusicAlbumType,
                    Name = "Cello Suites",
                    SortName = "cello suites",
                    ProductionYear = 2000,
                    IsFolder = true
                });
                context.SaveChanges();
            }
        }

        _repository = CreateBaseItemRepository(new ItemTypeLookup());
    }

    public static TheoryData<ItemSortBy[], SortOrder> Orderings => new()
    {
        { [], SortOrder.Ascending },
        { [ItemSortBy.SortName], SortOrder.Ascending },
        { [ItemSortBy.SortName], SortOrder.Descending },
        { [ItemSortBy.ProductionYear], SortOrder.Descending },
        { [ItemSortBy.Name, ItemSortBy.ProductionYear], SortOrder.Ascending },
    };

    [Theory]
    [MemberData(nameof(Orderings))]
    public void TiedItems_AreOrderedById(ItemSortBy[] sortBy, SortOrder sortOrder)
    {
        Assert.Equal(_albumIds, AlbumIds(sortBy, sortOrder, null, null));
    }

    [Theory]
    [MemberData(nameof(Orderings))]
    public void TiedItems_PageWithoutDuplicatesOrGaps(ItemSortBy[] sortBy, SortOrder sortOrder)
    {
        var paged = new List<Guid>();
        for (var start = 0; start < _albumIds.Length; start += 2)
        {
            paged.AddRange(AlbumIds(sortBy, sortOrder, start, 2));
        }

        Assert.Equal(_albumIds, paged);
    }

    [Theory]
    [MemberData(nameof(Orderings))]
    public void ApplyOrder_EndsWithId(ItemSortBy[] sortBy, SortOrder sortOrder)
    {
        using var context = CreateDbContext();
        var filter = new InternalItemsQuery { OrderBy = sortBy.Select(s => (s, sortOrder)).ToArray() };

        var sql = _repository.ApplyOrder(context.BaseItems, filter, context).ToQueryString();

        var orderBy = sql[(sql.LastIndexOf("ORDER BY", StringComparison.Ordinal) + "ORDER BY".Length)..];
        Assert.EndsWith(".\"Id\"", orderBy.Split(',')[^1].Trim(), StringComparison.Ordinal);
    }

    private List<Guid> AlbumIds(ItemSortBy[] sortBy, SortOrder sortOrder, int? startIndex, int? limit)
        => _repository
            .GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.MusicAlbum],
                OrderBy = sortBy.Select(s => (s, sortOrder)).ToArray(),
                StartIndex = startIndex,
                Limit = limit
            })
            .Select(i => i.Id)
            .ToList();
}
