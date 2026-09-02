using System;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class BaseItemRepositoryLegacyFilterTests : SqliteDbTestFixture
{
    private readonly BaseItemRepository _repository;
    private readonly string _audioTypeName;
    private readonly string _movieTypeName;

    public BaseItemRepositoryLegacyFilterTests()
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
        var firstTag = new ItemValue
        {
            ItemValueId = Guid.NewGuid(),
            Type = ItemValueType.Tags,
            Value = "Alpha",
            CleanValue = "alpha"
        };
        var duplicateTag = new ItemValue
        {
            ItemValueId = Guid.NewGuid(),
            Type = ItemValueType.Tags,
            Value = "alpha",
            CleanValue = "alpha"
        };
        var secondTag = new ItemValue
        {
            ItemValueId = Guid.NewGuid(),
            Type = ItemValueType.Tags,
            Value = "Beta",
            CleanValue = "beta"
        };
        var genre = new ItemValue
        {
            ItemValueId = Guid.NewGuid(),
            Type = ItemValueType.Genre,
            Value = "Genre Leak",
            CleanValue = "genre leak"
        };
        var excludedTag = new ItemValue
        {
            ItemValueId = Guid.NewGuid(),
            Type = ItemValueType.Tags,
            Value = "Excluded Tag",
            CleanValue = "excluded tag"
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
            context.ItemValues.AddRange(firstTag, duplicateTag, secondTag, genre, excludedTag, excludedGenre);
            context.ItemValuesMap.AddRange(
                CreateMap(firstItem, firstTag),
                CreateMap(firstItem, duplicateTag),
                CreateMap(secondItem, secondTag),
                CreateMap(firstItem, genre),
                CreateMap(excludedItem, excludedTag),
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
}
