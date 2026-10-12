using System;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// When several genres share a presentation key, the list returns the one with the smallest id. The ids
/// are picked so that their canonical order is the opposite of both their <see cref="Guid.ToByteArray()"/>
/// byte order and the uniqueidentifier order of SQL Server.
/// </summary>
public sealed class BaseItemRepositoryByNameRepresentativeTests : SqliteDbTestFixture
{
    private static readonly Guid _lowestId = Guid.Parse("0fffffff-0000-0000-0000-0000000000ff");
    private static readonly Guid _highestId = Guid.Parse("10000000-0000-0000-0000-000000000000");

    private readonly BaseItemRepository _repository;
    private readonly ItemTypeLookup _itemTypeLookup = new();

    public BaseItemRepositoryByNameRepresentativeTests()
    {
        _repository = CreateBaseItemRepository(_itemTypeLookup);
    }

    [Fact]
    public void GetGenres_DuplicatesSharingPresentationKey_ReturnsLowestId()
    {
        Seed();

        var result = _repository.GetGenres(CreateQuery());

        Assert.Equal(_lowestId, Assert.Single(result.Items).Item.Id);
    }

    private static InternalItemsQuery CreateQuery()
        => new(new User("test", "auth", "reset"));

    private void Seed()
    {
        var carrier = new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie],
            Name = "Carrier",
            IsFolder = false,
            IsVirtualItem = false
        };
        var itemValue = new ItemValue
        {
            ItemValueId = Guid.NewGuid(),
            Type = ItemValueType.Genre,
            Value = "Rock",
            CleanValue = "rock"
        };

        using var context = CreateDbContext();
        context.BaseItems.AddRange(carrier, CreateGenre(_highestId), CreateGenre(_lowestId));
        context.ItemValues.Add(itemValue);
        context.ItemValuesMap.Add(new ItemValueMap
        {
            ItemId = carrier.Id,
            ItemValueId = itemValue.ItemValueId,
            Item = carrier,
            ItemValue = itemValue
        });
        context.SaveChanges();
    }

    private BaseItemEntity CreateGenre(Guid id)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Genre],
            Name = "Rock",
            CleanName = "rock",
            PresentationUniqueKey = "rock",
            IsFolder = true,
            IsVirtualItem = false
        };
    }
}
