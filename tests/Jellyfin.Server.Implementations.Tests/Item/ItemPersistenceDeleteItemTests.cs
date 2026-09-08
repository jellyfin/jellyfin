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
public sealed class ItemPersistenceDeleteItemTests : SqliteDbTestFixture
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
