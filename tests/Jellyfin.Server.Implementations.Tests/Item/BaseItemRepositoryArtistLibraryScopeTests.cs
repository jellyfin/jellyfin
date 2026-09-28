using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Artists are by-name items: they live outside any library and carry no TopParentId, so a plain
/// item query for them is exempt from the library filter and spans every music library. Only the
/// by-name listings, which reach the artist through the tracks that credit it, can be scoped to
/// one library.
/// </summary>
public sealed class BaseItemRepositoryArtistLibraryScopeTests : SqliteDbTestFixture
{
    private static readonly Guid _firstLibrary = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid _secondLibrary = Guid.Parse("22222222-0000-0000-0000-000000000001");

    private readonly BaseItemRepository _repository;
    private readonly ItemTypeLookup _itemTypeLookup;

    public BaseItemRepositoryArtistLibraryScopeTests()
    {
        _itemTypeLookup = new ItemTypeLookup();
        _repository = CreateBaseItemRepository(_itemTypeLookup);

        Seed("First Artist", "first artist", _firstLibrary, hasImage: true);
        Seed("Second Artist", "second artist", _secondLibrary, hasImage: true);
    }

    [Fact]
    public void GetItemList_MusicArtistsScopedToOneLibrary_ReturnsEveryLibrarysArtists()
    {
        // The shape the library cover image used to be built from. By-name types are exempt from
        // the TopParentId filter, so the scope is silently dropped.
        var result = _repository.GetItemList(new InternalItemsQuery
        {
            DtoOptions = new DtoOptions(false),
            IncludeItemTypes = [BaseItemKind.MusicArtist],
            TopParentIds = [_firstLibrary]
        });

        Assert.Equal(["First Artist", "Second Artist"], result.Select(i => i.Name).OrderBy(n => n));
    }

    [Fact]
    public void GetAllArtists_ScopedToOneLibrary_ReturnsOnlyThatLibrarysArtists()
    {
        var result = _repository.GetAllArtists(new InternalItemsQuery
        {
            DtoOptions = new DtoOptions(false),
            TopParentIds = [_firstLibrary]
        });

        var (artist, _) = Assert.Single(result.Items);
        Assert.Equal("First Artist", artist.Name);
    }

    [Fact]
    public void GetAllArtists_ImageTypes_DropsArtistsWithoutThatImage()
    {
        // The collage has nothing to draw with an artist that has no image, so the listing has to
        // honour the image filter the caller asked for.
        Seed("Third Artist", "third artist", _firstLibrary, hasImage: false);

        var result = _repository.GetAllArtists(new InternalItemsQuery
        {
            DtoOptions = new DtoOptions(false),
            ImageTypes = [ImageType.Primary],
            TopParentIds = [_firstLibrary]
        });

        var (artist, _) = Assert.Single(result.Items);
        Assert.Equal("First Artist", artist.Name);
    }

    /// <summary>
    /// Seeds one by-name artist row and a track in the given library crediting it.
    /// </summary>
    /// <param name="name">The artist name.</param>
    /// <param name="cleanName">The cleaned artist name, which is what links the two rows.</param>
    /// <param name="topParentId">The library the track belongs to.</param>
    /// <param name="hasImage">Whether the artist row carries a primary image.</param>
    private void Seed(string name, string cleanName, Guid topParentId, bool hasImage)
    {
        using var ctx = CreateDbContext();

        var artistId = Guid.NewGuid();
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

        if (hasImage)
        {
            artist.Images =
            [
                new BaseItemImageInfo
                {
                    Id = Guid.NewGuid(),
                    ItemId = artistId,
                    Item = artist,
                    ImageType = ImageInfoImageType.Primary,
                    Path = $"/metadata/artists/{cleanName}/folder.jpg"
                }
            ];
        }

        ctx.BaseItems.Add(artist);

        var trackId = Guid.NewGuid();
        var track = new BaseItemEntity
        {
            Id = trackId,
            Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Audio],
            Name = $"{name} - Track",
            CleanName = $"{cleanName} - track",
            PresentationUniqueKey = trackId.ToString("N"),
            MediaType = "Audio",
            TopParentId = topParentId,
            IsFolder = false,
            IsVirtualItem = false
        };
        ctx.BaseItems.Add(track);

        var itemValue = new ItemValue
        {
            ItemValueId = Guid.NewGuid(),
            Type = ItemValueType.AlbumArtist,
            Value = name,
            CleanValue = cleanName
        };

        ctx.ItemValues.Add(itemValue);
        ctx.ItemValuesMap.Add(new ItemValueMap
        {
            ItemId = trackId,
            ItemValueId = itemValue.ItemValueId,
            Item = track,
            ItemValue = itemValue
        });

        ctx.SaveChanges();
    }
}
