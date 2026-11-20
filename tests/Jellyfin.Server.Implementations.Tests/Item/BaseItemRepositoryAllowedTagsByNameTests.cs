using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// A user restricted to a set of allowed tags still has to see the by-name items - genres, studios,
/// artists - of the media those tags let through. The by-name item carries none of the media's tags,
/// so the allowed-tags filter would otherwise hide every one of them.
/// </summary>
public sealed class BaseItemRepositoryAllowedTagsByNameTests : SqliteDbTestFixture
{
    private const string AllowedTag = "kids";

    private readonly BaseItemRepository _repository;
    private readonly ItemTypeLookup _itemTypeLookup;

    public BaseItemRepositoryAllowedTagsByNameTests()
    {
        _itemTypeLookup = new ItemTypeLookup();
        _repository = CreateBaseItemRepository(_itemTypeLookup);
        Seed();
    }

    [Fact]
    public void GetGenres_WithAllowedTags_ReturnsGenresOfAllowedItems()
    {
        var result = _repository.GetGenres(CreateQuery(AllowedTag));

        Assert.Equal(["Comedy"], Names(result));
    }

    [Fact]
    public void GetStudios_WithAllowedTags_ReturnsStudiosOfAllowedItems()
    {
        var result = _repository.GetStudios(CreateQuery(AllowedTag));

        Assert.Equal(["Pixar"], Names(result));
    }

    [Fact]
    public void GetAllArtists_WithAllowedTags_ReturnsArtistsOfAllowedItems()
    {
        var result = _repository.GetAllArtists(CreateQuery(AllowedTag));

        Assert.Equal(["Raffi"], Names(result));
    }

    [Fact]
    public void GetGenres_WithoutAllowedTags_ReturnsEveryGenre()
    {
        var result = _repository.GetGenres(CreateQuery());

        Assert.Equal(["Comedy", "Horror", "Jazz"], Names(result));
    }

    private static string[] Names(MediaBrowser.Model.Querying.QueryResult<(BaseItem Item, MediaBrowser.Model.Dto.ItemCounts? ItemCounts)> result)
        => result.Items.Select(i => i.Item.Name!).Order(StringComparer.Ordinal).ToArray();

    private static InternalItemsQuery CreateQuery(params string[] allowedTags)
    {
        var user = new User("restricted", "auth", "reset");
        if (allowedTags.Length > 0)
        {
            user.SetPreference(PreferenceKind.AllowedTags, allowedTags);
        }

        return new InternalItemsQuery(user);
    }

    private void Seed()
    {
        using var context = CreateDbContext();

        // Tagged media, plus the by-name items describing it.
        var movie = CreateItem(BaseItemKind.Movie, "Allowed Movie", mediaType: "Video");
        var song = CreateItem(BaseItemKind.Audio, "Allowed Song", mediaType: "Audio");
        var genre = CreateItem(BaseItemKind.Genre, "Comedy");
        var studio = CreateItem(BaseItemKind.Studio, "Pixar");
        var artist = CreateItem(BaseItemKind.MusicArtist, "Raffi");

        // Media the allow list keeps out, plus the by-name items only it is described by.
        var blockedMovie = CreateItem(BaseItemKind.Movie, "Untagged Movie", mediaType: "Video");
        var blockedGenre = CreateItem(BaseItemKind.Genre, "Horror");

        // A genre written on nothing but a by-name item: that item is no more visible than the
        // media behind it, so the genre must not leak into a restricted user's list.
        var artistOnlyGenre = CreateItem(BaseItemKind.Genre, "Jazz");
        var blockedArtist = CreateItem(BaseItemKind.MusicArtist, "Mingus");

        var raffi = new People { Id = Guid.NewGuid(), Name = "Raffi", CleanName = "raffi", PersonType = "Artist", ItemId = artist.Id };

        context.BaseItems.AddRange(movie, song, genre, studio, artist, blockedMovie, blockedGenre, artistOnlyGenre, blockedArtist);
        context.BaseItemTags.AddRange(
            new BaseItemTag { ItemId = movie.Id, Item = movie, Value = AllowedTag, CleanValue = AllowedTag },
            new BaseItemTag { ItemId = song.Id, Item = song, Value = AllowedTag, CleanValue = AllowedTag });
        context.Peoples.Add(raffi);
        context.PeopleBaseItemMap.Add(new PeopleBaseItemMap { ItemId = song.Id, PeopleId = raffi.Id, Item = song, People = raffi, Role = string.Empty });
        context.BaseItemGenres.AddRange(
            new BaseItemGenre { ItemId = movie.Id, Item = movie, GenreItemId = genre.Id },
            new BaseItemGenre { ItemId = blockedMovie.Id, Item = blockedMovie, GenreItemId = blockedGenre.Id },
            new BaseItemGenre { ItemId = blockedArtist.Id, Item = blockedArtist, GenreItemId = artistOnlyGenre.Id });
        context.BaseItemStudios.Add(new BaseItemStudio { ItemId = movie.Id, Item = movie, StudioItemId = studio.Id });
        context.SaveChanges();
    }

    private BaseItemEntity CreateItem(BaseItemKind kind, string name, string? mediaType = null)
    {
        var id = Guid.NewGuid();

        return new BaseItemEntity
        {
            Id = id,
            Type = _itemTypeLookup.BaseItemKindNames[kind],
            Name = name,
            CleanName = name.ToLowerInvariant(),
            PresentationUniqueKey = id.ToString("N"),
            MediaType = mediaType,
            IsMovie = kind == BaseItemKind.Movie,
            IsFolder = false,
            IsVirtualItem = false
        };
    }
}
