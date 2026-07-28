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
    public void ResumeFilter_VersionProgress_SurfacesPlayedVersion()
    {
        Guid userId, primaryId, versionId, otherId;

        using (var ctx = CreateDbContext())
        {
            (userId, primaryId, versionId, otherId) = Seed(ctx);
        }

        using (var ctx = CreateDbContext())
        {
            var inProgress = ctx.UserData
                .Where(ud => ud.UserId == userId && ud.PlaybackPositionTicks > 0);

            // Scope to the seeded items; EnsureCreated also seeds a placeholder row.
            var seededIds = new[] { primaryId, versionId, otherId };

            // Mirrors the resumable=true filter in BaseItemRepository.TranslateQuery.
            var inProgressIds = inProgress.Select(ud => ud.ItemId);
            var resumable = ctx.BaseItems
                .Where(e => seededIds.Contains(e.Id))
                .Where(e => inProgressIds.Contains(e.Id))
                .Where(e => !ctx.BaseItems
                    .Where(s => s.Id != e.Id
                        && inProgressIds.Contains(s.Id)
                        && (s.PrimaryVersionId ?? s.Id) == (e.PrimaryVersionId ?? e.Id))
                    .Any(s =>
                        inProgress.Where(su => su.ItemId == s.Id).Max(su => su.LastPlayedDate)
                            > inProgress.Where(eu => eu.ItemId == e.Id).Max(eu => eu.LastPlayedDate)
                        || (inProgress.Where(su => su.ItemId == s.Id).Max(su => su.LastPlayedDate)
                                == inProgress.Where(eu => eu.ItemId == e.Id).Max(eu => eu.LastPlayedDate)
                            && s.Id.CompareTo(e.Id) < 0)))
                .Select(e => e.Id)
                .ToList();

            Assert.Equal([versionId], resumable);

            // The not-resumable direction keeps primaries only.
            var resumableMovieIds = inProgress
                .Join(ctx.BaseItems, ud => ud.ItemId, bi => bi.Id, (ud, bi) => bi.PrimaryVersionId ?? bi.Id);
            var notResumable = ctx.BaseItems
                .Where(e => seededIds.Contains(e.Id) && e.PrimaryVersionId == null)
                .Where(e => !resumableMovieIds.Contains(e.Id))
                .Select(e => e.Id)
                .ToList();

            Assert.Equal([otherId], notResumable);
        }
    }

    [Fact]
    public void ResumeFilter_TiedLastPlayedDate_KeepsSingleVersion()
    {
        Guid userId, primaryId, versionAId, versionBId;

        using (var ctx = CreateDbContext())
        {
            (userId, primaryId, versionAId, versionBId) = SeedTiedVersions(ctx);
        }

        using (var ctx = CreateDbContext())
        {
            var inProgress = ctx.UserData
                .Where(ud => ud.UserId == userId && ud.PlaybackPositionTicks > 0);

            var seededIds = new[] { primaryId, versionAId, versionBId };
            var inProgressIds = inProgress.Select(ud => ud.ItemId);

            // The exact production dedup, including the Guid.CompareTo tie-break. This asserts the
            // expression translates on SQLite and that two versions sharing an identical LastPlayedDate
            // collapse to a single row instead of double-listing the item in Continue Watching.
            var resumable = ctx.BaseItems
                .Where(e => seededIds.Contains(e.Id))
                .Where(e => inProgressIds.Contains(e.Id))
                .Where(e => !ctx.BaseItems
                    .Where(s => s.Id != e.Id
                        && inProgressIds.Contains(s.Id)
                        && (s.PrimaryVersionId ?? s.Id) == (e.PrimaryVersionId ?? e.Id))
                    .Any(s =>
                        inProgress.Where(su => su.ItemId == s.Id).Max(su => su.LastPlayedDate)
                            > inProgress.Where(eu => eu.ItemId == e.Id).Max(eu => eu.LastPlayedDate)
                        || (inProgress.Where(su => su.ItemId == s.Id).Max(su => su.LastPlayedDate)
                                == inProgress.Where(eu => eu.ItemId == e.Id).Max(eu => eu.LastPlayedDate)
                            && s.Id.CompareTo(e.Id) < 0)))
                .Select(e => e.Id)
                .ToList();

            var survivor = Assert.Single(resumable);
            Assert.Contains(survivor, new[] { versionAId, versionBId });
        }
    }

    [Fact]
    public void DatePlayedOrdering_VersionProgress_SortsPrimaryByVersionDate()
    {
        Guid userId, primaryId, otherId;

        using (var ctx = CreateDbContext())
        {
            (userId, primaryId, _, otherId) = Seed(ctx);
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

    private static (Guid UserId, Guid PrimaryId, Guid VersionId, Guid OtherId) Seed(JellyfinDbContext ctx)
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
        return (user.Id, primary.Id, version.Id, other.Id);
    }

    private static (Guid UserId, Guid PrimaryId, Guid VersionAId, Guid VersionBId) SeedTiedVersions(JellyfinDbContext ctx)
    {
        var user = new User("test", "auth-provider", "reset-provider");
        ctx.Users.Add(user);

        var primary = new BaseItemEntity { Id = Guid.NewGuid(), Type = "MediaBrowser.Controller.Entities.Movies.Movie" };
        var versionA = new BaseItemEntity { Id = Guid.NewGuid(), Type = "MediaBrowser.Controller.Entities.Movies.Movie", PrimaryVersionId = primary.Id };
        var versionB = new BaseItemEntity { Id = Guid.NewGuid(), Type = "MediaBrowser.Controller.Entities.Movies.Movie", PrimaryVersionId = primary.Id };
        ctx.BaseItems.AddRange(primary, versionA, versionB);

        // Both versions in progress with the exact same LastPlayedDate - the tie that a strict '>' cannot break.
        var tied = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        ctx.UserData.Add(new UserData
        {
            ItemId = versionA.Id,
            Item = versionA,
            UserId = user.Id,
            User = user,
            CustomDataKey = versionA.Id.ToString("N"),
            PlaybackPositionTicks = 1000,
            LastPlayedDate = tied
        });
        ctx.UserData.Add(new UserData
        {
            ItemId = versionB.Id,
            Item = versionB,
            UserId = user.Id,
            User = user,
            CustomDataKey = versionB.Id.ToString("N"),
            PlaybackPositionTicks = 2000,
            LastPlayedDate = tied
        });

        ctx.SaveChanges();
        return (user.Id, primary.Id, versionA.Id, versionB.Id);
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
