using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;
using ItemSortBy = Jellyfin.Data.Enums.ItemSortBy;
using SortOrder = Jellyfin.Database.Implementations.Enums.SortOrder;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Strings sort by Unicode code point, case-sensitively: upper case before '_' before lower case before
/// accented letters. The name range filters compare the lower-cased sort name instead, so they ignore case
/// but still place '_' before letters and 'é' after 'z'.
/// </summary>
public sealed class BaseItemRepositoryStringOrderTests : JellyfinDbTestFixture
{
    private static readonly string[] _values = ["Bob", "_zed", "alice", "Ángel"];

    private readonly BaseItemRepository _repository;
    private readonly ItemTypeLookup _itemTypeLookup = new();

    public BaseItemRepositoryStringOrderTests()
    {
        _repository = CreateBaseItemRepository(_itemTypeLookup);
    }

    [Theory]
    [InlineData(ItemSortBy.Album)]
    [InlineData(ItemSortBy.SeriesSortName)]
    [InlineData(ItemSortBy.SortName)]
    public void GetItemList_StringSortKey_OrdersByCodePoint(ItemSortBy sortBy)
    {
        AddMovies(_values);

        KnownGap.On(KnownGaps.GuidAggregates, () =>
        {
            Assert.Equal(["Bob", "_zed", "alice", "Ángel"], Names(OrderedBy(sortBy, SortOrder.Ascending)));
            Assert.Equal(["Ángel", "alice", "_zed", "Bob"], Names(OrderedBy(sortBy, SortOrder.Descending)));
        });
    }

    [Theory]
    [InlineData("a", null, new[] { "alice", "Bob", "élan", "Zed" })]
    [InlineData("é", null, new[] { "élan" })]
    [InlineData(null, "é", new[] { "alice", "Bob", "Zed", "_zed" })]
    public void GetItemList_NameRange_IgnoresCaseAndOrdersByCodePoint(string? nameStartsWithOrGreater, string? nameLessThan, string[] expected)
    {
        AddMovies("alice", "Bob", "_zed", "Zed", "élan");

        KnownGap.On(KnownGaps.GuidAggregates, () =>
        {
            var result = _repository.GetItemList(new InternalItemsQuery(new User("test", "auth", "reset"))
            {
                IncludeItemTypes = [BaseItemKind.Movie],
                NameStartsWithOrGreater = nameStartsWithOrGreater,
                NameLessThan = nameLessThan
            });

            Assert.Equal(expected.Order(StringComparer.Ordinal), SortedNames(result));
        });
    }

    private static string[] SortedNames(IReadOnlyList<BaseItem> items)
        => items.Select(i => i.Name).Order(StringComparer.Ordinal).ToArray();

    private static string[] Names(IReadOnlyList<BaseItem> items)
        => items.Select(i => i.Name).ToArray();

    private IReadOnlyList<BaseItem> OrderedBy(ItemSortBy sortBy, SortOrder sortOrder)
    {
        return _repository.GetItemList(new InternalItemsQuery(new User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            OrderBy = [(sortBy, sortOrder)]
        });
    }

    private void AddMovies(params string[] names)
    {
        using var context = CreateDbContext();
        foreach (var name in names)
        {
            var id = Guid.NewGuid();
            context.BaseItems.Add(new BaseItemEntity
            {
                Id = id,
                PresentationUniqueKey = id.ToString("N"),
                Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie],
                Name = name,
                SortName = name,
                Album = name,
                SeriesName = name,
                MediaType = "Video",
                IsMovie = true,
                IsFolder = false,
                IsVirtualItem = false
            });
        }

        context.SaveChanges();
    }
}
