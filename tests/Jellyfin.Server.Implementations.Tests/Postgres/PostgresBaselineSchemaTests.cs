using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

public sealed class PostgresBaselineSchemaTests
{
    private const string ColumnsQuery =
        """
        SELECT c.relname || '.' || a.attname || ' ' || format_type(a.atttypid, a.atttypmod)
               || CASE WHEN a.attnotnull THEN ' NOT NULL' ELSE '' END
               || ' collate ' || COALESCE(o.collname, '')
               || ' identity ' || a.attidentity::text
               || ' default ' || COALESCE(pg_get_expr(d.adbin, d.adrelid), '') AS "Value"
        FROM pg_attribute a
        JOIN pg_class c ON c.oid = a.attrelid
        LEFT JOIN pg_collation o ON o.oid = a.attcollation AND a.attcollation <> 0
        LEFT JOIN pg_attrdef d ON d.adrelid = a.attrelid AND d.adnum = a.attnum
        WHERE c.relnamespace = current_schema()::regnamespace
          AND c.relkind = 'r'
          AND a.attnum > 0
          AND NOT a.attisdropped
          AND c.relname <> '__EFMigrationsHistory'
        """;

    private const string IndexesQuery =
        """
        SELECT indexname || ': ' || indexdef AS "Value"
        FROM pg_indexes
        WHERE schemaname = current_schema()
          AND tablename <> '__EFMigrationsHistory'
          AND indexname <> 'IX_Peoples_NameLower'
        """;

    private const string ConstraintsQuery =
        """
        SELECT c.relname || ': ' || k.conname || ' ' || pg_get_constraintdef(k.oid) AS "Value"
        FROM pg_constraint k
        JOIN pg_class c ON c.oid = k.conrelid
        WHERE k.connamespace = current_schema()::regnamespace
          AND c.relname <> '__EFMigrationsHistory'
        """;

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task MigratedDatabase_HasOnlyTheBaselineApplied()
    {
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Migrations, []);
        await using var context = CreateContext(database);

        var applied = await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);

        Assert.Equal([PostgresMigrationBaseline.MigrationId], applied);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));
    }

    [ProviderTheory(TestDbProvider.Postgres)]
    [InlineData("columns", ColumnsQuery)]
    [InlineData("indexes", IndexesQuery)]
    [InlineData("constraints", ConstraintsQuery)]
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
