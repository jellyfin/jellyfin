using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;
using ItemSortBy = Jellyfin.Data.Enums.ItemSortBy;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Album and track artist sorts should follow the linked <see cref="BaseItemEntity.SortName"/> on
/// <see cref="BaseItemKind.MusicArtist"/> items, not the raw <see cref="ItemValue.CleanValue"/>.
/// </summary>
public sealed class BaseItemRepositoryAlbumArtistOrderingTests : SqliteDbTestFixture
{
    private readonly BaseItemRepository _repository;
    private readonly ItemTypeLookup _itemTypeLookup;
    private readonly User _user = new("test", "auth-provider", "reset-provider");

    private readonly Guid _beatlesAlbumId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private readonly Guid _blondieAlbumId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private readonly Guid _theCureTrackId = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    private readonly Guid _abbaTrackId = Guid.Parse("dddddddd-0000-0000-0000-000000000001");

    public BaseItemRepositoryAlbumArtistOrderingTests()
    {
        _itemTypeLookup = new ItemTypeLookup();

        using var context = CreateDbContext();
        Seed(context);

        _repository = CreateBaseItemRepository(_itemTypeLookup);
    }

    [Fact]
    public void AlbumArtist_UsesMusicArtistSortNameNotCleanValue()
    {
        var ids = _repository
            .GetItemList(new InternalItemsQuery(_user)
            {
                IncludeItemTypes = [BaseItemKind.MusicAlbum],
                OrderBy = [(ItemSortBy.AlbumArtist, SortOrder.Ascending)],
            })
            .Select(i => i.Id)
            .ToList();

        Assert.Equal([_beatlesAlbumId, _blondieAlbumId], ids);
    }

    [Fact]
    public void Artist_UsesMusicArtistSortNameNotCleanValue()
    {
        var ids = _repository
            .GetItemList(new InternalItemsQuery(_user)
            {
                IncludeItemTypes = [BaseItemKind.Audio],
                OrderBy = [(ItemSortBy.Artist, SortOrder.Ascending)],
            })
            .Select(i => i.Id)
            .ToList();

        Assert.Equal([_abbaTrackId, _theCureTrackId], ids);
    }

    private void Seed(Jellyfin.Database.Implementations.JellyfinDbContext context)
    {
        context.Users.Add(_user);

        AddMusicArtist(context, "The Beatles", "the beatles", "beatles");
        AddMusicArtist(context, "Blondie", "blondie", "blondie");
        AddMusicArtist(context, "The Cure", "the cure", "cure");
        AddMusicArtist(context, "ABBA", "abba", "abba");

        AddAlbum(context, _beatlesAlbumId, "Abbey Road", "The Beatles", "the beatles");
        AddAlbum(context, _blondieAlbumId, "Parallel Lines", "Blondie", "blondie");

        AddAudio(context, _theCureTrackId, "Lovesong", "The Cure", "the cure");
        AddAudio(context, _abbaTrackId, "Dancing Queen", "ABBA", "abba");

        context.SaveChanges();
    }

    private void AddMusicArtist(
        Jellyfin.Database.Implementations.JellyfinDbContext context,
        string name,
        string cleanName,
        string sortName)
    {
        var id = Guid.NewGuid();
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist],
            Name = name,
            CleanName = cleanName,
            SortName = sortName,
            PresentationUniqueKey = id.ToString("N"),
            IsFolder = true,
            IsVirtualItem = false,
        });
    }

    private void AddAlbum(
        Jellyfin.Database.Implementations.JellyfinDbContext context,
        Guid albumId,
        string name,
        string albumArtist,
        string cleanAlbumArtist)
    {
        var album = new BaseItemEntity
        {
            Id = albumId,
            Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicAlbum],
            Name = name,
            CleanName = name.ToLowerInvariant(),
            SortName = name.ToLowerInvariant(),
            PresentationUniqueKey = albumId.ToString("N"),
            IsFolder = true,
            IsVirtualItem = false,
        };
        context.BaseItems.Add(album);
        AddItemValueMap(context, album, ItemValueType.AlbumArtist, albumArtist, cleanAlbumArtist);
    }

    private void AddAudio(
        Jellyfin.Database.Implementations.JellyfinDbContext context,
        Guid trackId,
        string name,
        string artist,
        string cleanArtist)
    {
        var track = new BaseItemEntity
        {
            Id = trackId,
            Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Audio],
            Name = name,
            CleanName = name.ToLowerInvariant(),
            SortName = name.ToLowerInvariant(),
            PresentationUniqueKey = trackId.ToString("N"),
            IsFolder = false,
            IsVirtualItem = false,
        };
        context.BaseItems.Add(track);
        AddItemValueMap(context, track, ItemValueType.Artist, artist, cleanArtist);
    }

    private static void AddItemValueMap(
        Jellyfin.Database.Implementations.JellyfinDbContext context,
        BaseItemEntity item,
        ItemValueType type,
        string value,
        string cleanValue)
    {
        var itemValue = new ItemValue
        {
            ItemValueId = Guid.NewGuid(),
            Type = type,
            Value = value,
            CleanValue = cleanValue,
        };
        context.ItemValues.Add(itemValue);
        context.ItemValuesMap.Add(new ItemValueMap
        {
            ItemId = item.Id,
            ItemValueId = itemValue.ItemValueId,
            Item = item,
            ItemValue = itemValue,
        });
    }
}
