using System;
using System.Linq;
using System.Threading.Tasks;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class ItemValuesCleanupTests : SqliteDbTestFixture
{
    private readonly ItemPersistenceService _service;

    public ItemValuesCleanupTests()
    {
        _service = new ItemPersistenceService(
            CreateDbContextFactory(),
            Mock.Of<IServerApplicationHost>(),
            NullLogger<ItemPersistenceService>.Instance);
    }

    [Fact]
    public void DeleteItem_LastReference_RemovesNewAndExistingOrphans()
    {
        var deleted = CreateItem();
        var survivor = CreateItem();
        var referenced = CreateValue("Referenced");
        using (var context = CreateDbContext())
        {
            context.ItemValuesMap.AddRange(
                Map(deleted, CreateValue("Last reference")),
                Map(survivor, referenced));
            context.ItemValues.Add(CreateValue("Already orphaned"));
            context.SaveChanges();
        }

        _service.DeleteItem([deleted.Id]);

        using var after = CreateDbContext();
        Assert.False(after.BaseItems.Any(e => e.Id.Equals(deleted.Id)));
        Assert.Equal(referenced.ItemValueId, Assert.Single(after.ItemValues).ItemValueId);
        Assert.Equal(survivor.Id, Assert.Single(after.ItemValuesMap).ItemId);
    }

    [Fact]
    public void DeleteItem_BatchWithDescendantAndOwnedExtra_CleansAllRemovedReferences()
    {
        var parent = CreateItem(isFolder: true);
        var child = CreateItem();
        child.ParentId = parent.Id;
        var extra = CreateItem();
        extra.OwnerId = child.Id;
        var otherDeleted = CreateItem();
        var survivor = CreateItem();
        var batchShared = CreateValue("Shared inside deletion batch");
        var survivingShared = CreateValue("Shared with surviving item");
        using (var context = CreateDbContext())
        {
            context.ItemValuesMap.AddRange(
                Map(parent, CreateValue("Parent value")),
                Map(child, batchShared),
                Map(otherDeleted, batchShared),
                Map(extra, CreateValue("Extra value")),
                Map(child, survivingShared),
                Map(survivor, survivingShared));
            context.AncestorIds.Add(new AncestorId
            {
                ItemId = child.Id,
                Item = child,
                ParentItemId = parent.Id,
                ParentItem = parent
            });
            context.SaveChanges();
        }

        _service.DeleteItem([parent.Id, otherDeleted.Id]);

        using var after = CreateDbContext();
        Assert.Equal(survivingShared.ItemValueId, Assert.Single(after.ItemValues).ItemValueId);
        Assert.Equal(survivor.Id, Assert.Single(after.ItemValuesMap).ItemId);
        Assert.Equal(survivor.Id, Assert.Single(after.BaseItems.Where(e => !e.Id.Equals(BaseItemRepository.PlaceholderId))).Id);
        Assert.Empty(after.AncestorIds);
    }

    [Fact]
    public void DeleteItem_ChildWithoutAncestorRows_CleansValueOrphanedByCascade()
    {
        var parent = CreateItem(isFolder: true);
        var child = CreateItem();
        child.ParentId = parent.Id;
        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(parent);
            context.ItemValuesMap.Add(Map(child, CreateValue("Cascaded child value")));
            context.SaveChanges();
        }

        _service.DeleteItem([parent.Id]);

        using var after = CreateDbContext();
        Assert.False(after.BaseItems.Any(e => e.Id.Equals(child.Id)));
        Assert.Empty(after.ItemValuesMap);
        Assert.Empty(after.ItemValues);
    }

    [Fact]
    public void DeleteItem_CleanupFails_RollsBackItemAndMapDeletion()
    {
        var item = CreateItem();
        var value = CreateValue("Last reference");
        using (var context = CreateDbContext())
        {
            context.ItemValuesMap.Add(Map(item, value));
            context.ItemValues.Add(CreateValue("Already orphaned"));
            context.SaveChanges();
            context.Database.ExecuteSqlRaw("""
                CREATE TRIGGER FailItemValuesCleanup BEFORE DELETE ON ItemValues
                BEGIN
                    SELECT RAISE(ABORT, 'injected orphan cleanup failure');
                END;
                """);
        }

        var exception = Assert.Throws<SqliteException>(() => _service.DeleteItem([item.Id]));

        Assert.Contains("injected orphan cleanup failure", exception.Message, StringComparison.Ordinal);
        using var after = CreateDbContext();
        Assert.True(after.BaseItems.Any(e => e.Id.Equals(item.Id)));
        Assert.Equal(value.ItemValueId, Assert.Single(after.ItemValuesMap).ItemValueId);
        Assert.Equal(2, after.ItemValues.Count());
    }

    [Fact]
    public async Task PostScanRun_NoDeadItems_RemovesUnrelatedOrphansAndPreservesReferences()
    {
        var item = CreateItem();
        var referenced = CreateValue("Referenced");
        using (var context = CreateDbContext())
        {
            context.ItemValuesMap.Add(Map(item, referenced));
            context.ItemValues.Add(CreateValue("Unrelated orphan"));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var library = new Mock<ILibraryManager>(MockBehavior.Strict);
        library.Setup(e => e.GetItemIds(It.Is<InternalItemsQuery>(query => query.HasDeadParentId == true)))
            .Returns(Array.Empty<Guid>());
        library.Setup(e => e.GetItemList(It.IsAny<InternalItemsQuery>())).Returns(Array.Empty<BaseItem>());
        var task = new CleanDatabaseScheduledTask(
            library.Object,
            NullLogger<CleanDatabaseScheduledTask>.Instance,
            CreateDbContextFactory(),
            Mock.Of<IPathManager>(MockBehavior.Strict));

        await task.Run(Mock.Of<IProgress<double>>(), TestContext.Current.CancellationToken);

        using var after = CreateDbContext();
        Assert.Equal(referenced.ItemValueId, Assert.Single(after.ItemValues).ItemValueId);
        Assert.Equal(item.Id, Assert.Single(after.ItemValuesMap).ItemId);
        Assert.True(after.BaseItems.Any(e => e.Id.Equals(item.Id)));
    }

    private static BaseItemEntity CreateItem(bool isFolder = false) => new()
    {
        Id = Guid.NewGuid(),
        Type = isFolder ? typeof(Folder).FullName! : typeof(Book).FullName!,
        IsFolder = isFolder
    };

    private static ItemValue CreateValue(string value) => new()
    {
        ItemValueId = Guid.NewGuid(),
        Type = ItemValueType.Genre,
        Value = value,
        CleanValue = value.ToLowerInvariant()
    };

    private static ItemValueMap Map(BaseItemEntity item, ItemValue value) => new()
    {
        ItemId = item.Id,
        Item = item,
        ItemValueId = value.ItemValueId,
        ItemValue = value
    };
}
