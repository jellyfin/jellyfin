using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.SqlServer;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SqlServer;

public sealed class SqlServerBaselineSchemaTests
{
    private const string TablesQuery =
        """
        SELECT name AS [Value]
        FROM sys.tables
        WHERE is_ms_shipped = 0
          AND name <> '__EFMigrationsHistory'
        """;

    private const string ColumnsQuery =
        """
        SELECT CONCAT(t.name COLLATE database_default, '.', c.name COLLATE database_default, ' ', tp.name COLLATE database_default,
                      CASE
                          WHEN tp.name IN ('nvarchar', 'nchar') THEN '(' + CASE WHEN c.max_length = -1 THEN 'max' ELSE CAST(c.max_length / 2 AS varchar(10)) END + ')'
                          WHEN tp.name IN ('varchar', 'char', 'varbinary', 'binary') THEN '(' + CASE WHEN c.max_length = -1 THEN 'max' ELSE CAST(c.max_length AS varchar(10)) END + ')'
                          WHEN tp.name IN ('decimal', 'numeric') THEN '(' + CAST(c.precision AS varchar(10)) + ',' + CAST(c.scale AS varchar(10)) + ')'
                          ELSE ''
                      END,
                      CASE WHEN c.is_nullable = 1 THEN ' nullable' ELSE ' not null' END,
                      ' collate ', ISNULL(c.collation_name COLLATE database_default, ''),
                      ' identity ', c.is_identity,
                      ' default ', ISNULL(d.definition COLLATE database_default, '')) AS [Value]
        FROM sys.columns c
        JOIN sys.tables t ON t.object_id = c.object_id
        JOIN sys.types tp ON tp.user_type_id = c.user_type_id
        LEFT JOIN sys.default_constraints d ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
        WHERE t.is_ms_shipped = 0
          AND t.name <> '__EFMigrationsHistory'
        """;

    private const string IndexesQuery =
        """
        SELECT CONCAT(t.name COLLATE database_default, '.', i.name COLLATE database_default, ' #', ic.key_ordinal, ' ', c.name COLLATE database_default,
                      ' unique ', i.is_unique,
                      ' type ', i.type_desc COLLATE database_default,
                      ' desc ', ic.is_descending_key,
                      ' filter ', ISNULL(i.filter_definition COLLATE database_default, '')) AS [Value]
        FROM sys.indexes i
        JOIN sys.tables t ON t.object_id = i.object_id
        JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE t.is_ms_shipped = 0
          AND t.name <> '__EFMigrationsHistory'
          AND ic.is_included_column = 0
        """;

    private const string ForeignKeysQuery =
        """
        SELECT CONCAT(t.name COLLATE database_default, '.', fk.name COLLATE database_default, ' #', fkc.constraint_column_id, ' ', c.name COLLATE database_default,
                      ' -> ', rt.name COLLATE database_default, '.', rc.name COLLATE database_default,
                      ' update ', fk.update_referential_action_desc COLLATE database_default,
                      ' delete ', fk.delete_referential_action_desc COLLATE database_default) AS [Value]
        FROM sys.foreign_keys fk
        JOIN sys.tables t ON t.object_id = fk.parent_object_id
        JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
        JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
        JOIN sys.tables rt ON rt.object_id = fk.referenced_object_id
        JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
        WHERE t.is_ms_shipped = 0
        """;

    [ProviderFact(TestDbProvider.SqlServer)]
    public async Task MigratedDatabase_HasEveryMigrationAppliedStartingWithTheBaseline()
    {
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Migrations, []);
        await using var context = CreateContext(database);

        var applied = await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(SqlServerMigrationBaseline.MigrationId, applied.First());
        Assert.Equal(context.Database.GetMigrations(), applied);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public async Task MigratedDatabase_HasNoPendingModelChanges()
    {
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Migrations, []);
        await using var context = CreateContext(database);

        Assert.False(context.Database.HasPendingModelChanges());
    }

    [ProviderTheory(TestDbProvider.SqlServer)]
    [InlineData("tables", TablesQuery)]
    [InlineData("columns", ColumnsQuery)]
    [InlineData("indexes", IndexesQuery)]
    [InlineData("foreign keys", ForeignKeysQuery)]
    public async Task MigratedSchema_MatchesTheSchemaOfTheModel(string description, string query)
    {
        using var fromModel = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Model, []);
        using var fromMigrations = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Migrations, []);

        var expected = await ReadAsync(fromModel, query);
        var actual = await ReadAsync(fromMigrations, query);

        Assert.NotEmpty(expected);
        Assert.True(
            expected.SequenceEqual(actual),
            $"The {description} differ.{Environment.NewLine}Only in the model: {string.Join("; ", expected.Except(actual))}{Environment.NewLine}Only in the migrations: {string.Join("; ", actual.Except(expected))}");
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public async Task MigratedDatabase_TextColumns_HaveBinaryCollationAndExpectedTypes()
    {
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Migrations, []);

        var query = """
            SELECT CONCAT(t.name COLLATE database_default, '.', c.name COLLATE database_default, ' ', tp.name COLLATE database_default,
                          CASE
                              WHEN c.max_length = -1 THEN '(max)'
                              WHEN tp.name IN ('nvarchar', 'nchar') THEN '(' + CAST(c.max_length / 2 AS varchar(10)) + ')'
                              ELSE '(' + CAST(c.max_length AS varchar(10)) + ')'
                          END, ' collate ', ISNULL(c.collation_name COLLATE database_default, '')) AS [Value]
            FROM sys.columns c
            JOIN sys.tables t ON t.object_id = c.object_id
            JOIN sys.types tp ON tp.user_type_id = c.user_type_id
            WHERE t.is_ms_shipped = 0
              AND t.name <> '__EFMigrationsHistory'
              AND tp.name IN ('nvarchar', 'nchar', 'varchar', 'char', 'text', 'ntext')
            """;

        var rows = await ReadAsync(database, query);
        Assert.NotEmpty(rows);
        Assert.All(rows, row => Assert.Contains("collate " + SqlServerDatabaseProvider.Collation, row, StringComparison.Ordinal));

        // Indexed text-class columns (unbounded in the model) are nvarchar(4000).
        string[] textClassColumns =
        [
            "BaseItemProviders.ProviderValue",
            "BaseItems.CleanName",
            "BaseItems.Name",
            "BaseItems.Path",
            "BaseItems.PresentationUniqueKey",
            "BaseItems.SeriesName",
            "BaseItems.SeriesPresentationUniqueKey",
            "BaseItems.SortName",
            "ItemValues.CleanValue",
            "MediaStreamInfos.Language",
            "Peoples.Name",
            "Peoples.NameLower"
        ];

        foreach (var col in textClassColumns)
        {
            var match = rows.FirstOrDefault(r => r.StartsWith(col + " ", StringComparison.Ordinal));
            Assert.NotNull(match);
            Assert.StartsWith(col + " nvarchar(4000)", match, StringComparison.Ordinal);
        }

        // Unindexed text columns outside every index and key are nvarchar(max).
        string[] unindexedColumns =
        [
            "ActivityLogs.Overview",
            "ActivityLogs.ShortOverview",
            "ImageInfos.Path"
        ];

        foreach (var col in unindexedColumns)
        {
            var match = rows.FirstOrDefault(r => r.StartsWith(col + " ", StringComparison.Ordinal));
            Assert.NotNull(match);
            Assert.StartsWith(col + " nvarchar(max)", match, StringComparison.Ordinal);
        }
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public async Task MigratedDatabase_HoldsTheEighteenOverBudgetIndexes()
    {
        using var database = TestDatabaseEnvironment.Backend.CreateEmpty();
        var logger = new ListLogger();
        var options = new DbContextOptionsBuilder<JellyfinDbContext>(database.Options)
            .UseLoggerFactory(logger)
            .Options;
        await using var context = new JellyfinDbContext(
            options,
            logger,
            database.CreateProvider(),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        Assert.NotEmpty(logger.Entries);
        Assert.DoesNotContain(logger.Entries, entry => entry.Level is LogLevel.Error or LogLevel.Critical);

        var existingIndexes = await context.Database
            .SqlQueryRaw<string>("SELECT i.name AS [Value] FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id WHERE t.is_ms_shipped = 0")
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(18, SqlServerIndexKeyBudgetTests.OverBudget.Count);
        foreach (var indexName in SqlServerIndexKeyBudgetTests.OverBudget.Keys)
        {
            Assert.Contains(indexName, existingIndexes);
        }
    }

    private static async Task<List<string>> ReadAsync(TestDatabase database, string query)
    {
        await using var context = CreateContext(database);
        var rows = await context.Database.SqlQueryRaw<string>(query).ToListAsync(TestContext.Current.CancellationToken);
        return [.. rows.Order(StringComparer.Ordinal)];
    }

    private static JellyfinDbContext CreateContext(TestDatabase database) => new(
        database.Options,
        NullLogger<JellyfinDbContext>.Instance,
        database.CreateProvider(),
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

    private sealed class ListLogger : ILogger<JellyfinDbContext>, ILoggerProvider, ILoggerFactory
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }

        public ILogger CreateLogger(string categoryName) => this;

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }
    }
}
