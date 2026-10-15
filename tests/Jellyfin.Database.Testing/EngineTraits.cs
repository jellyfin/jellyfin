using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Providers.MySql;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Database.Providers.Sqlite;
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

    private static readonly Dictionary<TestDbProvider, EngineTraits> _table = new[]
    {
        new EngineTraits
        {
            Provider = TestDbProvider.Sqlite,
            ProviderType = typeof(SqliteDatabaseProvider),
            IsEmbedded = true,
            SupportsPartialIndexes = true,
            PurgeResetsIdentityInATransaction = true,
            AssertUniqueViolation = ex => Assert.Equal(SqliteConstraintUnique, Assert.IsType<SqliteException>(ex).SqliteExtendedErrorCode),
            Pool = new PoolStatements
            {
                ListTables = "SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'",
                ListSchemaObjectNames = "SELECT name AS Value FROM sqlite_master",
                DisableForeignKeys = "PRAGMA foreign_keys = OFF",
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
            SupportsPartialIndexes = true,
            PurgeResetsIdentityInATransaction = true,
            AssertUniqueViolation = ex => Assert.Equal(PostgresErrorCodes.UniqueViolation, Assert.IsType<PostgresException>(ex).SqlState)
        },
        MySqlFamily(TestDbProvider.MySql, null),
        MySqlFamily(TestDbProvider.MariaDb, "MariaDB")
    }.ToDictionary(traits => traits.Provider);

    private EngineTraits()
    {
    }

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
    /// Gets a value indicating whether the engine can create a partial index, one over the rows that match a filter.
    /// The provider of an engine that cannot creates a filtered index of the model over every row.
    /// </summary>
    public required bool SupportsPartialIndexes { get; init; }

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
        SupportsPartialIndexes = false,
        PurgeResetsIdentityInATransaction = false,
        CoversThroughSqliteMigration = MySqlMigrationBaseline.CoversThroughSqliteMigration,
        AssertUniqueViolation = ex => Assert.Equal(MySqlErrorCode.DuplicateKeyEntry, Assert.IsType<MySqlException>(ex).ErrorCode),
        Pool = new PoolStatements
        {
            ListTables = "SELECT TABLE_NAME AS Value FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE'",
            ListSchemaObjectNames = "SELECT INDEX_NAME AS Value FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() "
                + "UNION SELECT TRIGGER_NAME FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA = DATABASE() "
                + "UNION SELECT COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()",
            DisableForeignKeys = "SET SESSION foreign_key_checks = 0",
            CreateTable = "CREATE TABLE `Scratch` (`Id` int)",
            CreateIndex = "CREATE INDEX `IX_Scratch` ON `ActivityLogs` (`LogSeverity`)",
            CreateTrigger = "CREATE TRIGGER `Scratch` BEFORE INSERT ON `ActivityLogs` FOR EACH ROW SET NEW.`Name` = NEW.`Name`",
            AddColumn = "ALTER TABLE `ActivityLogs` ADD COLUMN `Scratch` int NULL"
        }
    };
}
