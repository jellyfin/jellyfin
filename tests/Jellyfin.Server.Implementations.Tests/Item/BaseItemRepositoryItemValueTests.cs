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

    public BaseItemRepositoryItemValueTests()
    {
        var itemTypeLookup = new ItemTypeLookup();
        _audioTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Audio];
        _movieTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie];
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
        var genre = new ItemValue
        {
            ItemValueId = Guid.NewGuid(),
            Type = ItemValueType.Genre,
            Value = "Genre Leak",
            CleanValue = "genre leak"
        };
        var excludedGenre = new ItemValue
        {
            ItemValueId = Guid.NewGuid(),
            Type = ItemValueType.Genre,
            Value = "Excluded Genre",
            CleanValue = "excluded genre"
        };

        using (var context = CreateDbContext())
        {
            context.BaseItems.AddRange(firstItem, secondItem, excludedItem);
            context.ItemValues.AddRange(genre, excludedGenre);
            context.BaseItemTags.AddRange(
                CreateTag(firstItem, "Alpha", "alpha"),
                CreateTag(firstItem, "alpha", "alpha"),
                CreateTag(secondItem, "Beta", "beta"),
                CreateTag(excludedItem, "Excluded Tag", "excluded tag"));
            context.ItemValuesMap.AddRange(
                CreateMap(firstItem, genre),
                CreateMap(excludedItem, excludedGenre));
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
        var genre = CreateItemValue(ItemValueType.Genre, "Genre Leak", "genre leak");

        using (var context = CreateDbContext())
        {
            context.BaseItems.AddRange(movie, otherMovie, audio);
            context.ItemValues.Add(genre);
            context.BaseItemTags.AddRange(
                CreateTag(movie, "Alpha", "alpha"),
                CreateTag(movie, "alpha", "alpha"),
                CreateTag(otherMovie, "Beta", "beta"),
                CreateTag(audio, "Excluded Tag", "excluded tag"));
            context.ItemValuesMap.Add(CreateMap(movie, genre));
            context.SaveChanges();
        }

        var result = _repository.GetTagNames(new InternalItemsQuery(new Database.Implementations.Entities.User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Movie]
        });

        Assert.Equal(["Alpha", "Beta"], result);
    }

    [Fact]
    public void GetGenreNames_GroupsAndFiltersMappedItemValues()
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
        var movieGenre = CreateItemValue(ItemValueType.Genre, "Movie Genre", "movie genre");
        var duplicateMovieGenre = CreateItemValue(ItemValueType.Genre, "movie genre", "movie genre");
        var musicGenre = CreateItemValue(ItemValueType.Genre, "Music Genre", "music genre");
        var orphanedGenre = CreateItemValue(ItemValueType.Genre, "Orphaned Genre", "orphaned genre");

        using (var context = CreateDbContext())
        {
            context.BaseItems.AddRange(movie, audio);
            context.ItemValues.AddRange(movieGenre, duplicateMovieGenre, musicGenre, orphanedGenre);
            context.ItemValuesMap.AddRange(
                CreateMap(movie, movieGenre),
                CreateMap(movie, duplicateMovieGenre),
                CreateMap(audio, musicGenre));
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

    private static ItemValueMap CreateMap(BaseItemEntity item, ItemValue itemValue)
    {
        return new ItemValueMap
        {
            ItemId = item.Id,
            ItemValueId = itemValue.ItemValueId,
            Item = item,
            ItemValue = itemValue
        };
    }

    private static ItemValue CreateItemValue(ItemValueType type, string value, string cleanValue)
    {
        return new ItemValue
        {
            ItemValueId = Guid.NewGuid(),
            Type = type,
            Value = value,
            CleanValue = cleanValue
        };
    }
}
