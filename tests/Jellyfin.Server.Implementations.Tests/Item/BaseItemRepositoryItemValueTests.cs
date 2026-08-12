using System;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class BaseItemRepositoryItemValueTests : SqliteDbTestFixture
{
    private readonly BaseItemRepository _repository;
    private readonly string _audioTypeName;
    private readonly string _movieTypeName;
    private readonly string _genreTypeName;
    private readonly string _musicGenreTypeName;

    public BaseItemRepositoryItemValueTests()
    {
        var itemTypeLookup = new ItemTypeLookup();
        _audioTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Audio];
        _movieTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie];
        _genreTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Genre];
        _musicGenreTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicGenre];
        _repository = CreateBaseItemRepository(itemTypeLookup);
    }

    [Fact]
    public void GetQueryFiltersLegacy_GroupsAndFiltersItemValues()
    {
        var firstItem = CreateMovieEntity(Guid.NewGuid(), "First");
        var secondItem = CreateMovieEntity(Guid.NewGuid(), "Second");
        var excludedItem = new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = _audioTypeName,
            Name = "Excluded Audio",
            MediaType = "Audio",
            IsMovie = false,
            IsFolder = false,
            IsVirtualItem = false
        };
        var genre = CreateByNameEntity(_genreTypeName, "Genre Leak");
        var excludedGenre = CreateByNameEntity(_genreTypeName, "Excluded Genre");

        using (var context = CreateDbContext())
        {
            context.BaseItems.AddRange(firstItem, secondItem, excludedItem, genre, excludedGenre);
            context.BaseItemTags.AddRange(
                CreateTag(firstItem, "Alpha", "alpha"),
                CreateTag(firstItem, "alpha", "alpha"),
                CreateTag(secondItem, "Beta", "beta"),
                CreateTag(excludedItem, "Excluded Tag", "excluded tag"));
            context.BaseItemGenres.AddRange(
                CreateGenreLink(firstItem, genre),
                CreateGenreLink(excludedItem, excludedGenre));
            context.SaveChanges();
        }

        var result = _repository.GetQueryFiltersLegacy(new InternalItemsQuery(new Database.Implementations.Entities.User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Movie]
        });

        Assert.Equal(["Alpha", "Beta"], result.Tags);
        Assert.Equal(["Genre Leak"], result.Genres);
    }

    [Fact]
    public void GetTagNames_GroupsAndFiltersItemValues()
    {
        var movie = CreateMovieEntity(Guid.NewGuid(), "Movie");
        var otherMovie = CreateMovieEntity(Guid.NewGuid(), "Other Movie");
        var audio = new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = _audioTypeName,
            Name = "Excluded Audio",
            MediaType = "Audio",
            IsMovie = false,
            IsFolder = false,
            IsVirtualItem = false
        };
        var genre = CreateByNameEntity(_genreTypeName, "Genre Leak");

        using (var context = CreateDbContext())
        {
            context.BaseItems.AddRange(movie, otherMovie, audio);
            context.BaseItems.Add(genre);
            context.BaseItemTags.AddRange(
                CreateTag(movie, "Alpha", "alpha"),
                CreateTag(movie, "alpha", "alpha"),
                CreateTag(otherMovie, "Beta", "beta"),
                CreateTag(audio, "Excluded Tag", "excluded tag"));
            context.BaseItemGenres.Add(CreateGenreLink(movie, genre));
            context.SaveChanges();
        }

        var result = _repository.GetTagNames(new InternalItemsQuery(new Database.Implementations.Entities.User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Movie]
        });

        Assert.Equal(["Alpha", "Beta"], result);
    }

    [Fact]
    public void GetGenreNames_ListsOnlyLinkedGenresOfTheirKind()
    {
        var movie = CreateMovieEntity(Guid.NewGuid(), "Movie");
        var audio = new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = _audioTypeName,
            Name = "Audio",
            MediaType = "Audio",
            IsFolder = false,
            IsVirtualItem = false
        };
        var movieGenre = CreateByNameEntity(_genreTypeName, "Movie Genre");
        var musicGenre = CreateByNameEntity(_musicGenreTypeName, "Music Genre");
        var orphanedGenre = CreateByNameEntity(_genreTypeName, "Orphaned Genre");

        using (var context = CreateDbContext())
        {
            context.BaseItems.AddRange(movie, audio, movieGenre, musicGenre, orphanedGenre);
            context.BaseItemGenres.AddRange(
                CreateGenreLink(movie, movieGenre),
                CreateGenreLink(audio, musicGenre));
            context.SaveChanges();
        }

        Assert.Equal(["Movie Genre"], _repository.GetGenreNames());
        Assert.Equal(["Music Genre"], _repository.GetMusicGenreNames());
    }

    private BaseItemEntity CreateMovieEntity(Guid id, string name)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = _movieTypeName,
            Name = name,
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false
        };
    }

    private static BaseItemTag CreateTag(BaseItemEntity item, string value, string cleanValue)
    {
        return new BaseItemTag
        {
            ItemId = item.Id,
            Item = item,
            Value = value,
            CleanValue = cleanValue
        };
    }

    private static BaseItemEntity CreateByNameEntity(string type, string name)
    {
        return new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = type,
            Name = name,
            CleanName = name.ToLowerInvariant(),
            IsFolder = true,
            IsVirtualItem = false
        };
    }

    private static BaseItemGenre CreateGenreLink(BaseItemEntity item, BaseItemEntity genre)
    {
        return new BaseItemGenre
        {
            ItemId = item.Id,
            Item = item,
            GenreItemId = genre.Id
        };
    }
}
