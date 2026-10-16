using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Providers.MySql;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Database.Providers.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using Xunit;

namespace Jellyfin.Database.Testing;

/// <summary>
/// What the tests need to know about an engine, one row for each engine, so that a test asks the table instead of
/// switching on the engine itself, and a new engine is described in one place.
/// </summary>
public sealed class EngineTraits
{
    // The extended result code of SQLite for a violated unique constraint.
    private const int SqliteConstraintUnique = 2067;

    // The numbers of SQL Server for a duplicate in a unique index, and in a unique constraint or primary key.
    private const int SqlServerDuplicateInIndex = 2601;
    private const int SqlServerDuplicateKey = 2627;

    // The filters of the indexes of the SQLite model, as the model configuration writes them.
    private const string OwnedFilter = "\"OwnerId\" IS NOT NULL";
    private const string ExtraTypeFilter = "\"ExtraType\" IS NOT NULL";
    private const string AlternateVersionFilter = "\"PrimaryVersionId\" IS NOT NULL";
    private const string PrimaryVersionFilter = "\"PrimaryVersionId\" IS NULL AND (\"OwnerId\" IS NULL OR \"ExtraType\" IS NOT NULL)";

    private static readonly Dictionary<TestDbProvider, EngineTraits> _table = new[]
    {
        new EngineTraits
        {
            Provider = TestDbProvider.Sqlite,
            ProviderType = typeof(SqliteDatabaseProvider),
            IsEmbedded = true,
            IsRegistered = true,
            HasSchemaMigrations = true,
            BoundsIndexedStrings = false,
            UnexpressibleIndexFilters = [],
            NoActionCascades = [],
            NullFilteredUniqueIndexes = [],
            PurgeResetsIdentityInATransaction = true,
            AssertUniqueViolation = ex => Assert.Equal(SqliteConstraintUnique, Assert.IsType<SqliteException>(ex).SqliteExtendedErrorCode),
            Pool = new PoolStatements
            {
                ListTables = "SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'",
                CountRows = "SELECT COUNT(*) AS Value FROM {0}",
                ListSchemaObjectNames = "SELECT name AS Value FROM sqlite_master",
                DisableForeignKeys = "PRAGMA foreign_keys = OFF",
                CountDistrustedForeignKeys = "SELECT 0 AS Value",
                CreateTable = "CREATE TABLE \"Scratch\" (\"Id\" INTEGER)",
                CreateIndex = "CREATE INDEX \"IX_Scratch\" ON \"ActivityLogs\" (\"Name\")",
                CreateTrigger = "CREATE TRIGGER \"Scratch\" AFTER INSERT ON \"ActivityLogs\" BEGIN SELECT 1; END",
                AddColumn = "ALTER TABLE \"ActivityLogs\" ADD COLUMN \"Scratch\" INTEGER"
            }
        },
        new EngineTraits
        {
            Provider = TestDbProvider.Postgres,
            ProviderType = typeof(PostgresDatabaseProvider),
            CoversThroughSqliteMigration = PostgresMigrationBaseline.CoversThroughSqliteMigration,
            ServerMarker = "PostgreSQL",
            IsRegistered = true,
            HasSchemaMigrations = true,
            BoundsIndexedStrings = false,
            UnexpressibleIndexFilters = [],
            NoActionCascades = [],
            NullFilteredUniqueIndexes = [],
            PurgeResetsIdentityInATransaction = true,
            AssertUniqueViolation = ex => Assert.Equal(PostgresErrorCodes.UniqueViolation, Assert.IsType<PostgresException>(ex).SqlState)
        },
        MySqlFamily(TestDbProvider.MySql, null),
        MySqlFamily(TestDbProvider.MariaDb, "MariaDB"),
        new EngineTraits
        {
            Provider = TestDbProvider.SqlServer,
            ProviderType = typeof(SqlServerDatabaseProvider),
            ServerMarker = "Microsoft SQL Server",

            // The server cannot select the provider yet, and the provider has no migrations yet; both change when the
            // provider is registered with its baseline migration.
            IsRegistered = false,
            HasSchemaMigrations = false,
            BoundsIndexedStrings = true,

            // A filtered index cannot have an OR, and the server refuses a cascade to the same table and a second cascade
            // path to a table, so the provider drops that filter and creates the two keys without an action.
            UnexpressibleIndexFilters = [PrimaryVersionFilter],
            NoActionCascades = [("BaseItems", "ParentId"), ("AncestorIds", "ParentItemId")],

            // A unique index of the server takes one NULL, so EF Core filters it to the rows with a value.
            NullFilteredUniqueIndexes = [("ImageInfos", "UserId", "[UserId] IS NOT NULL")],
            PurgeResetsIdentityInATransaction = true,
            AssertUniqueViolation = ex => Assert.Contains(Assert.IsType<SqlException>(ex).Number, new[] { SqlServerDuplicateInIndex, SqlServerDuplicateKey }),
            Pool = new PoolStatements
            {
                ListTables = "SELECT name AS [Value] FROM sys.tables WHERE is_ms_shipped = 0",
                CountRows = "SELECT COUNT_BIG(*) AS [Value] FROM {0}",
                ListSchemaObjectNames = "SELECT name AS [Value] FROM sys.objects WHERE is_ms_shipped = 0 "
                    + "UNION SELECT i.name FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id WHERE i.name IS NOT NULL "
                    + "UNION SELECT c.name FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id",

                // The server has no switch for a session: this stops the checks of every table of the database. The purge
                // of the provider, which resets a returned database, checks every table again WITH CHECK, so that the keys
                // trust their rows again, which the pool self-tests count.
                DisableForeignKeys = "DECLARE @s nvarchar(max) = N''; SELECT @s += N'ALTER TABLE ' + QUOTENAME(name) + N' NOCHECK CONSTRAINT ALL; ' FROM sys.tables WHERE is_ms_shipped = 0; EXEC (@s)",
                CountDistrustedForeignKeys = "SELECT COUNT_BIG(*) AS [Value] FROM sys.foreign_keys WHERE is_disabled = 1 OR is_not_trusted = 1",
                CreateTable = "CREATE TABLE [Scratch] ([Id] int)",
                CreateIndex = "CREATE INDEX [IX_Scratch] ON [ActivityLogs] ([LogSeverity])",
                CreateTrigger = "CREATE TRIGGER [Scratch] ON [ActivityLogs] AFTER INSERT AS SET NOCOUNT ON",
                AddColumn = "ALTER TABLE [ActivityLogs] ADD [Scratch] int NULL"
            }
        }
    }.ToDictionary(traits => traits.Provider);

    private EngineTraits()
    {
    }

    /// <summary>
    /// Gets the traits of every engine.
    /// </summary>
    public static IReadOnlyCollection<EngineTraits> All => _table.Values;

    /// <summary>
    /// Gets the traits of the engine selected for the test run.
    /// </summary>
    public static EngineTraits Selected => For(TestDatabaseEnvironment.Provider);

    /// <summary>
    /// Gets the engine.
    /// </summary>
    public required TestDbProvider Provider { get; init; }

    /// <summary>
    /// Gets the type of the database provider of the server for the engine. Its assembly holds the migrations, and its
    /// key is what the database configuration of the server names.
    /// </summary>
    public required Type ProviderType { get; init; }

    /// <summary>
    /// Gets the last SQLite migration that the baseline migration of the engine already includes, or <see langword="null"/>
    /// for an engine without a baseline.
    /// </summary>
    public string? CoversThroughSqliteMigration { get; init; }

    /// <summary>
    /// Gets a value indicating whether the server can select the provider of the engine, so that it can run on the engine.
    /// The tests that start the server are skipped on an engine that it cannot select.
    /// </summary>
    public required bool IsRegistered { get; init; }

    /// <summary>
    /// Gets a value indicating whether the provider of the engine has migrations. Without them no schema can be built from
    /// migrations, so the tests use the schema of the model, and a test that needs the migrations is skipped.
    /// </summary>
    public required bool HasSchemaMigrations { get; init; }

    /// <summary>
    /// Gets the schema the tests of the engine use unless they ask for one: that of the migrations, or that of the model
    /// for an engine whose provider has no migrations.
    /// </summary>
    public TestSchemaSource DefaultSchemaSource => HasSchemaMigrations ? TestSchemaSource.Migrations : TestSchemaSource.Model;

    /// <summary>
    /// Gets a value indicating whether the engine stores an indexed string that has a maximum length in the model with that
    /// length, so that it refuses a longer value. No row over the length can then be stored.
    /// </summary>
    public required bool BoundsIndexedStrings { get; init; }

    /// <summary>
    /// Gets a value indicating whether the database is a file that the server creates itself, rather than one on a
    /// server that has to be made for it.
    /// </summary>
    public bool IsEmbedded { get; init; }

    /// <summary>
    /// Gets a text that the version the server of the engine reports always holds, and the version that the server of
    /// any other engine reports never does, compared without regard to case; or <see langword="null"/> when the version
    /// of the engine holds no such text. It tells apart servers that speak the same protocol.
    /// </summary>
    public string? ServerMarker { get; init; }

    /// <summary>
    /// Gets the filters of indexes of the SQLite model that the engine cannot express, each the exact text of the filter
    /// of an index that is not unique. The provider of the engine creates such an index over every row.
    /// </summary>
    public required IReadOnlyList<string> UnexpressibleIndexFilters { get; init; }

    /// <summary>
    /// Gets the foreign keys that delete in cascade in the SQLite model and that the engine cannot cascade, by table and
    /// columns. The provider of the engine creates them without an action on delete, and Jellyfin deletes the rows that
    /// point at a deleted row first.
    /// </summary>
    public required IReadOnlyList<(string Table, string Columns)> NoActionCascades { get; init; }

    /// <summary>
    /// Gets the unique indexes with a column that takes NULL, by table and columns, that the provider of the engine filters
    /// to the rows where those columns have a value, with the exact filter. A unique index of such an engine takes one
    /// NULL, and without the filter it would refuse rows that the unique index of the SQLite model takes.
    /// </summary>
    public required IReadOnlyList<(string Table, string Columns, string Filter)> NullFilteredUniqueIndexes { get; init; }

    /// <summary>
    /// Gets where the model of the engine may differ from the SQLite model.
    /// </summary>
    public ModelAllowances ModelAllowances => new(UnexpressibleIndexFilters, NoActionCascades, NullFilteredUniqueIndexes);

    /// <summary>
    /// Gets a value indicating whether the purge of the provider starts the identity columns of the purged tables again
    /// inside a transaction, as it does outside one. A provider whose engine commits the transaction to change a counter
    /// leaves the counters inside one.
    /// </summary>
    public required bool PurgeResetsIdentityInATransaction { get; init; }

    /// <summary>
    /// Gets an assertion that an exception is the violation of a unique index or key on the engine: an exception of
    /// exactly the type that the client of the engine throws, with the code of such a violation.
    /// </summary>
    public required Action<Exception?> AssertUniqueViolation { get; init; }

    /// <summary>
    /// Gets the statements that the self-tests of a pool run, for an engine whose test databases a pool hands out; or
    /// <see langword="null"/> for an engine that copies a template database instead.
    /// </summary>
    public PoolStatements? Pool { get; init; }

    /// <summary>
    /// Gets the assembly that holds the migrations of the engine.
    /// </summary>
    public Assembly MigrationsAssembly => ProviderType.Assembly;

    /// <summary>
    /// Gets the key of the database provider, as the database configuration of the server names it.
    /// </summary>
    public string ProviderKey => ProviderType.GetCustomAttribute<JellyfinDatabaseProviderKeyAttribute>()!.DatabaseProviderKey;

    /// <summary>
    /// Gets the traits of an engine.
    /// </summary>
    /// <param name="provider">The engine.</param>
    /// <returns>Its traits.</returns>
    public static EngineTraits For(TestDbProvider provider)
        => _table.TryGetValue(provider, out var traits)
            ? traits
            : throw new ArgumentOutOfRangeException(nameof(provider), provider, "The engine has no traits.");

    private static EngineTraits MySqlFamily(TestDbProvider provider, string? serverMarker) => new()
    {
        Provider = provider,
        ProviderType = typeof(MySqlDatabaseProvider),
        ServerMarker = serverMarker,

        // Neither server can create a partial index, so the provider removes every filter.
        UnexpressibleIndexFilters = [OwnedFilter, ExtraTypeFilter, AlternateVersionFilter, PrimaryVersionFilter],
        NoActionCascades = [],
        NullFilteredUniqueIndexes = [],
        IsRegistered = true,
        HasSchemaMigrations = true,
        BoundsIndexedStrings = true,
        PurgeResetsIdentityInATransaction = false,
        CoversThroughSqliteMigration = MySqlMigrationBaseline.CoversThroughSqliteMigration,
        AssertUniqueViolation = ex => Assert.Equal(MySqlErrorCode.DuplicateKeyEntry, Assert.IsType<MySqlException>(ex).ErrorCode),
        Pool = new PoolStatements
        {
            ListTables = "SELECT TABLE_NAME AS Value FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE'",
            CountRows = "SELECT COUNT(*) AS Value FROM {0}",
            ListSchemaObjectNames = "SELECT INDEX_NAME AS Value FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() "
                + "UNION SELECT TRIGGER_NAME FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA = DATABASE() "
                + "UNION SELECT COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()",
            DisableForeignKeys = "SET SESSION foreign_key_checks = 0",
            CountDistrustedForeignKeys = "SELECT 0 AS Value",
            CreateTable = "CREATE TABLE `Scratch` (`Id` int)",
            CreateIndex = "CREATE INDEX `IX_Scratch` ON `ActivityLogs` (`LogSeverity`)",
            CreateTrigger = "CREATE TRIGGER `Scratch` BEFORE INSERT ON `ActivityLogs` FOR EACH ROW SET NEW.`Name` = NEW.`Name`",
            AddColumn = "ALTER TABLE `ActivityLogs` ADD COLUMN `Scratch` int NULL"
        }
    };
}
