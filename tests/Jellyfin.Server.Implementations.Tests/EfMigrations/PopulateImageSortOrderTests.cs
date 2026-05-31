using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Migrations.Routines;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
            .UseSqlite(_connection)
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
