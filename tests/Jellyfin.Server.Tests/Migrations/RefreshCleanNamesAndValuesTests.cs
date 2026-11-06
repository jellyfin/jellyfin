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
using MediaBrowser.Common.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// Tests for the <see cref="RefreshCleanNamesAndValues"/> migration routine.
/// </summary>
public sealed class RefreshCleanNamesAndValuesTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;

    public RefreshCleanNamesAndValuesTests()
    {
        // The connection owns the in-memory database, so it stays open for the whole test.
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
    public async Task PerformAsync_WithCorruptedDateTimeInBaseItems_RefreshesCleanNames()
    {
        var healthyId = Guid.NewGuid();
        var corruptedId = Guid.NewGuid();

        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(new BaseItemEntity
            {
                Id = healthyId,
                Type = "MediaBrowser.Controller.Entities.Movies.Movie",
                Name = "Healthy Movie"
            });
            await context.SaveChangesAsync(Ct);

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO BaseItems (Id, Type, Name, DateCreated, IsFolder, IsInMixedFolder, IsLocked, IsMovie, IsRepeat, IsSeries, IsVirtualItem) VALUES ({corruptedId}, 'MediaBrowser.Controller.Entities.Movies.Movie', 'Corrupted Movie', '2023-01-17 03:02:94.3383473', 0, 0, 0, 1, 0, 0, 0);",
                Ct);
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDbContext);

        var migration = new RefreshCleanNamesAndValues(
            new StartupLogger<RefreshCleanNamesAndValues>(NullLogger<RefreshCleanNamesAndValues>.Instance),
            factory.Object);

        await migration.PerformAsync(Ct);

        using (var verifyContext = CreateDbContext())
        {
            var healthyItem = await verifyContext.BaseItems
                .SingleAsync(b => b.Id.Equals(healthyId), Ct);
            Assert.Equal("healthy movie", healthyItem.CleanName);

            var corruptedItem = await verifyContext.BaseItems
                .Where(b => b.Id.Equals(corruptedId))
                .Select(b => new { b.Id, b.Name, b.CleanName })
                .SingleAsync(Ct);
            Assert.Equal("Corrupted Movie", corruptedItem.Name);
            Assert.Null(corruptedItem.CleanName);
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private JellyfinDbContext CreateDbContext() => new(
        _dbOptions,
        NullLogger<JellyfinDbContext>.Instance,
        new SqliteDatabaseProvider(new Mock<IApplicationPaths>().Object, NullLogger<SqliteDatabaseProvider>.Instance),
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
}
