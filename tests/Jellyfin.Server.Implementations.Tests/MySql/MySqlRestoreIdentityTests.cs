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
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.SystemBackupService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

/// <summary>
/// A restore deletes the rows of every table and inserts rows with the ids they had in the backup. The restore holds a
/// transaction, and changing the counter behind an auto-increment column commits the transaction it runs in, so the
/// counters are not reset during a restore. What must hold is that the next row saved after a restore never gets an id
/// that a restored row has, and because the restore leaves the counters where they were, an id above every id the target
/// handed out before.
/// </summary>
public sealed class MySqlRestoreIdentityTests : DatabaseTestFixture
{
    private readonly string _testRoot = Path.Combine(AppContext.BaseDirectory, "jellyfin-mysql-restore-" + Guid.NewGuid().ToString("N"));

    protected override void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, true);
        }

        base.Dispose(disposing);
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public async Task RestoreBackup_IdsOfTheBackupHaveGaps_NextRowsGetAnIdAfterTheHighestOne()
    {
        var archive = await CreateSqliteArchiveAsync();

        await CreateRestoreService().RestoreBackupAsync(archive);

        await AssertRestoredRowsAndFreeIdsAsync(0, 0);
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public async Task RestoreBackup_CountersOfTheTargetAreFurtherThanTheBackup_NextRowsGetIdsAfterTheCountersOfTheTarget()
    {
        var archive = await CreateSqliteArchiveAsync();
        var (highestPermissionId, highestScheduleId) = await RunTheCountersOfTheTargetPastTheArchiveAsync();

        await CreateRestoreService().RestoreBackupAsync(archive);

        // The restore does not reset the counters, and the ids it inserts are below them, so they stay where they were.
        await AssertRestoredRowsAndFreeIdsAsync(highestPermissionId, highestScheduleId);
    }

    /// <summary>
    /// A purge outside of a transaction resets the counters of the tables it deletes from, and only of those.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public async Task PurgeDatabase_OutsideATransaction_StartsTheCountersOfThePurgedTablesAgainAtOne()
    {
        await using var context = CreateDbContext();
        try
        {
            await RunTheCountersOfTheTargetPastTheArchiveAsync();
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TABLE `Unrelated` (`Id` int NOT NULL AUTO_INCREMENT PRIMARY KEY)",
                TestContext.Current.CancellationToken);
            await context.Database.ExecuteSqlRawAsync("INSERT INTO `Unrelated` VALUES (), (), ()", TestContext.Current.CancellationToken);
            await context.Database.ExecuteSqlRawAsync("DELETE FROM `Unrelated`", TestContext.Current.CancellationToken);
            var tables = context.Model.GetEntityTypes().Select(e => e.GetTableName()!).Distinct().ToList();

            await DatabaseProvider.PurgeDatabase(context, tables);

            var user = new User("after the purge", "provider", "reset");
            var permission = new Permission(PermissionKind.IsAdministrator, true);
            user.Permissions.Add(permission);
            context.Users.Add(user);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1, permission.Id);

            // LAST_INSERT_ID belongs to a connection, so the insert and the read share the one of this context.
            await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
            await context.Database.ExecuteSqlRawAsync("INSERT INTO `Unrelated` VALUES ()", TestContext.Current.CancellationToken);
            var next = await context.Database
                .SqlQueryRaw<long>("SELECT CAST(LAST_INSERT_ID() AS SIGNED) AS `Value`")
                .ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal([4L], next);
        }
        finally
        {
            await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS `Unrelated`", CancellationToken.None);
        }
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public async Task IdentityColumns_AreAutoIncrement_SoARestoreCanInsertTheirIds()
    {
        await using var context = CreateDbContext();

        var columns = await context.Database
            .SqlQueryRaw<string>(
                "SELECT CONCAT(CAST(TABLE_NAME AS CHAR), '.', CAST(COLUMN_NAME AS CHAR)) AS `Value` FROM information_schema.COLUMNS "
                + "WHERE TABLE_SCHEMA = DATABASE() AND EXTRA LIKE '%auto_increment%' AND TABLE_NAME IN ('Permissions', 'AccessSchedules', 'Devices') "
                + "ORDER BY TABLE_NAME")
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["AccessSchedules.Id", "Devices.Id", "Permissions.Id"], columns);
    }

    private async Task AssertRestoredRowsAndFreeIdsAsync(int highestPermissionIdOfTheTarget, int highestScheduleIdOfTheTarget)
    {
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

        Assert.True(permission.Id > Math.Max(4, highestPermissionIdOfTheTarget), $"The new permission got the id {permission.Id}");
        Assert.True(schedule.Id > Math.Max(2, highestScheduleIdOfTheTarget), $"The new access schedule got the id {schedule.Id}");
        Assert.True(device.Id > 2, $"The new device got the id {device.Id}");
        Assert.Equal(4, await context.Permissions.CountAsync(TestContext.Current.CancellationToken));
    }

    private BackupService CreateRestoreService()
        => CreateBackupService(CreateDbContextFactory(), DatabaseProvider);

    /// <summary>
    /// Saves and deletes rows of the target, so that the counter of each table is further than the ids that the archive holds.
    /// </summary>
    /// <returns>The highest id that a permission and an access schedule of the target had.</returns>
    private async Task<(int PermissionId, int ScheduleId)> RunTheCountersOfTheTargetPastTheArchiveAsync()
    {
        await using var context = CreateDbContext();
        var user = new User("existing", "provider", "reset");
        for (var i = 0; i < 10; i++)
        {
            user.Permissions.Add(new Permission((PermissionKind)i, true));
            user.AccessSchedules.Add(new AccessSchedule(DynamicDayOfWeek.Weekday, i, i + 1, user.Id));
        }

        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await context.Permissions.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context.AccessSchedules.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        return (user.Permissions.Max(e => e.Id), user.AccessSchedules.Max(e => e.Id));
    }

    private async Task<string> CreateSqliteArchiveAsync()
    {
#pragma warning disable RS0030 // Do not use banned APIs: The archive to restore has to come from SQLite whichever provider the tests run on.
        using var sqlite = SqliteTestBackend.Instance.Create(TestSchemaSource.Migrations, []);
#pragma warning restore RS0030
        var sqliteProvider = sqlite.CreateProvider();
        JellyfinDbContext Create() => new(sqlite.Options, NullLogger<JellyfinDbContext>.Instance, sqliteProvider, new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

        await using (var context = Create())
        {
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
            NullLogger<BackupService>.Instance,
            factory,
            applicationHost.Object,
            applicationPaths.Object,
            provider,
            new Mock<IHostApplicationLifetime>().Object,
            libraryManager.Object,
            routineCatalog.Object);
    }
}
