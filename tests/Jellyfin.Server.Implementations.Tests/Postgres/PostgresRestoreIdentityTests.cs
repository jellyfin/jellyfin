using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.FullSystemBackup;
using Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.SystemBackupService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

/// <summary>
/// A restore inserts rows with the ids they had in the backup, which does not advance the sequence behind an
/// identity column. Without a reset the next row saved after a restore is given an id that is already taken.
/// </summary>
public sealed class PostgresRestoreIdentityTests : DatabaseTestFixture
{
    private readonly string _testRoot = Path.Combine(AppContext.BaseDirectory, "jellyfin-postgres-restore-" + Guid.NewGuid().ToString("N"));

    protected override void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, true);
        }

        base.Dispose(disposing);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task RestoreBackup_IdsOfTheBackupHaveGaps_NextRowsGetTheIdAfterTheHighestOne()
    {
        var archive = await CreateSqliteArchiveAsync(seed: true);

        await CreateRestoreService().RestoreBackupAsync(archive);

        await using var context = CreateDbContext();
        Assert.Equal([2, 3, 4], await context.Permissions.OrderBy(e => e.Id).Select(e => e.Id).ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal([2], await context.AccessSchedules.Select(e => e.Id).ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal([2], await context.Devices.Select(e => e.Id).ToListAsync(TestContext.Current.CancellationToken));

        var userId = await context.Users.Select(e => e.Id).FirstAsync(TestContext.Current.CancellationToken);
        var permission = new Permission(PermissionKind.EnableLiveTvManagement, true) { UserId = userId };
        var schedule = new AccessSchedule(DynamicDayOfWeek.Weekend, 9, 10, userId);
        var device = new Device(userId, "App", "2.0", "Tablet", "device-3");
        context.Permissions.Add(permission);
        context.AccessSchedules.Add(schedule);
        context.Devices.Add(device);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(5, permission.Id);
        Assert.Equal(3, schedule.Id);
        Assert.Equal(3, device.Id);
    }

    /// <summary>
    /// The purge's TRUNCATE ... RESTART IDENTITY already starts the sequences again at 1, so this passes without
    /// the reset. It guards against setting an empty table's sequence to the wrong value, not against ids that
    /// collide with the restored rows.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [ProviderFact(TestDbProvider.Postgres)]
    public async Task RestoreBackup_BackupHoldsNoRows_IdsStartAgainAtOne()
    {
        var archive = await CreateSqliteArchiveAsync(seed: false);
        var userId = Guid.NewGuid();
        await using (var context = CreateDbContext())
        {
            context.Users.Add(new User("existing", "provider", "reset") { Id = userId });
            context.Permissions.Add(new Permission(PermissionKind.IsAdministrator, true) { UserId = userId });
            context.Permissions.Add(new Permission(PermissionKind.IsHidden, true) { UserId = userId });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await CreateRestoreService().RestoreBackupAsync(archive);

        await using (var context = CreateDbContext())
        {
            Assert.Empty(await context.Permissions.ToListAsync(TestContext.Current.CancellationToken));

            var user = new User("restored", "provider", "reset");
            var permission = new Permission(PermissionKind.IsAdministrator, true);
            user.Permissions.Add(permission);
            context.Users.Add(user);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Assert.Equal(1, permission.Id);
        }
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task RestoreBackup_TableOutsideTheModel_KeepsItsSequence()
    {
        var archive = await CreateSqliteArchiveAsync(seed: true);
        await using (var context = CreateDbContext())
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE "Unrelated" ("Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY);
                INSERT INTO "Unrelated" VALUES (DEFAULT), (DEFAULT), (DEFAULT);
                DELETE FROM "Unrelated";
                """,
                TestContext.Current.CancellationToken);
        }

        await CreateRestoreService().RestoreBackupAsync(archive);

        await using (var context = CreateDbContext())
        {
            var next = await context.Database
                .SqlQueryRaw<int>("""INSERT INTO "Unrelated" VALUES (DEFAULT) RETURNING "Id" AS "Value" """)
                .ToListAsync(TestContext.Current.CancellationToken);

            Assert.Equal([4], next);
        }
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task IdentityColumns_AreGeneratedByDefault_SoARestoreCanInsertTheirIds()
    {
        await using var context = CreateDbContext();

        var generation = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT identity_generation AS "Value" FROM information_schema.columns
                WHERE table_schema = 'public' AND is_identity = 'YES' AND table_name IN ('Permissions', 'AccessSchedules', 'Devices')
                """)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["BY DEFAULT", "BY DEFAULT", "BY DEFAULT"], generation);
    }

    private BackupService CreateRestoreService()
        => CreateBackupService(CreateDbContextFactory(), DatabaseProvider);

    private async Task<string> CreateSqliteArchiveAsync(bool seed)
    {
#pragma warning disable RS0030 // Do not use banned APIs: The archive to restore has to come from SQLite whichever provider the tests run on.
        using var sqlite = SqliteTestBackend.Instance.Create(TestSchemaSource.Migrations, []);
#pragma warning restore RS0030
        var sqliteProvider = sqlite.CreateProvider();
        JellyfinDbContext Create() => new(sqlite.Options, NullLogger<JellyfinDbContext>.Instance, sqliteProvider, new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

        if (seed)
        {
            await using var context = Create();
            await SeedAsync(context);
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(() => Create());
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => Create());
        var provider = new Mock<IJellyfinDatabaseProvider>();
        provider.Setup(p => p.RunScheduledOptimisation(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var manifest = await CreateBackupService(factory.Object, provider.Object).CreateBackupAsync(new BackupOptionsDto());
        return manifest.Path;
    }

    private static async Task SeedAsync(JellyfinDbContext context)
    {
        var alice = new User("alice", "provider", "reset");
        alice.Permissions.Add(new Permission(PermissionKind.IsAdministrator, true));
        alice.Permissions.Add(new Permission(PermissionKind.EnableAllFolders, false));
        alice.Permissions.Add(new Permission(PermissionKind.EnableAllDevices, true));
        var bob = new User("bob", "provider", "reset");
        bob.Permissions.Add(new Permission(PermissionKind.IsAdministrator, false));
        context.Users.AddRange(alice, bob);
        context.AccessSchedules.Add(new AccessSchedule(DynamicDayOfWeek.Weekday, 8, 17, alice.Id));
        context.AccessSchedules.Add(new AccessSchedule(DynamicDayOfWeek.Weekend, 9, 12, alice.Id));
        context.Devices.Add(new Device(alice.Id, "App", "1.0", "Phone", "device-1"));
        context.Devices.Add(new Device(alice.Id, "App", "1.0", "Tablet", "device-2"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // The first row of each table goes again, so the highest id is not the number of rows.
        await context.Permissions.Where(e => e.Id == 1).ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context.AccessSchedules.Where(e => e.Id == 1).ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context.Devices.Where(e => e.Id == 1).ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    private BackupService CreateBackupService(IDbContextFactory<JellyfinDbContext> factory, IJellyfinDatabaseProvider provider)
    {
        var applicationHost = new Mock<IServerApplicationHost>();
        applicationHost.Setup(a => a.ApplicationVersion).Returns(new Version(10, 11, 0));

        var applicationPaths = new Mock<IServerApplicationPaths>();
        applicationPaths.Setup(a => a.BackupPath).Returns(Path.Combine(_testRoot, "Backup"));
        applicationPaths.Setup(a => a.ConfigurationDirectoryPath).Returns(Path.Combine(_testRoot, "Config"));
        applicationPaths.Setup(a => a.DataPath).Returns(Path.Combine(_testRoot, "Data"));
        applicationPaths.Setup(a => a.CachePath).Returns(Path.Combine(_testRoot, "Cache"));
        applicationPaths.Setup(a => a.ProgramDataPath).Returns(Path.Combine(_testRoot, "ProgramData"));
        applicationPaths.Setup(a => a.RootFolderPath).Returns(Path.Combine(_testRoot, "Root"));
        applicationPaths.Setup(a => a.InternalMetadataPath).Returns(Path.Combine(_testRoot, "Metadata"));
        applicationPaths.Setup(a => a.DefaultInternalMetadataPath).Returns(Path.Combine(_testRoot, "MetadataDefault"));
        Directory.CreateDirectory(Path.Combine(_testRoot, "Backup"));
        Directory.CreateDirectory(Path.Combine(_testRoot, "Config"));

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.IsScanRunning).Returns(false);

        var routineCatalog = new Mock<IMigrationRoutineCatalog>();
        routineCatalog.Setup(c => c.RoutineIds).Returns(new HashSet<string>(StringComparer.Ordinal));

        return new BackupService(
            new CollectingLogger<BackupService>(),
            factory,
            applicationHost.Object,
            applicationPaths.Object,
            provider,
            new Mock<IHostApplicationLifetime>().Object,
            libraryManager.Object,
            routineCatalog.Object);
    }
}
