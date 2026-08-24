using System;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Database.Providers.Sqlite.Migrations;
using Jellyfin.Server.Implementations.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.EfMigrations;

public class EfMigrationTests
{
    [Fact]
    public void CheckForUnappliedMigrations_SqLite()
    {
        var dbDesignContext = new SqliteDesignTimeJellyfinDbFactory();
        var context = dbDesignContext.CreateDbContext([]);
        Assert.False(context.Database.HasPendingModelChanges(), "There are unapplied changes to the EFCore model for SQLite. Please create a Migration.");
    }

    [Fact]
    public async Task Migrate_AppliesAnOutOfOrderMigrationWithoutRevertingNewerOnes()
    {
        // Merging branches whose migration timestamps interleave leaves a pending migration that sorts before an applied
        // one. IMigrator.MigrateAsync takes the state to end up in, so targeting that migration directly would revert
        // everything newer - which drops data and, on SQLite, usually cannot even be generated. JellyfinMigrationService
        // therefore caps the target at the newest applied migration; this pins that this applies the straggler and
        // leaves the newer migrations alone.
        const string BeforeStraggler = "20260728182152_AddPeopleItemMapCoveringIndex";
        const string Straggler = "20260812050902_AddMediaStreamFilterIndex";
        const string AfterStraggler = "20260815063607_RemoveOrphanedUserPermissionsAndPreferences";
        const string StragglerIndex = "IX_MediaStreamInfos_StreamType_ItemId_Language_IsExternal";

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var options = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(connection, f => f.MigrationsAssembly(typeof(SqliteDesignTimeJellyfinDbFactory).Assembly))
            .Options;

        using var context = new JellyfinDbContext(
            options,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

        var migrator = context.GetService<IMigrator>();

        // Recreate a database that only learned about the straggler after the newer migrations had been applied: apply
        // everything before it, claim it is applied so its Up is skipped, move on, then forget it again.
        await migrator.MigrateAsync(BeforeStraggler, TestContext.Current.CancellationToken);
        SetApplied(connection, Straggler, true);
        await migrator.MigrateAsync(AfterStraggler, TestContext.Current.CancellationToken);
        SetApplied(connection, Straggler, false);
        Assert.False(IndexExists(connection, StragglerIndex));

        await migrator.MigrateAsync(AfterStraggler, TestContext.Current.CancellationToken);

        Assert.True(IndexExists(connection, StragglerIndex));
        Assert.True(IsApplied(connection, Straggler));
        Assert.True(IsApplied(connection, AfterStraggler));
    }

    private static void SetApplied(SqliteConnection connection, string migrationId, bool applied)
    {
        using var command = connection.CreateCommand();
        if (applied)
        {
            command.CommandText = "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ($id, '0.0.0')";
        }
        else
        {
            command.CommandText = "DELETE FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = $id";
        }

        command.Parameters.AddWithValue("$id", migrationId);
        command.ExecuteNonQuery();
    }

    private static bool IsApplied(SqliteConnection connection, string migrationId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = $id";
        command.Parameters.AddWithValue("$id", migrationId);

        return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) == 1;
    }

    private static bool IndexExists(SqliteConnection connection, string name)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM sqlite_master WHERE type = 'index' AND name = $name";
        command.Parameters.AddWithValue("$name", name);

        return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) == 1;
    }
}
