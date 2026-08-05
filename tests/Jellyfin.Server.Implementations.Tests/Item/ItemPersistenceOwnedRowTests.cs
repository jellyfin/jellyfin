using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class ItemPersistenceOwnedRowTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly ItemPersistenceService _service;
    private readonly IApplicationPaths _applicationPaths;
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;

    public ItemPersistenceOwnedRowTests()
    {
        _applicationPaths = new Mock<IApplicationPaths>().Object;

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var ctx = CreateDbContext())
        {
            ctx.Database.EnsureCreated();
        }

        // BaseItem resolves these through process-wide statics; restored in Dispose.
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousConfigurationManager = BaseItem.ConfigurationManager;

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.GetCollectionFolders(It.IsAny<BaseItem>()))
            .Returns([]);
        BaseItem.LibraryManager = libraryManager.Object;

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configurationManager.Object;

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);

        _service = new ItemPersistenceService(
            factory.Object,
            new Mock<IServerApplicationHost>().Object,
            NullLogger<ItemPersistenceService>.Instance);
    }

    public void Dispose()
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.ConfigurationManager = _previousConfigurationManager!;
        _connection.Dispose();
    }

    [Fact]
    public void SaveItems_UpdateExistingItem_ReplacesOwnedRows()
    {
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        _service.SaveItems(
            [CreateBook(id, new() { ["Imdb"] = "tt0001", ["Tmdb"] = "555" }, [MetadataField.Name])],
            CancellationToken.None);

        using (var ctx = CreateDbContext())
        {
            Assert.Equal(2, ctx.BaseItemProviders.Count(e => e.ItemId.Equals(id)));
            Assert.Equal(1, ctx.BaseItemImageInfos.Count(e => e.ItemId.Equals(id)));
            Assert.Equal(1, ctx.BaseItemMetadataFields.Count(e => e.ItemId.Equals(id)));
        }

        // Re-save with different owned rows: the update path rewrites all three tables wholesale.
        _service.SaveItems(
            [CreateBook(id, new() { ["Imdb"] = "tt9999" }, [MetadataField.Name, MetadataField.Genres])],
            CancellationToken.None);

        using (var ctx = CreateDbContext())
        {
            var providers = ctx.BaseItemProviders.Where(e => e.ItemId.Equals(id)).ToList();
            Assert.Equal("tt9999", Assert.Single(providers).ProviderValue);

            Assert.Equal(1, ctx.BaseItemImageInfos.Count(e => e.ItemId.Equals(id)));
            Assert.Equal(2, ctx.BaseItemMetadataFields.Count(e => e.ItemId.Equals(id)));
        }
    }

    [Fact]
    public void SaveItems_MixedNewAndExistingBatch_ReplacesOnlyExistingOwnedRows()
    {
        var existing = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var fresh = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        _service.SaveItems([CreateBook(existing, new() { ["Imdb"] = "tt0001" }, [])], CancellationToken.None);

        // One already-persisted item and one brand new item in the same batch.
        _service.SaveItems(
            [
                CreateBook(existing, new() { ["Imdb"] = "tt0002" }, []),
                CreateBook(fresh, new() { ["Tmdb"] = "777" }, [])
            ],
            CancellationToken.None);

        using var ctx = CreateDbContext();
        Assert.Equal("tt0002", Assert.Single(ctx.BaseItemProviders.Where(e => e.ItemId.Equals(existing))).ProviderValue);
        Assert.Equal("777", Assert.Single(ctx.BaseItemProviders.Where(e => e.ItemId.Equals(fresh))).ProviderValue);
    }

    private static Book CreateBook(Guid id, Dictionary<string, string> providerIds, MetadataField[] lockedFields)
    {
        var book = new Book
        {
            Id = id,
            Name = "Book",
            ProviderIds = providerIds,
            LockedFields = lockedFields
        };

        book.SetImage(new ItemImageInfo { Path = "/img/primary.jpg", Type = ImageType.Primary }, 0);
        return book;
    }

    private JellyfinDbContext CreateDbContext() => new(
        _dbOptions,
        NullLogger<JellyfinDbContext>.Instance,
        new SqliteDatabaseProvider(_applicationPaths, NullLogger<SqliteDatabaseProvider>.Instance),
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
}
