using System;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public class ItemPersistenceServiceSaveImagesTests : SqliteDbTestFixture
{
    private readonly ItemPersistenceService _service;

    public ItemPersistenceServiceSaveImagesTests()
    {
        _service = new ItemPersistenceService(
            CreateDbContextFactory(),
            Mock.Of<IServerApplicationHost>(),
            NullLogger<ItemPersistenceService>.Instance);
    }

    [Fact]
    public async Task SaveImagesAsync_ReplacesThePreviousImages()
    {
        var itemId = Guid.NewGuid();
        Seed(itemId);

        await _service.SaveImagesAsync(CreateItem(itemId, "/first.jpg"), TestContext.Current.CancellationToken);
        await _service.SaveImagesAsync(CreateItem(itemId, "/second.jpg"), TestContext.Current.CancellationToken);

        using var context = CreateDbContext();
        var paths = context.BaseItemImageInfos
            .Where(e => e.ItemId.Equals(itemId))
            .Select(e => e.Path)
            .ToList();

        Assert.Equal(["/second.jpg"], paths);
    }

    [Fact]
    public async Task SaveImagesAsync_ItemDeletedFromUnderIt_IsANoOp()
    {
        // A scan can delete the item between the refresh reading it and the images being written. That
        // must not fail the whole refresh, and must not leave the images of an item that is gone.
        var itemId = Guid.NewGuid();

        await _service.SaveImagesAsync(CreateItem(itemId, "/gone.jpg"), TestContext.Current.CancellationToken);

        using var context = CreateDbContext();
        Assert.Empty(context.BaseItemImageInfos.Where(e => e.ItemId.Equals(itemId)));
    }

    private static BaseItem CreateItem(Guid itemId, string imagePath)
        => new Folder
        {
            Id = itemId,
            ImageInfos = [new ItemImageInfo { Path = imagePath, Type = ImageType.Primary }]
        };

    private void Seed(Guid itemId)
    {
        using var context = CreateDbContext();
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = itemId,
            Type = "Folder",
            IsFolder = true
        });
        context.SaveChanges();
    }
}
