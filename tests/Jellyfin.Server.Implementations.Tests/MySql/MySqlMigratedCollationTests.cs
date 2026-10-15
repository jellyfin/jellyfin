using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

/// <summary>
/// After the migrations of a new installation, the server reports the collation <c>utf8mb4_0900_bin</c> as the default
/// of the database, of every table, the migration history included, and of every text column, however the database
/// came to exist.
/// </summary>
public sealed partial class MySqlMigratedCollationTests
{
    private const string Binary = "utf8mb4_0900_bin";
    private const string HistoryTable = "__EFMigrationsHistory";

    /// <summary>
    /// How the database of a new installation comes to exist.
    /// </summary>
    public enum Creation
    {
        /// <summary>The database does not exist, and the provider creates it when it migrates.</summary>
        ByTheProvider,

        /// <summary>The database was created with the defaults of the server, which compare without regard to case.</summary>
        WithTheServerDefaults,

        /// <summary>The database was created with <c>utf8mb4</c> and <c>utf8mb4_0900_bin</c>, as the documentation says.</summary>
        WithTheBinaryCollation
    }

    [ProviderTheory(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    [InlineData(Creation.ByTheProvider)]
    [InlineData(Creation.WithTheServerDefaults)]
    [InlineData(Creation.WithTheBinaryCollation)]
    public async Task Migrate_OfANewDatabase_MakesTheDatabaseEveryTableAndEveryTextColumnBinary(Creation creation)
    {
        // The test databases are created with the binary collation, which is the last case.
        using var database = TestDatabaseEnvironment.Backend.CreateEmpty();
        var connectionString = RelationalOptionsExtension.Extract(database.Options).ConnectionString!;
        var name = new MySqlConnectionStringBuilder(connectionString).Database;
        switch (creation)
        {
            case Creation.ByTheProvider:
                await ExecuteOnServerAsync(connectionString, $"DROP DATABASE `{name}`");
                break;
            case Creation.WithTheServerDefaults:
                await ExecuteOnServerAsync(connectionString, $"DROP DATABASE `{name}`");
                await ExecuteOnServerAsync(connectionString, $"CREATE DATABASE `{name}`");
                break;
        }

        HashSet<string> expectedColumns;
        await using (var context = CreateContext(database))
        {
            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
            expectedColumns = TextColumnsOfTheModel(context);
        }

        await using var connection = new MySqlConnection(new MySqlConnectionStringBuilder(connectionString) { Pooling = false }.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var schema = Assert.Single(await ReadAsync(connection, "SELECT DEFAULT_CHARACTER_SET_NAME, DEFAULT_COLLATION_NAME FROM information_schema.SCHEMATA WHERE SCHEMA_NAME = DATABASE()"));
        var tables = await ReadAsync(connection, "SELECT TABLE_NAME, TABLE_COLLATION FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE'");
        var columns = await ReadAsync(connection, "SELECT CONCAT(TABLE_NAME, '.', COLUMN_NAME), COLLATION_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND COLLATION_NAME IS NOT NULL");

        expectedColumns.UnionWith([$"{HistoryTable}.MigrationId", $"{HistoryTable}.ProductVersion"]);
        Assert.Multiple(
            () => Assert.Equal(["utf8mb4", Binary], schema),
            () => Assert.Contains(tables, t => t[0] == HistoryTable),
            () => Assert.Empty(tables.Where(t => t[1] != Binary).Select(t => $"{t[0]} {t[1]}")),
            () => Assert.Equal(expectedColumns.Order(StringComparer.Ordinal), columns.Select(c => c[0]).Order(StringComparer.Ordinal)),
            () => Assert.Empty(columns.Where(c => c[1] != Binary).Select(c => $"{c[0]} {c[1]}")));
    }

    // The columns that the model stores as text, read from the model and not from the convention that sets their collation.
    private static HashSet<string> TextColumnsOfTheModel(JellyfinDbContext context)
    {
        var columns = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entityType in context.GetService<IDesignTimeModel>().Model.GetEntityTypes().Where(e => e.GetTableName() is not null))
        {
            var table = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
            foreach (var property in entityType.GetFlattenedProperties().Where(p => p.GetColumnType() is { } type && TextStoreType().IsMatch(type)))
            {
                columns.Add($"{table.Name}.{property.GetColumnName(table)}");
            }
        }

        return columns;
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "The statements are built from the generated name of a test database.")]
    private static async Task ExecuteOnServerAsync(string connectionString, string commandText)
    {
        var server = new MySqlConnectionStringBuilder(connectionString) { Database = string.Empty, Pooling = false };
        await using var connection = new MySqlConnection(server.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new MySqlCommand(commandText, connection);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "The queries are constants.")]
    private static async Task<List<string[]>> ReadAsync(MySqlConnection connection, string query)
    {
        await using var command = new MySqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var rows = new List<string[]>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            rows.Add([.. Enumerable.Range(0, reader.FieldCount).Select(i => Text(reader.GetValue(i)))]);
        }

        return rows;
    }

    // MySQL returns some columns of information_schema as bytes.
    private static string Text(object value)
    {
        return value is byte[] bytes ? Encoding.UTF8.GetString(bytes) : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static JellyfinDbContext CreateContext(TestDatabase database) => new(
        database.Options,
        NullLogger<JellyfinDbContext>.Instance,
        database.CreateProvider(),
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

    [GeneratedRegex(@"^(char|varchar|tinytext|text|mediumtext|longtext)(\(|\s|$)", RegexOptions.IgnoreCase)]
    private static partial Regex TextStoreType();
}
