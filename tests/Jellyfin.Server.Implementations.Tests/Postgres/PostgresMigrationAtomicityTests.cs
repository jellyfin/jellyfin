using System;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

/// <summary>
/// Runs migrations through the options the provider configures, to pin what happens when one of them fails. Most tests
/// use migrations that live in the tests, as they pin how the provider configures EF Core and not what a particular
/// migration does. One of them runs the real baseline.
/// </summary>
public sealed class PostgresMigrationAtomicityTests : IDisposable
{
    private readonly TestDatabase _database = PostgresTestBackend.Instance.CreateEmpty();

    /// <summary>
    /// Pins how the provider configures EF Core, which runs every migration in a transaction of its own, using a test
    /// migration that fails after it created a table.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [ProviderFact(TestDbProvider.Postgres)]
    public async Task Migrate_FailingMigration_RollsBackAllItsStatements()
    {
        await using var context = new FailingContext(CreateOptions<FailingContext>());

        await Assert.ThrowsAsync<PostgresException>(() => context.Database.MigrateAsync(TestContext.Current.CancellationToken));

        Assert.True(await TableExists(context, "Kept"));
        Assert.False(await TableExists(context, "Lost"));
    }

    /// <summary>
    /// Pins how the provider configures EF Core, which commits a migration before it starts the next one, using test
    /// migrations of which the second fails.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [ProviderFact(TestDbProvider.Postgres)]
    public async Task Migrate_FailingMigration_KeepsTheMigrationsBeforeIt()
    {
        await using var context = new FailingContext(CreateOptions<FailingContext>());

        await Assert.ThrowsAsync<PostgresException>(() => context.Database.MigrateAsync(TestContext.Current.CancellationToken));

        Assert.Equal(
            ["20200101000001_CreateKeptTable"],
            await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Pins how the provider configures EF Core, which throws for an operation that cannot run in a transaction
    /// (<c>NonTransactionalMigrationOperationWarning</c>), using a test migration with such an operation.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [ProviderFact(TestDbProvider.Postgres)]
    public async Task Migrate_MigrationOutsideATransaction_IsRefused()
    {
        await using var context = new NonTransactionalContext(CreateOptions<NonTransactionalContext>());

        var exception = await Assert.ThrowsAnyAsync<Exception>(() => context.Database.MigrateAsync(TestContext.Current.CancellationToken));

        Assert.Contains("cannot be executed in a transaction", exception.Message, StringComparison.Ordinal);
        Assert.Empty(await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken));
        Assert.False(await TableExists(context, "Indexed"));
    }

    /// <summary>
    /// Runs the real baseline and makes its last statements fail, after it created every table. A function named like
    /// the one that the baseline creates for the <c>min</c> aggregate is in the way.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [ProviderFact(TestDbProvider.Postgres)]
    public async Task Migrate_BaselineFailingAfterCreatingTheTables_LeavesNoTableAndNoHistoryRow()
    {
        await using var context = new JellyfinDbContext(
            _database.Options,
            NullLogger<JellyfinDbContext>.Instance,
            _database.CreateProvider(),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
        await context.Database.ExecuteSqlRawAsync(
            "CREATE FUNCTION jf_uuid_smaller(a uuid, b uuid) RETURNS uuid LANGUAGE sql AS 'SELECT a'",
            TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<PostgresException>(() => context.Database.MigrateAsync(TestContext.Current.CancellationToken));

        Assert.Equal(PostgresErrorCodes.DuplicateFunction, exception.SqlState);
        Assert.False(await TableExists(context, "BaseItems"));
        Assert.Empty(await context.Database
            .SqlQueryRaw<string>("""SELECT relname AS "Value" FROM pg_class WHERE relnamespace = current_schema()::regnamespace AND relkind = 'r' AND relname <> '__EFMigrationsHistory'""")
            .ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken));
        Assert.Equal(
            1,
            await context.Database
                .SqlQueryRaw<int>("""SELECT count(*)::int AS "Value" FROM pg_proc WHERE proname = 'jf_uuid_smaller'""")
                .SingleAsync(TestContext.Current.CancellationToken));
    }

    public void Dispose()
    {
        _database.Dispose();
    }

    private static async Task<bool> TableExists(DbContext context, string name)
    {
        var quoted = $"\"{name}\"";
        return await context.Database
            .SqlQuery<bool>($"SELECT to_regclass({quoted}) IS NOT NULL AS \"Value\"")
            .SingleAsync(TestContext.Current.CancellationToken);
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
                DatabaseType = "Jellyfin-PostgreSQL",
                CustomProviderOptions = new CustomDatabaseOptions
                {
                    PluginName = string.Empty,
                    PluginAssembly = string.Empty,
                    ConnectionString = connectionString
                }
            });

        // The provider looks for migrations in its own assembly, these live in the tests.
        builder.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(PostgresMigrationAtomicityTests).Assembly));
        return builder.Options;
    }

    private sealed class FailingContext : DbContext
    {
        public FailingContext(DbContextOptions<FailingContext> options)
            : base(options)
        {
        }
    }

    private sealed class NonTransactionalContext : DbContext
    {
        public NonTransactionalContext(DbContextOptions<NonTransactionalContext> options)
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
            migrationBuilder.Sql("""CREATE TABLE "Kept" ("Id" integer PRIMARY KEY)""");
        }
    }

    [DbContext(typeof(FailingContext))]
    [Migration("20200101000002_CreateLostTableThenFail")]
    private sealed class CreateLostTableThenFail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""CREATE TABLE "Lost" ("Id" integer PRIMARY KEY)""");
            migrationBuilder.Sql("SELECT 1 / 0");
        }
    }

    [DbContext(typeof(NonTransactionalContext))]
    [Migration("20200101000003_CreateIndexConcurrently")]
    private sealed class CreateIndexConcurrently : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""CREATE TABLE "Indexed" ("Id" integer PRIMARY KEY)""");
            migrationBuilder.Sql("""CREATE INDEX CONCURRENTLY "IX_Indexed_Id" ON "Indexed" ("Id")""", suppressTransaction: true);
        }
    }
}
