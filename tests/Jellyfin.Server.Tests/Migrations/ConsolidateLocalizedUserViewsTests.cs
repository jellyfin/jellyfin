using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Migrations.Routines;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// Covers the references a database carried up from 10.x can still hold against a view the migration
/// is about to drop. Only ParentId cascades; everything else here is a NO ACTION foreign key that
/// used to abort the migration, and with it the whole startup.
/// </summary>
public sealed class ConsolidateLocalizedUserViewsTests : IDisposable
{
    private const string MetadataPath = "/metadata";

    private static readonly Guid _staleId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _canonicalId = new("22222222-2222-2222-2222-222222222222");

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;

    public ConsolidateLocalizedUserViewsTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task PerformAsync_ItemOwnedByStaleView_MovesItAndDropsTheView()
    {
        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(StaleView());
            context.BaseItems.Add(new BaseItemEntity { Id = Guid.NewGuid(), Type = "Trailer", OwnerId = _staleId });
            await context.SaveChangesAsync(Ct);
        }

        await CreateMigration().PerformAsync(Ct);

        using (var context = CreateDbContext())
        {
            Assert.Null(await context.BaseItems.FindAsync([_staleId], Ct));
            Assert.Equal(_canonicalId, (await context.BaseItems.SingleAsync(e => e.Type == "Trailer", Ct)).OwnerId);
        }
    }

    [Fact]
    public async Task PerformAsync_StaleViewInLinkedChildren_DropsTheLinksAndTheView()
    {
        var movieId = Guid.NewGuid();

        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(StaleView());
            context.BaseItems.Add(new BaseItemEntity { Id = movieId, Type = "Movie" });
            context.LinkedChildren.Add(new LinkedChildEntity { ParentId = _staleId, SortOrder = 0, ChildId = movieId, ChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType.Manual });
            context.LinkedChildren.Add(new LinkedChildEntity { ParentId = movieId, SortOrder = 0, ChildId = _staleId, ChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType.Manual });
            await context.SaveChangesAsync(Ct);
        }

        await CreateMigration().PerformAsync(Ct);

        using (var context = CreateDbContext())
        {
            Assert.Null(await context.BaseItems.FindAsync([_staleId], Ct));
            Assert.Empty(context.LinkedChildren);
            Assert.NotNull(await context.BaseItems.FindAsync([movieId], Ct));
        }
    }

    [Fact]
    public async Task PerformAsync_OrphanedAncestry_IsNotResurrectedUnderTheCanonicalView()
    {
        var childId = Guid.NewGuid();
        var orphanId = Guid.NewGuid();

        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(StaleView());
            context.BaseItems.Add(new BaseItemEntity { Id = childId, Type = "Movie", ParentId = _staleId });
            await context.SaveChangesAsync(Ct);

            context.AncestorIds.Add(new AncestorId { ItemId = childId, ParentItemId = _staleId, Item = null!, ParentItem = null! });
            await context.SaveChangesAsync(Ct);

            // Written while foreign keys went unenforced: the item behind it is long gone.
            await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF", Ct);
            context.AncestorIds.Add(new AncestorId { ItemId = orphanId, ParentItemId = _staleId, Item = null!, ParentItem = null! });
            await context.SaveChangesAsync(Ct);
            await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON", Ct);
        }

        await CreateMigration().PerformAsync(Ct);

        using (var context = CreateDbContext())
        {
            Assert.Null(await context.BaseItems.FindAsync([_staleId], Ct));
            Assert.Equal(_canonicalId, (await context.BaseItems.SingleAsync(e => e.Id.Equals(childId), Ct)).ParentId);
            var ancestors = await context.AncestorIds.ToListAsync(Ct);
            Assert.Equal(new[] { childId }, ancestors.Select(e => e.ItemId));
            Assert.Equal(_canonicalId, ancestors[0].ParentItemId);
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private static BaseItemEntity StaleView() => new()
    {
        Id = _staleId,
        Type = "MediaBrowser.Controller.Entities.UserView",
        Path = Path.Combine(MetadataPath, "views", "livetv")
    };

    private JellyfinDbContext CreateDbContext() => new(
        _dbOptions,
        NullLogger<JellyfinDbContext>.Instance,
        new SqliteDatabaseProvider(new Mock<IApplicationPaths>().Object, NullLogger<SqliteDatabaseProvider>.Instance),
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

    private ConsolidateLocalizedUserViews CreateMigration()
    {
        var view = new UserView
        {
            Id = _staleId,
            Path = Path.Combine(MetadataPath, "views", "livetv"),
            Name = "Live TV",
            ViewType = CollectionType.livetv
        };

        var applicationPaths = new Mock<IServerApplicationPaths>();
        applicationPaths.Setup(e => e.InternalMetadataPath).Returns(MetadataPath);

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(e => e.ApplicationPaths).Returns(applicationPaths.Object);

        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(e => e.GetValidFilename(It.IsAny<string>())).Returns((string name) => name);

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(e => e.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns(new List<BaseItem> { view });
        libraryManager.Setup(e => e.GetNewItemId(It.IsAny<string>(), It.IsAny<Type>()))
            .Returns(_canonicalId);
        libraryManager.Setup(e => e.CreateItem(It.IsAny<BaseItem>(), It.IsAny<BaseItem?>()))
            .Callback((BaseItem item, BaseItem? parent) =>
            {
                using var context = CreateDbContext();
                context.BaseItems.Add(new BaseItemEntity
                {
                    Id = item.Id,
                    Type = item.GetType().FullName!,
                    Path = item.Path
                });
                context.SaveChanges();
            });

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(CreateDbContext);

        return new ConsolidateLocalizedUserViews(
            new StartupLogger<ConsolidateLocalizedUserViews>(NullLogger<ConsolidateLocalizedUserViews>.Instance),
            libraryManager.Object,
            configurationManager.Object,
            fileSystem.Object,
            factory.Object);
    }
}
