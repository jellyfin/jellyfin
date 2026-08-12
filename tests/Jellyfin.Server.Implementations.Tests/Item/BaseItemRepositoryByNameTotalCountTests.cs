using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// The by-name endpoints (artists, album artists, genres, studios) all funnel through
/// <c>GetItemValues</c>. A query without a <c>Limit</c> used to have its total record count
/// silently disabled, so callers got a populated <c>Items</c> array next to a zero total.
/// </summary>
public sealed class BaseItemRepositoryByNameTotalCountTests : SqliteDbTestFixture
{
    private readonly BaseItemRepository _repository;
    private readonly ItemTypeLookup _itemTypeLookup;

    public BaseItemRepositoryByNameTotalCountTests()
    {
        _itemTypeLookup = new ItemTypeLookup();

        _repository = CreateBaseItemRepository(_itemTypeLookup);
    }

    [Fact]
    public void GetArtists_WithoutLimit_ReportsTotalRecordCount()
    {
        SeedArtists(3);

        var result = _repository.GetArtists(CreateQuery(limit: null));

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(3, result.TotalRecordCount);
    }

    [Fact]
    public void GetArtists_WithLimit_ReportsTotalBeyondThePage()
    {
        SeedArtists(3);

        var result = _repository.GetArtists(CreateQuery(limit: 2));

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(3, result.TotalRecordCount);
    }

    [Fact]
    public void GetArtists_TotalRecordCountDisabled_StaysZero()
    {
        SeedArtists(3);

        var query = CreateQuery(limit: null);
        query.EnableTotalRecordCount = false;

        var result = _repository.GetArtists(query);

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(0, result.TotalRecordCount);
    }

    [Fact]
    public void GetArtists_WithoutLimit_DoesNotMutateCallerQuery()
    {
        SeedArtists(1);

        var query = CreateQuery(limit: null);
        Assert.True(query.EnableTotalRecordCount);

        _repository.GetArtists(query);

        // The repository used to flip this flag on the caller's own query object, so a
        // reused query silently lost its total on every subsequent call.
        Assert.True(query.EnableTotalRecordCount);
    }

    private static InternalItemsQuery CreateQuery(int? limit)
    {
        return new InternalItemsQuery(new User("test", "auth", "reset"))
        {
            Limit = limit
        };
    }

    /// <summary>
    /// Creates <paramref name="count"/> artists, each credited on one song, which is what
    /// makes them visible to the item-value join behind the by-name endpoints.
    /// </summary>
    private void SeedArtists(int count)
    {
        using var ctx = CreateDbContext();

        for (var i = 0; i < count; i++)
        {
            var name = $"Artist {i}";
            var cleanName = name.ToLowerInvariant();

            var artistId = Guid.Parse($"aaaaaaaa-0000-0000-0000-{i:D12}");
            var songId = Guid.Parse($"55555555-0000-0000-0000-{i:D12}");
            var valueId = Guid.Parse($"cccccccc-0000-0000-0000-{i:D12}");

            var artist = new BaseItemEntity
            {
                Id = artistId,
                Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist],
                Name = name,
                CleanName = cleanName,
                PresentationUniqueKey = artistId.ToString("N"),
                IsFolder = true,
                IsVirtualItem = false
            };

            var song = new BaseItemEntity
            {
                Id = songId,
                Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Audio],
                Name = $"Song {i}",
                CleanName = $"song {i}",
                PresentationUniqueKey = songId.ToString("N"),
                MediaType = "Audio",
                IsFolder = false,
                IsVirtualItem = false
            };

            var itemValue = new ItemValue
            {
                ItemValueId = valueId,
                Type = ItemValueType.Artist,
                Value = name,
                CleanValue = cleanName
            };

            ctx.BaseItems.Add(artist);
            ctx.BaseItems.Add(song);
            ctx.ItemValues.Add(itemValue);
            ctx.ItemValuesMap.Add(new ItemValueMap
            {
                ItemId = songId,
                ItemValueId = valueId,
                Item = song,
                ItemValue = itemValue
            });
        }

        ctx.SaveChanges();
    }
}
