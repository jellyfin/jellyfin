using System;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// DeleteItem has to hand SQLite one statement that already contains everything the foreign keys
/// on BaseItems require, because FK_BaseItems_BaseItems_OwnerId is NO ACTION: anything left behind
/// pointing at a deleted row fails the whole delete with SQLite error 19.
/// </summary>
public sealed class ItemPersistenceDeleteItemTests : JellyfinDbTestFixture
{
    private static readonly Guid _owner = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid _extra = Guid.Parse("eeeeeeee-0000-0000-0000-000000000001");
    private static readonly Guid _extraOfExtra = Guid.Parse("eeeeeeee-0000-0000-0000-000000000002");
    private static readonly Guid _child = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    private static readonly Guid _extraOfChild = Guid.Parse("eeeeeeee-0000-0000-0000-000000000003");

    private readonly ItemPersistenceService _service;

    public ItemPersistenceDeleteItemTests()
    {
        _service = new ItemPersistenceService(
            CreateDbContextFactory(),
            DatabaseProvider,
            new Mock<IServerApplicationHost>().Object,
            NullLogger<ItemPersistenceService>.Instance);
    }

    [Fact]
    public void DeleteItem_OwnerIdChain_DeletesWholeChain()
    {
        // An extra that owns an extra of its own. Real libraries carry these in bulk, and a single
        // expansion pass over OwnerId leaves the second level behind.
        Seed(
            (_owner, null, null),
            (_extra, _owner, null),
            (_extraOfExtra, _extra, null));

        _service.DeleteItem([_owner]);

        using var context = CreateDbContext();
        Assert.Empty(context.BaseItems.Where(e => e.Id.Equals(_owner) || e.Id.Equals(_extra) || e.Id.Equals(_extraOfExtra)));
    }

    [Fact]
    public void DeleteItem_ExtraOwnedByCascadedChild_DeletesExtraToo()
    {
        // The child goes away through FK_BaseItems_BaseItems_ParentId's ON DELETE CASCADE whether or
        // not it is listed, so an extra owned by that child has to be listed with it.
        Seed(
            (_owner, null, null),
            (_child, null, _owner),
            (_extraOfChild, _child, null));

        _service.DeleteItem([_owner]);

        using var context = CreateDbContext();
        Assert.Empty(context.BaseItems.Where(e => e.Id.Equals(_owner) || e.Id.Equals(_child) || e.Id.Equals(_extraOfChild)));
    }

    [Fact]
    public void DeleteItem_OwnershipCycle_Terminates()
    {
        // A malformed pair that owns each other must not spin the closure loop forever.
        Seed((_owner, null, null), (_extra, _owner, null));

        using (var context = CreateDbContext())
        {
            context.BaseItems.Single(e => e.Id.Equals(_owner)).OwnerId = _extra;
            context.SaveChanges();
        }

        _service.DeleteItem([_owner]);

        using var assertContext = CreateDbContext();
        Assert.Empty(assertContext.BaseItems.Where(e => e.Id.Equals(_owner) || e.Id.Equals(_extra)));
    }

    [Theory]
    [InlineData(20)]
    [InlineData(40)]
    public void DeleteItem_FolderTreeDeeperThanTheCascadeLimitOfSomeServers_DeletesTheWholeTreeAndItsData(int depth)
    {
        // MySQL and MariaDB stop a cascade nested more than 15 levels deep, and ParentId cascades into itself.
        var ids = Enumerable.Range(0, depth).Select(i => Guid.Parse($"dddddddd-0000-0000-0000-{i:D12}")).ToArray();
        var bystander = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
        Seed((bystander, null, null));
        SeedChain(ids);

        _service.DeleteItem([ids[0]]);

        using var context = CreateDbContext();
        Assert.Equal(bystander, Assert.Single(context.BaseItems.Where(e => !e.Id.Equals(BaseItemRepository.PlaceholderId))).Id);
        Assert.Empty(context.KeyframeData);
        Assert.Empty(context.Chapters);
        Assert.Empty(context.BaseItemProviders);
    }

    [Fact]
    public void DeleteItem_SubtreeOfADeepTree_LeavesTheAncestorsAndTheirData()
    {
        var ids = Enumerable.Range(0, 30).Select(i => Guid.Parse($"dddddddd-0000-0000-0000-{i:D12}")).ToArray();
        SeedChain(ids);

        _service.DeleteItem([ids[10]]);

        using var context = CreateDbContext();
        Assert.Equal(ids.Take(10).OrderBy(e => e), context.BaseItems.Where(e => !e.Id.Equals(BaseItemRepository.PlaceholderId)).Select(e => e.Id).AsEnumerable().OrderBy(e => e));
        Assert.Equal(ids.Take(10).OrderBy(e => e), context.KeyframeData.Select(e => e.ItemId).AsEnumerable().OrderBy(e => e));
        Assert.Equal(10, context.Chapters.Count());
        Assert.Equal(10, context.BaseItemProviders.Count());
    }

    private void SeedChain(Guid[] ids)
    {
        using var context = CreateDbContext();

        // Each item is the child of the one before it, and holds one row of each kind of data.
        for (var i = 0; i < ids.Length; i++)
        {
            context.BaseItems.Add(new BaseItemEntity
            {
                Id = ids[i],
                Type = "MediaBrowser.Controller.Entities.Folder",
                IsFolder = true,
                ParentId = i == 0 ? null : ids[i - 1]
            });
            context.SaveChanges();

            context.KeyframeData.Add(new KeyframeData { ItemId = ids[i], TotalDuration = i });
            context.Chapters.Add(new Chapter { ItemId = ids[i], Item = null!, ChapterIndex = 0, StartPositionTicks = i });
            context.BaseItemProviders.Add(new BaseItemProvider { ItemId = ids[i], Item = null!, ProviderId = "Test", ProviderValue = "x" });
            context.SaveChanges();
        }
    }

    private void Seed(params (Guid Id, Guid? OwnerId, Guid? ParentId)[] items)
    {
        using var context = CreateDbContext();

        // Owners before the rows referencing them: the seed itself is foreign key checked.
        foreach (var (id, ownerId, parentId) in items)
        {
            context.BaseItems.Add(new BaseItemEntity
            {
                Id = id,
                Type = "MediaBrowser.Controller.Entities.Video",
                OwnerId = ownerId,
                ParentId = parentId
            });

            context.SaveChanges();
        }
    }
}
