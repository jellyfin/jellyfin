using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Migrations.Routines;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

public sealed class ClearDanglingItemReferencesTests : IDisposable
{
    // The last migration a 10.10 jellyfin.db carries; its items are still in library.db at that point.
    private const string Last1010Migration = "20240928082930_MarkSegmentProviderIdNonNullable";

    private static readonly Guid _userId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _itemId = new("22222222-2222-2222-2222-222222222222");

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;

    public ClearDanglingItemReferencesTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection, o => o.MigrationsAssembly(typeof(SqliteDatabaseProvider).Assembly))
            .Options;
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task PerformAsync_UserSettingsDisplayPreferences_Survive()
    {
        using (var context = CreateDbContext())
        {
            await context.Database.MigrateAsync(Ct);
            context.Users.Add(new User("user", "Default", "Default") { Id = _userId });
            await context.SaveChangesAsync(Ct);

            // jellyfin-web keeps its settings under the "usersettings" id, which is stored as its MD5 and is no item.
            var key = "usersettings".GetMD5();
            context.ItemDisplayPreferences.Add(new ItemDisplayPreferences(_userId, key, "emby"));
            context.CustomItemDisplayPreferences.Add(new CustomItemDisplayPreferences(_userId, key, "emby", "enableThemeSongs", "true"));
            await context.SaveChangesAsync(Ct);
        }

        await CreateMigration().PerformAsync(Ct);

        using (var context = CreateDbContext())
        {
            Assert.Single(context.ItemDisplayPreferences);
            Assert.Single(context.CustomItemDisplayPreferences);
        }
    }

    [Fact]
    public async Task Upgrade1010_TrickplayAndSegments_RestoredOnceTheLibraryIsMigrated()
    {
        var orphanId = Guid.NewGuid();
        using (var context = CreateDbContext())
        {
            await context.GetService<IMigrator>().MigrateAsync(Last1010Migration, cancellationToken: Ct);
            foreach (var id in new[] { _itemId, orphanId })
            {
                context.TrickplayInfos.Add(new TrickplayInfo { ItemId = id, Width = 320, Height = 180, TileWidth = 10, TileHeight = 10, ThumbnailCount = 1, Interval = 10000, Bandwidth = 1 });
                context.MediaSegments.Add(new MediaSegment { Id = Guid.NewGuid(), ItemId = id, Type = MediaSegmentType.Intro, StartTicks = 0, EndTicks = 1000, SegmentProviderId = "Test" });
            }

            await context.SaveChangesAsync(Ct);

            // BaseItems is empty until MigrateLibraryDb, so the constraint has to set both rows aside.
            await context.Database.MigrateAsync(Ct);
            Assert.Empty(context.TrickplayInfos);
            Assert.Empty(context.MediaSegments);

            context.BaseItems.Add(new BaseItemEntity { Id = _itemId, Type = "Movie" });
            await context.SaveChangesAsync(Ct);
            MigrateLibraryDb.RestoreOrphanedItemData(context);
        }

        await CreateMigration().PerformAsync(Ct);

        using (var context = CreateDbContext())
        {
            Assert.Equal(_itemId, Assert.Single(context.TrickplayInfos).ItemId);
            Assert.Equal(_itemId, Assert.Single(context.MediaSegments).ItemId);
            Assert.Equal(0, context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sqlite_master WHERE name LIKE 'Orphaned%'").AsEnumerable().First());
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private JellyfinDbContext CreateDbContext() => new(
        _dbOptions,
        NullLogger<JellyfinDbContext>.Instance,
        new SqliteDatabaseProvider(new Mock<IApplicationPaths>().Object, NullLogger<SqliteDatabaseProvider>.Instance),
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

    private ClearDanglingItemReferences CreateMigration()
    {
        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(CreateDbContext);

        return new ClearDanglingItemReferences(
            new StartupLogger<ClearDanglingItemReferences>(NullLogger<ClearDanglingItemReferences>.Instance),
            factory.Object);
    }
}
