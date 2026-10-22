using System;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.SqlServer;
using Jellyfin.Database.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SqlServer;

/// <summary>
/// Runs migrations through the options the provider configures, to pin what happens when one of them fails.
/// Because SQL Server supports transactional DDL, a failing migration rolls back completely.
/// </summary>
public sealed class SqlServerMigrationAtomicityTests : IDisposable
{
    private readonly TestDatabase _database = SqlServerTestBackend.Instance.CreateEmpty();

    [ProviderFact(TestDbProvider.SqlServer)]
    public async Task Migrate_FailingMigration_RollsBackAllItsStatements()
    {
        await using var context = new FailingContext(CreateOptions<FailingContext>());

        await Assert.ThrowsAsync<SqlException>(() => context.Database.MigrateAsync(TestContext.Current.CancellationToken));

        Assert.True(await TableExists(context, "Kept"));
        Assert.False(await TableExists(context, "Lost"));
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public async Task Migrate_FailingMigration_KeepsTheMigrationsBeforeIt()
    {
        await using var context = new FailingContext(CreateOptions<FailingContext>());

        await Assert.ThrowsAsync<SqlException>(() => context.Database.MigrateAsync(TestContext.Current.CancellationToken));

        Assert.True(await TableExists(context, "Kept"));
        Assert.False(await TableExists(context, "Lost"));
        Assert.Equal(
            ["20200101000001_CreateKeptTable"],
            await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken));
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public async Task Migrate_BaselineFailingAfterCreatingTheTables_LeavesNoTableAndNoHistoryRow()
    {
        await using var context = new JellyfinDbContext(
            _database.Options,
            NullLogger<JellyfinDbContext>.Instance,
            _database.CreateProvider(),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

        await context.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER tr_fail_users ON DATABASE FOR CREATE_TABLE AS BEGIN IF EVENTDATA().value('(/EVENT_INSTANCE/ObjectName)[1]', 'nvarchar(max)') = 'Users' THROW 50000, 'Refusing Users table', 1; END",
            TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<SqlException>(() => context.Database.MigrateAsync(TestContext.Current.CancellationToken));

        Assert.Contains("Refusing Users table", exception.Message, StringComparison.Ordinal);
        Assert.False(await TableExists(context, "BaseItems"));
        Assert.Empty(await context.Database
            .SqlQueryRaw<string>("SELECT name AS [Value] FROM sys.tables WHERE is_ms_shipped = 0 AND name <> '__EFMigrationsHistory'")
            .ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken));
    }

    public void Dispose()
    {
        _database.Dispose();
    }

    private static async Task<bool> TableExists(DbContext context, string name)
    {
        return await context.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.tables WHERE is_ms_shipped = 0 AND name = {name}")
            .SingleAsync(TestContext.Current.CancellationToken) > 0;
    }

    private DbContextOptions<TContext> CreateOptions<TContext>()
        where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>();
        var connectionString = RelationalOptionsExtension.Extract(_database.Options).ConnectionString!;
        _database.CreateProvider().Initialise(
            builder,
            new DatabaseConfigurationOptions
            {
                DatabaseType = "Jellyfin-SqlServer",
                CustomProviderOptions = new CustomDatabaseOptions
                {
                    PluginName = string.Empty,
                    PluginAssembly = string.Empty,
                    ConnectionString = connectionString
                }
            });

        builder.UseSqlServer(connectionString, sqlServer => sqlServer.MigrationsAssembly(typeof(SqlServerMigrationAtomicityTests).Assembly));
        return builder.Options;
    }

    private sealed class FailingContext : DbContext
    {
        public FailingContext(DbContextOptions<FailingContext> options)
            : base(options)
        {
        }
    }

    [DbContext(typeof(FailingContext))]
    [Migration("20200101000001_CreateKeptTable")]
    private sealed class CreateKeptTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE TABLE [Kept] ([Id] int PRIMARY KEY)");
        }
    }

    [DbContext(typeof(FailingContext))]
    [Migration("20200101000002_CreateLostTableThenFail")]
    private sealed class CreateLostTableThenFail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE TABLE [Lost] ([Id] int PRIMARY KEY)");
            migrationBuilder.Sql("SELECT 1 / 0");
        }
    }
}
