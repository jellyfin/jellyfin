#pragma warning disable RS0030 // Do not use banned APIs: Guid == is required inside EF expression trees to mirror the production query shapes.

using System;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Verifies that the alternate-version-aware query shapes used by the resume filter
/// (BaseItemRepository.TranslateQuery) and the DatePlayed ordering (OrderMapper) translate
/// and evaluate correctly on the SQLite provider.
/// </summary>
public sealed class AlternateVersionQueryTranslationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;

    public AlternateVersionQueryTranslationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = CreateDbContext();
        ctx.Database.EnsureCreated();
    }

    [Fact]
    public void ResumeFilter_VersionProgress_SurfacesPrimary()
    {
        Guid userId, primaryId, otherId;

        using (var ctx = CreateDbContext())
        {
            (userId, primaryId, otherId) = Seed(ctx);
        }

        using (var ctx = CreateDbContext())
        {
            // Mirrors the resumable filter in BaseItemRepository.TranslateQuery: progress on any
            // version coalesces onto the primary's id.
            var inProgress = ctx.UserData
                .Where(ud => ud.UserId == userId && ud.PlaybackPositionTicks > 0);
            var resumableMovieIds = inProgress
                .Join(ctx.BaseItems, ud => ud.ItemId, bi => bi.Id, (ud, bi) => bi.PrimaryVersionId ?? bi.Id);

            // Scope to the seeded items; EnsureCreated also seeds a placeholder row.
            var seededIds = new[] { primaryId, otherId };

            var resumable = ctx.BaseItems
                .Where(e => seededIds.Contains(e.Id) && e.PrimaryVersionId == null)
                .Where(e => resumableMovieIds.Contains(e.Id))
                .Select(e => e.Id)
                .ToList();

            Assert.Equal([primaryId], resumable);

            // The inverse (not-resumable) direction must exclude the primary as well.
            var notResumable = ctx.BaseItems
                .Where(e => seededIds.Contains(e.Id) && e.PrimaryVersionId == null)
                .Where(e => resumableMovieIds.Contains(e.Id) == false)
                .Select(e => e.Id)
                .ToList();

            Assert.Equal([otherId], notResumable);
        }
    }

    [Fact]
    public void DatePlayedOrdering_VersionProgress_SortsPrimaryByVersionDate()
    {
        Guid userId, primaryId, otherId;

        using (var ctx = CreateDbContext())
        {
            (userId, primaryId, otherId) = Seed(ctx);
        }

        using (var ctx = CreateDbContext())
        {
            // Scope to the seeded items; EnsureCreated also seeds a placeholder row.
            var seededIds = new[] { primaryId, otherId };

            // Mirrors the DatePlayed mapping in OrderMapper.
            var ordered = ctx.BaseItems
                .Where(e => seededIds.Contains(e.Id) && e.PrimaryVersionId == null)
                .OrderByDescending(e => ctx.UserData
                    .Where(w => w.UserId == userId && (w.ItemId == e.Id || w.Item!.PrimaryVersionId == e.Id))
                    .Max(f => f.LastPlayedDate))
                .Select(e => e.Id)
                .ToList();

            // The movie whose only progress is on its alternate version sorts before the unplayed one.
            Assert.Equal([primaryId, otherId], ordered);
        }
    }

    private static (Guid UserId, Guid PrimaryId, Guid OtherId) Seed(JellyfinDbContext ctx)
    {
        var user = new User("test", "auth-provider", "reset-provider");
        ctx.Users.Add(user);

        var primary = new BaseItemEntity { Id = Guid.NewGuid(), Type = "MediaBrowser.Controller.Entities.Movies.Movie" };
        var version = new BaseItemEntity { Id = Guid.NewGuid(), Type = "MediaBrowser.Controller.Entities.Movies.Movie", PrimaryVersionId = primary.Id };
        var other = new BaseItemEntity { Id = Guid.NewGuid(), Type = "MediaBrowser.Controller.Entities.Movies.Movie" };
        ctx.BaseItems.AddRange(primary, version, other);

        // Progress only on the alternate version.
        ctx.UserData.Add(new UserData
        {
            ItemId = version.Id,
            Item = version,
            UserId = user.Id,
            User = user,
            CustomDataKey = version.Id.ToString("N"),
            PlaybackPositionTicks = 1000,
            LastPlayedDate = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        });

        ctx.SaveChanges();
        return (user.Id, primary.Id, other.Id);
    }

    private JellyfinDbContext CreateDbContext()
    {
        return new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
