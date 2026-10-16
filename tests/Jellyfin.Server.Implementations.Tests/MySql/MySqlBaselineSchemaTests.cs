using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.MySql;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;
using Microting.EntityFrameworkCore.MySql.Metadata;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

public sealed class MySqlBaselineSchemaTests
{
    private const string TablesQuery =
        """
        SELECT CONCAT(TABLE_NAME, ' ', ENGINE, ' ', TABLE_COLLATION, ' ', ROW_FORMAT) AS `Value`
        FROM information_schema.TABLES
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_TYPE = 'BASE TABLE'
          AND TABLE_NAME <> '__EFMigrationsHistory'
        """;

    private const string ColumnsQuery =
        """
        SELECT CONCAT(TABLE_NAME, '.', COLUMN_NAME, ' ', COLUMN_TYPE, ' nullable ', IS_NULLABLE,
                      ' charset ', IFNULL(CHARACTER_SET_NAME, ''), ' collate ', IFNULL(COLLATION_NAME, ''),
                      ' extra ', EXTRA, ' default ', IFNULL(COLUMN_DEFAULT, '<none>'),
                      ' generated ', IFNULL(GENERATION_EXPRESSION, '')) AS `Value`
        FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME <> '__EFMigrationsHistory'
        """;

    private const string IndexesQuery =
        """
        SELECT CONCAT(TABLE_NAME, '.', INDEX_NAME, ' #', SEQ_IN_INDEX, ' ', COLUMN_NAME,
                      ' unique ', 1 - NON_UNIQUE, ' prefix ', IFNULL(SUB_PART, '<none>'),
                      ' ', INDEX_TYPE, ' ', IFNULL(COLLATION, '')) AS `Value`
        FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME <> '__EFMigrationsHistory'
        """;

    private const string ForeignKeysQuery =
        """
        SELECT CONCAT(k.TABLE_NAME, '.', k.CONSTRAINT_NAME, ' #', k.ORDINAL_POSITION, ' ', k.COLUMN_NAME,
                      ' -> ', k.REFERENCED_TABLE_NAME, '.', k.REFERENCED_COLUMN_NAME,
                      ' update ', r.UPDATE_RULE, ' delete ', r.DELETE_RULE) AS `Value`
        FROM information_schema.KEY_COLUMN_USAGE k
        JOIN information_schema.REFERENTIAL_CONSTRAINTS r
          ON r.CONSTRAINT_SCHEMA = k.CONSTRAINT_SCHEMA
         AND r.TABLE_NAME = k.TABLE_NAME
         AND r.CONSTRAINT_NAME = k.CONSTRAINT_NAME
        WHERE k.TABLE_SCHEMA = DATABASE()
          AND k.REFERENCED_TABLE_NAME IS NOT NULL
        """;

    private const string PrefixesQuery =
        """
        SELECT TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX, COLUMN_NAME, SUB_PART
        FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
        """;

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public async Task MigratedDatabase_HasEveryMigrationAppliedStartingWithTheBaseline()
    {
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Migrations, []);
        await using var context = CreateContext(database);

        var applied = await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(MySqlMigrationBaseline.MigrationId, applied.First());
        Assert.Equal(context.Database.GetMigrations(), applied);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public async Task MigratedDatabase_HasNoPendingModelChanges()
    {
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Migrations, []);
        await using var context = CreateContext(database);

        Assert.False(context.Database.HasPendingModelChanges());
    }

    [ProviderTheory(TestDbProvider.MySql, TestDbProvider.MariaDb)]
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

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public async Task MigratedDatabase_IndexesTextOnlyByThePrefixTheConventionChose()
    {
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Migrations, []);
        await using var context = CreateContext(database);

        var expected = ExpectedPrefixes(context);
        var actual = await ReadPrefixesAsync(context);

        Assert.Contains(expected, entry => entry.Value is not null);
        Assert.Equal(512, actual["BaseItems.IX_BaseItems_Path #1 Path"]);
        Assert.True(
            Describe(expected).SequenceEqual(Describe(actual)),
            $"The prefix lengths differ.{Environment.NewLine}Expected: {string.Join("; ", Describe(expected))}{Environment.NewLine}On the server: {string.Join("; ", Describe(actual))}");
    }

    private static Dictionary<string, int?> ExpectedPrefixes(JellyfinDbContext context)
    {
        var expected = new Dictionary<string, int?>(StringComparer.Ordinal);
        foreach (var entityType in context.GetService<IDesignTimeModel>().Model.GetEntityTypes().Where(e => e.GetTableName() is not null))
        {
            var table = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
            foreach (var index in entityType.GetDeclaredIndexes())
            {
                var prefixes = index.PrefixLength();
                for (var position = 0; position < index.Properties.Count; position++)
                {
                    var prefix = prefixes is { } lengths && lengths[position] > 0 ? lengths[position] : (int?)null;
                    expected[Key(table.Name, index.GetDatabaseName(table)!, position + 1, index.Properties[position].GetColumnName(table)!)] = prefix;
                }
            }
        }

        return expected;
    }

    private static async Task<Dictionary<string, int?>> ReadPrefixesAsync(JellyfinDbContext context)
    {
        await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = PrefixesQuery;
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var rows = new Dictionary<string, int?>(StringComparer.Ordinal);
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            var table = reader.GetString(0);
            var index = reader.GetString(1);
            if (table == "__EFMigrationsHistory" || index == "PRIMARY")
            {
                continue;
            }

            var position = Convert.ToInt32(reader.GetValue(2), CultureInfo.InvariantCulture);
            var prefix = await reader.IsDBNullAsync(4, TestContext.Current.CancellationToken) ? (int?)null : Convert.ToInt32(reader.GetValue(4), CultureInfo.InvariantCulture);
            rows[Key(table, index, position, reader.GetString(3))] = prefix;
        }

        return rows;
    }

    private static string Key(string table, string index, int position, string column)
    {
        return $"{table}.{index} #{position} {column}";
    }

    private static List<string> Describe(Dictionary<string, int?> prefixes)
    {
        return [.. prefixes.Select(entry => $"{entry.Key} = {(entry.Value is { } prefix ? prefix.ToString(CultureInfo.InvariantCulture) : "none")}").Order(StringComparer.Ordinal)];
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
}
