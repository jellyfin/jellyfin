using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

public sealed class PostgresDatabaseProviderTests : DatabaseTestFixture
{
    private readonly CollectingLogger<PostgresDatabaseProvider> _logger = new();
    private readonly CommandRecorder _commands;

    public PostgresDatabaseProviderTests()
        : this(new CommandRecorder())
    {
    }

    private PostgresDatabaseProviderTests(CommandRecorder commands)
        : base(commands)
    {
        _commands = commands;
    }

    [ProviderTheory(TestDbProvider.Postgres)]
    [InlineData("ActivityLogs")]
    [InlineData("public.ActivityLogs")]
    public async Task PurgeDatabase_ClearsTheTableAndResetsIdentity(string tableName)
    {
        AddActivityLogs(2);

        await using (var context = CreateDbContext())
        {
            await CreateProvider().PurgeDatabase(context, [tableName]);
        }

        AddActivityLogs(1);
        await using var verification = CreateDbContext();
        Assert.Equal(1, await verification.ActivityLogs.CountAsync());
        Assert.Equal(1, await verification.ActivityLogs.Select(e => e.Id).SingleAsync());
    }

    [ProviderTheory(TestDbProvider.Postgres)]
    [InlineData("we\"ird")]
    [InlineData("public.we\"ird")]
    public async Task PurgeDatabase_IdentifierWithQuote_IsEscaped(string tableName)
    {
        await using var context = CreateDbContext();
        await context.Database.ExecuteSqlRawAsync("""CREATE TABLE "we""ird" ("Id" serial PRIMARY KEY)""");
        await context.Database.ExecuteSqlRawAsync("""INSERT INTO "we""ird" DEFAULT VALUES""");

        await CreateProvider().PurgeDatabase(context, [tableName]);

        Assert.Equal(0, await context.Database.SqlQueryRaw<long>("""SELECT count(*) AS "Value" FROM "we""ird" """).SingleAsync());
        await context.Database.ExecuteSqlRawAsync("""INSERT INTO "we""ird" DEFAULT VALUES""");
        Assert.Equal(1, await context.Database.SqlQueryRaw<int>("""SELECT "Id" AS "Value" FROM "we""ird" """).SingleAsync());
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task PurgeDatabase_NoTables_DoesNothing()
    {
        AddActivityLogs(1);

        await using var context = CreateDbContext();
        await CreateProvider().PurgeDatabase(context, []);

        Assert.Equal(1, await context.ActivityLogs.CountAsync());
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task RunScheduledOptimisation_EmptyLibrary_SkipsTheAnalyze()
    {
        await CreateProvider().RunScheduledOptimisation(CancellationToken.None);

        Assert.Single(_logger.Entries, e => e.Message.Contains("Not analyzing the database", StringComparison.Ordinal));
        Assert.DoesNotContain(_commands.Texts, IsAnalyze);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task RunScheduledOptimisation_LibraryHasItems_AnalyzesTheTables()
    {
        AddLibraryItem();
        Assert.False(await IsAnalyzed());

        await CreateProvider().RunScheduledOptimisation(CancellationToken.None);

        Assert.True(await WaitForAnalyze());
        Assert.Contains(_commands.Texts, IsAnalyze);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task RefreshStatistics_EmptyLibrary_DoesNotAnalyze()
    {
        await CreateProvider().RefreshStatistics(CancellationToken.None);

        Assert.Empty(_logger.Entries);
        Assert.DoesNotContain(_commands.Texts, IsAnalyze);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task RefreshStatistics_LibraryHasItems_AnalyzesTheTables()
    {
        AddLibraryItem();
        Assert.False(await IsAnalyzed());

        await CreateProvider().RefreshStatistics(CancellationToken.None);

        Assert.True(await WaitForAnalyze());
        Assert.Contains(_commands.Texts, IsAnalyze);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task MigrationBackupFast_ReturnsAnEmptyKey()
    {
        var key = await CreateProvider().MigrationBackupFast(CancellationToken.None);

        Assert.Equal(string.Empty, key);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task MigrationBackupFast_NothingApplied_DoesNotWarn()
    {
        DropMigrationHistory();

        await CreateProvider().MigrationBackupFast(CancellationToken.None);

        Assert.DoesNotContain(_logger.Entries, e => e.Level == LogLevel.Warning);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task MigrationBackupFast_MigrationsApplied_WarnsAboutPgDump()
    {
        SetAppliedMigrations("20200101000000_InitialCreate");

        await CreateProvider().MigrationBackupFast(CancellationToken.None);

        var warning = Assert.Single(_logger.Entries, e => e.Level == LogLevel.Warning);
        Assert.Contains("pg_dump", warning.Message, StringComparison.Ordinal);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task MigrationBackupFast_OnlyCodeRoutinesRecorded_DoesNotWarn()
    {
        // A fresh install records the routines that have nothing to do before the schema migrations have run.
        SetAppliedMigrations("20250420050000_DisableTranscodingThrottling");

        await CreateProvider().MigrationBackupFast(CancellationToken.None);

        Assert.DoesNotContain(_logger.Entries, e => e.Level == LogLevel.Warning);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task RestoreBackupFast_EmptyKey_LogsCriticalWithTheLastAppliedMigration()
    {
        SetAppliedMigrations("20200101000000_InitialCreate", "20200102000000_Later");

        await CreateProvider().RestoreBackupFast(string.Empty, CancellationToken.None);

        var critical = Assert.Single(_logger.Entries, e => e.Level == LogLevel.Critical);
        Assert.Contains("20200102000000_Later", critical.Message, StringComparison.Ordinal);
        Assert.Contains("pg_dump", critical.Message, StringComparison.Ordinal);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task RestoreBackupFast_UnknownKey_LogsCritical()
    {
        await CreateProvider().RestoreBackupFast("unknown", CancellationToken.None);

        var critical = Assert.Single(_logger.Entries, e => e.Level == LogLevel.Critical);
        Assert.Contains("does not exist: unknown", critical.Message, StringComparison.Ordinal);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task DeleteBackup_EmptyKey_DoesNothing()
    {
        await CreateProvider().DeleteBackup(string.Empty);

        Assert.Empty(_logger.Entries);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task RunShutdownTask_DoesNotThrow()
    {
        await CreateProvider().RunShutdownTask(CancellationToken.None);
    }

    private static bool IsAnalyze(string commandText) => commandText.StartsWith("ANALYZE", StringComparison.Ordinal);

    private PostgresDatabaseProvider CreateProvider() => new(_logger) { DbContextFactory = CreateDbContextFactory() };

    private void AddActivityLogs(int count)
    {
        using var context = CreateDbContext();
        for (var i = 0; i < count; i++)
        {
            context.ActivityLogs.Add(new ActivityLog("test", "test", Guid.NewGuid()));
        }

        context.SaveChanges();
    }

    private void AddLibraryItem()
    {
        using var context = CreateDbContext();
        context.BaseItems.Add(new BaseItemEntity { Id = Guid.NewGuid(), Type = "Test", IsFolder = false });
        context.SaveChanges();
    }

    private async Task<bool> IsAnalyzed()
    {
        await using var context = CreateDbContext();
        return await context.Database
            .SqlQueryRaw<bool>("""SELECT last_analyze IS NOT NULL AS "Value" FROM pg_stat_user_tables WHERE relname = 'BaseItems'""")
            .SingleAsync();
    }

    // Statistics are flushed to the shared views some time after the command that produced them.
    private async Task<bool> WaitForAnalyze()
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (await IsAnalyzed())
            {
                return true;
            }

            await Task.Delay(200);
        }

        return false;
    }

    private void DropMigrationHistory()
    {
        using var context = CreateDbContext();
        context.Database.ExecuteSqlRaw("""DROP TABLE "__EFMigrationsHistory";""");
    }

    private void SetAppliedMigrations(params string[] migrationIds)
    {
        using var context = CreateDbContext();
        context.Database.ExecuteSqlRaw("""DELETE FROM "__EFMigrationsHistory";""");
        foreach (var migrationId in migrationIds)
        {
            context.Database.ExecuteSql($"""INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ({migrationId}, '10.0.0')""");
        }
    }

    private sealed class CommandRecorder : DbCommandInterceptor
    {
        public List<string> Texts { get; } = [];

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Texts.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}
