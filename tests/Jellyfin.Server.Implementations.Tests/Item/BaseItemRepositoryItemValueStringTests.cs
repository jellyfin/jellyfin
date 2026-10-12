using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Item values that differ only in case or accents share a clean value and collapse into the one whose
/// text is smallest by Unicode code point, case-sensitively: upper case before '_' before lower case
/// before accented letters.
/// </summary>
public sealed class BaseItemRepositoryItemValueStringTests : JellyfinDbTestFixture
{
    private static readonly string[] _expected = ["Rock", "_zed", "alpha", "Élan"];

    private readonly BaseItemRepository _repository;
    private readonly ItemTypeLookup _itemTypeLookup = new();

    public BaseItemRepositoryItemValueStringTests()
    {
        _repository = CreateBaseItemRepository(_itemTypeLookup);
    }

    [Fact]
    public void GetTagNames_ValuesSharingCleanValue_ReturnsSmallestCodePointSorted()
    {
        Seed(ItemValueType.Tags);

        var result = _repository.GetTagNames(CreateQuery());

        Assert.Equal(_expected, result);
    }

    [Fact]
    public void GetGenreNames_ValuesSharingCleanValue_ReturnsSmallestCodePoint()
    {
        Seed(ItemValueType.Genre);

        var result = _repository.GetGenreNames();

        Assert.Equal(_expected, result.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void GetQueryFiltersLegacy_ValuesSharingCleanValue_ReturnsSmallestCodePointSorted()
    {
        Seed(ItemValueType.Tags);
        Seed(ItemValueType.Genre);

        var result = _repository.GetQueryFiltersLegacy(CreateQuery());

        Assert.Equal(_expected, result.Tags);
        Assert.Equal(_expected, result.Genres);
    }

    private static InternalItemsQuery CreateQuery()
        => new(new User("test", "auth", "reset")) { IncludeItemTypes = [BaseItemKind.Movie] };

    private void Seed(ItemValueType type)
    {
        var movie = new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie],
            Name = Guid.NewGuid().ToString("N"),
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false
        };
        var values = new[]
        {
            CreateItemValue(type, "élan", "elan"),
            CreateItemValue(type, "Élan", "elan"),
            CreateItemValue(type, "rock", "rock"),
            CreateItemValue(type, "Rock", "rock"),
            CreateItemValue(type, "zed", "zed"),
            CreateItemValue(type, "_zed", "zed"),
            CreateItemValue(type, "alpha", "alpha")
        };

        using var context = CreateDbContext();
        context.BaseItems.Add(movie);
        context.ItemValues.AddRange(values);
        context.ItemValuesMap.AddRange(values.Select(v => new ItemValueMap
        {
            ItemId = movie.Id,
            ItemValueId = v.ItemValueId,
            Item = movie,
            ItemValue = v
        }));
        context.SaveChanges();
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
