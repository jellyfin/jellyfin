using System;
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

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// A date the database holds but cannot read back aborts every later migration that materialises
/// the row, which is what blocks the upgrade in jellyfin/jellyfin#17849.
/// </summary>
public sealed class ClearUnreadableDatesTests : IAsyncDisposable
{
    // Seconds are out of range, so neither SQLite nor DateTime.Parse can read this value.
    private const string UnreadableDate = "2023-01-17 03:02:94.3383473";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly Guid _itemId = Guid.NewGuid();

    public ClearUnreadableDatesTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>().UseSqlite(_connection).Options;
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task PerformAsync_UnreadableDate_ClearsItAndLetsTheRowLoad()
    {
        await SeedAsync().ConfigureAwait(true);

        await using (var broken = CreateDbContext())
        {
            await Assert.ThrowsAsync<FormatException>(
                () => broken.BaseItems.AsNoTracking().SingleAsync(e => e.Id.Equals(_itemId), TestContext.Current.CancellationToken)).ConfigureAwait(true);
        }

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        await using var context = CreateDbContext();
        var item = await context.BaseItems.AsNoTracking().SingleAsync(e => e.Id.Equals(_itemId), TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(_itemId, item.Id);
        Assert.Null(item.DateCreated);
    }

    [Fact]
    public async Task PerformAsync_ReadableDate_LeavesItAlone()
    {
        var kept = new DateTime(2023, 1, 17, 3, 2, 4, DateTimeKind.Utc);

        await using (var seed = CreateDbContext())
        {
            await seed.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            seed.BaseItems.Add(new BaseItemEntity
            {
                Id = _itemId,
                Type = "MediaBrowser.Controller.Entities.Movies.Movie",
                Name = "A",
                PresentationUniqueKey = _itemId.ToString("N"),
                DateCreated = kept
            });
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        await using var context = CreateDbContext();
        var item = await context.BaseItems.AsNoTracking().SingleAsync(e => e.Id.Equals(_itemId), TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(kept, item.DateCreated);
    }

    [Fact]
    public async Task PerformAsync_UnreadableDateInANonNullableColumn_ReplacesItWithTheUnknownDate()
    {
        var userId = Guid.NewGuid();

        await using (var seed = CreateDbContext())
        {
            await seed.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            seed.ActivityLogs.Add(new ActivityLog("entry", "type", userId));
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            await seed.Database.ExecuteSqlRawAsync(
                "UPDATE ActivityLogs SET DateCreated = {0}",
                [UnreadableDate],
                TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        await using (var broken = CreateDbContext())
        {
            await Assert.ThrowsAsync<FormatException>(
                () => broken.ActivityLogs.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).ConfigureAwait(true);
        }

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        await using var context = CreateDbContext();
        var entry = await context.ActivityLogs.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(default(DateTime), entry.DateCreated);
    }

    private async Task SeedAsync()
    {
        await using var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        context.BaseItems.Add(new BaseItemEntity
        {
            Id = _itemId,
            Type = "MediaBrowser.Controller.Entities.Movies.Movie",
            Name = "A",
            PresentationUniqueKey = _itemId.ToString("N"),
            DateCreated = DateTime.UtcNow
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Written directly, because EF cannot produce a value it is unable to read back.
        await context.Database.ExecuteSqlRawAsync(
            "UPDATE BaseItems SET DateCreated = {0} WHERE Id = {1}",
            [UnreadableDate, _itemId],
            TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    private ClearUnreadableDates CreateMigration()
    {
        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(CreateDbContext);

        return new ClearUnreadableDates(
            NullLogger<ClearUnreadableDates>.Instance,
            new StartupLogger<ClearUnreadableDates>(NullLogger<ClearUnreadableDates>.Instance),
            factory.Object);
    }

    private JellyfinDbContext CreateDbContext() => new(
        _dbOptions,
        NullLogger<JellyfinDbContext>.Instance,
        new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
}
