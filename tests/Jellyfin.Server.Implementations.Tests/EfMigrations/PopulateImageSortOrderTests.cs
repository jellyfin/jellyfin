using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Migrations;
using Jellyfin.Server.Migrations.Routines;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.EfMigrations;

public sealed class PopulateImageSortOrderTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;

    public PopulateImageSortOrderTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection, options => options.MigrationsAssembly(typeof(SqliteDatabaseProvider).Assembly))
            .Options;

        using var context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task PerformAsync_PopulatesSortOrderByItemTypePriorityAndNaturalNumber()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var itemId = Guid.NewGuid();
        var item = new BaseItemEntity
        {
            Id = itemId,
            Type = "MediaBrowser.Controller.Entities.Movies.Movie",
            Path = "/media/Movie/Movie.mkv"
        };

        await using (var context = CreateDbContext())
        {
            context.BaseItems.Add(item);
            context.BaseItemImageInfos.AddRange(
                CreateImage(item, "/media/Movie/backdrop2.jpg", ImageInfoImageType.Backdrop, 99),
                CreateImage(item, "/media/Movie/fanart-10.jpg", ImageInfoImageType.Backdrop, 99),
                CreateImage(item, "/media/Movie/extrafanart/fanart1.jpg", ImageInfoImageType.Backdrop, 99),
                CreateImage(item, "/media/Movie/Movie-fanart.jpg", ImageInfoImageType.Backdrop, 99),
                CreateImage(item, "/media/Movie/fanart-2.jpg", ImageInfoImageType.Backdrop, 99),
                CreateImage(item, "/media/Movie/poster.jpg", ImageInfoImageType.Primary, 99));
            await context.SaveChangesAsync(cancellationToken);
        }

        var migration = new PopulateImageSortOrder(
            new StartupLogger<PopulateImageSortOrder>(NullLogger<PopulateImageSortOrder>.Instance),
            CreateDbContextFactory());

        await migration.PerformAsync(cancellationToken);

        await using var assertContext = CreateDbContext();
        var backdrops = await assertContext.BaseItemImageInfos
            .Where(i => i.ItemId.Equals(itemId) && i.ImageType == ImageInfoImageType.Backdrop)
            .OrderBy(i => i.SortOrder)
            .ToArrayAsync(cancellationToken);
        var primary = await assertContext.BaseItemImageInfos
            .SingleAsync(i => i.ItemId.Equals(itemId) && i.ImageType == ImageInfoImageType.Primary, cancellationToken);

        Assert.Equal(
            [
                "/media/Movie/Movie-fanart.jpg",
                "/media/Movie/fanart-2.jpg",
                "/media/Movie/fanart-10.jpg",
                "/media/Movie/extrafanart/fanart1.jpg",
                "/media/Movie/backdrop2.jpg"
            ],
            backdrops.Select(i => i.Path));
        Assert.Equal([0, 1, 2, 3, 4], backdrops.Select(i => i.SortOrder));
        Assert.Equal(0, primary.SortOrder);
    }

    private static BaseItemImageInfo CreateImage(BaseItemEntity item, string path, ImageInfoImageType imageType, int sortOrder)
    {
        return new BaseItemImageInfo
        {
            Id = Guid.NewGuid(),
            Item = item,
            ItemId = item.Id,
            ImageType = imageType,
            Path = path,
            SortOrder = sortOrder
        };
    }

    [Fact]
    public async Task SchemaMigrationScript_BackfillsSortOrderWithinEachImageType()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var item = new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = "MediaBrowser.Controller.Entities.Movies.Movie",
            Path = "/media/Movie/Movie.mkv"
        };

        await using var context = CreateDbContext();
        context.BaseItems.Add(item);
        context.BaseItemImageInfos.AddRange(
            CreateImage(item, "/media/Movie/fanart-b.jpg", ImageInfoImageType.Backdrop, 99),
            CreateImage(item, "/media/Movie/fanart-a.jpg", ImageInfoImageType.Backdrop, 99),
            CreateImage(item, "/media/Movie/poster.jpg", ImageInfoImageType.Primary, 99));
        await context.SaveChangesAsync(cancellationToken);

        await context.Database.ExecuteSqlRawAsync("DROP INDEX \"IX_BaseItemImageInfos_ItemId_ImageType_SortOrder\";", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("ALTER TABLE \"BaseItemImageInfos\" DROP COLUMN \"SortOrder\";", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE INDEX \"IX_BaseItemImageInfos_ItemId_ImageType\" ON \"BaseItemImageInfos\" (\"ItemId\", \"ImageType\");", cancellationToken);
        await context.Database.ExecuteSqlRawAsync(context.GetService<IHistoryRepository>().GetCreateScript(), cancellationToken);

        var efMigrationId = typeof(Database.Providers.Sqlite.Migrations.AddSortOrderToBaseItemImageInfo)
            .GetCustomAttribute<MigrationAttribute>()?.Id;
        var migrations = context.Database.GetMigrations().ToArray();
        Assert.Equal(efMigrationId, migrations[^1]);

        var script = context.GetService<IMigrator>().GenerateScript(migrations[^2], efMigrationId);
        await context.Database.ExecuteSqlRawAsync(script, cancellationToken);
        context.ChangeTracker.Clear();

        var images = await context.BaseItemImageInfos
            .OrderBy(i => i.Path)
            .ToArrayAsync(cancellationToken);
        Assert.Equal([0, 1, 0], images.Select(i => i.SortOrder));
    }

    [Fact]
    public void MigrationOrder_RoutineRunsAfterSortOrderSchemaMigration()
    {
        var routineAttribute = typeof(PopulateImageSortOrder).GetCustomAttribute<JellyfinMigrationAttribute>();
        Assert.NotNull(routineAttribute);

        var routineKey = routineAttribute.Order.ToString("yyyyMMddHHmmsss", CultureInfo.InvariantCulture) + "_" + routineAttribute.Name;
        var efMigrationId = typeof(Database.Providers.Sqlite.Migrations.AddSortOrderToBaseItemImageInfo)
            .GetCustomAttribute<MigrationAttribute>()?.Id;

        Assert.NotNull(efMigrationId);
        Assert.True(
            string.CompareOrdinal(efMigrationId, routineKey) < 0,
            $"Routine key '{routineKey}' must sort after EF migration id '{efMigrationId}'.");
    }

    private IDbContextFactory<JellyfinDbContext> CreateDbContextFactory()
    {
        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDbContext);
        return factory.Object;
    }

    private JellyfinDbContext CreateDbContext()
    {
        return new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }
}
