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

public sealed class ItemLinkCleanupTests : SqliteDbTestFixture
{
    private readonly ItemPersistenceService _service;

    public ItemLinkCleanupTests()
    {
        _service = new ItemPersistenceService(
            CreateDbContextFactory(),
            Mock.Of<IServerApplicationHost>(),
            NullLogger<ItemPersistenceService>.Instance);
    }

    [Fact]
    public void DeleteItem_BatchWithDescendantAndOwnedExtra_CleansAllRemovedLinks()
    {
        var parent = CreateItem(isFolder: true);
        var child = CreateItem();
        child.ParentId = parent.Id;
        var extra = CreateItem();
        extra.OwnerId = child.Id;
        var otherDeleted = CreateItem();
        var survivor = CreateItem();
        var shared = Guid.NewGuid();
        using (var context = CreateDbContext())
        {
            context.BaseItemGenres.AddRange(
                Link(parent, Guid.NewGuid()),
                Link(child, shared),
                Link(otherDeleted, shared),
                Link(extra, Guid.NewGuid()),
                Link(survivor, shared));
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
        Assert.Equal(survivor.Id, Assert.Single(after.BaseItemGenres).ItemId);
        Assert.Equal(survivor.Id, Assert.Single(after.BaseItems.Where(e => !e.Id.Equals(BaseItemRepository.PlaceholderId))).Id);
        Assert.Empty(after.AncestorIds);
    }

    [Fact]
    public void DeleteItem_ChildWithoutAncestorRows_CleansLinkRemovedByCascade()
    {
        var parent = CreateItem(isFolder: true);
        var child = CreateItem();
        child.ParentId = parent.Id;
        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(parent);
            context.BaseItemGenres.Add(Link(child, Guid.NewGuid()));
            context.SaveChanges();
        }

        _service.DeleteItem([parent.Id]);

        using var after = CreateDbContext();
        Assert.False(after.BaseItems.Any(e => e.Id.Equals(child.Id)));
        Assert.Empty(after.BaseItemGenres);
    }

    [Fact]
    public void DeleteItem_GenreItem_RemovesTheLinksPointingAtIt()
    {
        // Nothing keys the genre end of a link, so only the delete itself can clean it up.
        var genre = CreateItem();
        var tagged = CreateItem();
        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(genre);
            context.BaseItemGenres.Add(Link(tagged, genre.Id));
            context.SaveChanges();
        }

        _service.DeleteItem([genre.Id]);

        using var after = CreateDbContext();
        Assert.True(after.BaseItems.Any(e => e.Id.Equals(tagged.Id)));
        Assert.Empty(after.BaseItemGenres);
    }

    private static BaseItemEntity CreateItem(bool isFolder = false) => new()
    {
        Id = Guid.NewGuid(),
        Type = isFolder ? typeof(Folder).FullName! : typeof(Book).FullName!,
        IsFolder = isFolder
    };

    private static BaseItemGenre Link(BaseItemEntity item, Guid genreItemId) => new()
    {
        ItemId = item.Id,
        Item = item,
        GenreItemId = genreItemId
    };
}
