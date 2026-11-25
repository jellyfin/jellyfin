using System;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class ItemPersistencePeopleCleanupTests : SqliteDbTestFixture
{
    private readonly ItemPersistenceService _service;

    public ItemPersistencePeopleCleanupTests()
    {
        _service = new ItemPersistenceService(
            CreateDbContextFactory(),
            Mock.Of<IServerApplicationHost>(),
            NullLogger<ItemPersistenceService>.Instance);
    }

    [Fact]
    public void DeleteItem_RemovesUnusedPeopleForItemsDescendantsAndExtras()
    {
        var parent = CreateItem(isFolder: true);
        var child = CreateItem();
        child.ParentId = parent.Id;
        var extra = CreateItem();
        extra.OwnerId = child.Id;
        var survivor = CreateItem();
        var shared = CreatePerson("Shared person");
        var unrelatedOrphan = CreatePerson("Unrelated orphan");
        using (var context = CreateDbContext())
        {
            context.PeopleBaseItemMap.AddRange(
                Map(parent, CreatePerson("Parent credit")),
                Map(child, CreatePerson("Child credit")),
                Map(extra, CreatePerson("Extra credit")),
                Map(child, shared),
                Map(survivor, shared));
            context.Peoples.Add(unrelatedOrphan);
            context.AncestorIds.Add(new AncestorId
            {
                ItemId = child.Id,
                Item = child,
                ParentItemId = parent.Id,
                ParentItem = parent
            });
            context.SaveChanges();
        }

        _service.DeleteItem([parent.Id]);

        using var after = CreateDbContext();
        Assert.Equal(survivor.Id, Assert.Single(after.BaseItems.Where(e => !e.Id.Equals(BaseItemRepository.PlaceholderId))).Id);
        Assert.Equal(survivor.Id, Assert.Single(after.PeopleBaseItemMap).ItemId);
        Assert.Equal(new[] { shared.Id, unrelatedOrphan.Id }.Order(), after.Peoples.Select(e => e.Id).Order());
    }

    private static BaseItemEntity CreateItem(bool isFolder = false) => new()
    {
        Id = Guid.NewGuid(),
        Type = isFolder ? typeof(Folder).FullName! : typeof(Book).FullName!,
        IsFolder = isFolder
    };

    private static People CreatePerson(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        PersonType = "Actor"
    };

    private static PeopleBaseItemMap Map(BaseItemEntity item, People person) => new()
    {
        ItemId = item.Id,
        Item = item,
        PeopleId = person.Id,
        People = person,
        Role = string.Empty
    };
}
