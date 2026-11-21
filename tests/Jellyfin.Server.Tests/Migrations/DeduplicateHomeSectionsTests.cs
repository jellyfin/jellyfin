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
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

public sealed class DeduplicateHomeSectionsTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _itemId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public DeduplicateHomeSectionsTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task Perform_SectionListSavedMoreThanOnce_KeepsTheNewestRowPerPosition()
    {
        int preferencesId;
        using (var context = CreateDbContext())
        {
            context.Users.Add(new User("user", "auth-provider", "reset-provider") { Id = _userId });

            var preferences = new DisplayPreferences(_userId, _itemId, "client");
            context.DisplayPreferences.Add(preferences);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            preferencesId = preferences.Id;

            // Three saves of a section list, as the old code stored them: every save inserted a full new set.
            for (var save = 1; save <= 3; save++)
            {
                context.Set<HomeSection>().Add(new HomeSection
                {
                    DisplayPreferencesId = preferencesId,
                    Order = 0,
                    Type = save == 3 ? HomeSectionType.Resume : HomeSectionType.SmallLibraryTiles
                });
                context.Set<HomeSection>().Add(new HomeSection
                {
                    DisplayPreferencesId = preferencesId,
                    Order = 1,
                    Type = HomeSectionType.NextUp
                });
            }

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using (var context = CreateDbContext())
        {
            var sections = context.Set<HomeSection>()
                .Where(section => section.DisplayPreferencesId == preferencesId)
                .OrderBy(section => section.Order)
                .ToList();

            Assert.Equal(2, sections.Count);
            Assert.Equal(HomeSectionType.Resume, sections[0].Type);
            Assert.Equal(HomeSectionType.NextUp, sections[1].Type);
        }
    }

    [Fact]
    public async Task Perform_NoDuplicateRows_LeavesTheStoredSectionsAlone()
    {
        int preferencesId;
        using (var context = CreateDbContext())
        {
            context.Users.Add(new User("user", "auth-provider", "reset-provider") { Id = _userId });

            var preferences = new DisplayPreferences(_userId, _itemId, "client");
            context.DisplayPreferences.Add(preferences);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            preferencesId = preferences.Id;

            context.Set<HomeSection>().Add(new HomeSection
            {
                DisplayPreferencesId = preferencesId,
                Order = 0,
                Type = HomeSectionType.SmallLibraryTiles
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using (var context = CreateDbContext())
        {
            var section = Assert.Single(context.Set<HomeSection>());
            Assert.Equal(preferencesId, section.DisplayPreferencesId);
            Assert.Equal(0, section.Order);
            Assert.Equal(HomeSectionType.SmallLibraryTiles, section.Type);
        }
    }

    private JellyfinDbContext CreateDbContext() => new(
        _dbOptions,
        NullLogger<JellyfinDbContext>.Instance,
        new SqliteDatabaseProvider(new Mock<IApplicationPaths>().Object, NullLogger<SqliteDatabaseProvider>.Instance),
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

    private DeduplicateHomeSections CreateMigration()
    {
        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(CreateDbContext);

        return new DeduplicateHomeSections(
            new StartupLogger<DeduplicateHomeSections>(NullLogger<DeduplicateHomeSections>.Instance),
            factory.Object);
    }
}
