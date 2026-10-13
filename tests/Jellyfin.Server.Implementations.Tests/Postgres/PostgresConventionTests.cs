using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

public sealed class PostgresConventionTests : DatabaseTestFixture
{
    [ProviderFact(TestDbProvider.Postgres)]
    public async Task StringColumns_AreTextWithCCollation()
    {
        await using var context = CreateDbContext();
        var expected = context.Model.GetEntityTypes()
            .Where(e => e.GetTableName() is not null)
            .SelectMany(e => e.GetProperties()
                .Where(p => p.ClrType == typeof(string))
                .Select(p => $"{e.GetTableName()}.{p.GetColumnName()}"))
            .ToHashSet(StringComparer.Ordinal);
        Assert.NotEmpty(expected);

        var textColumns = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT c.relname || '.' || a.attname AS "Value"
                FROM pg_attribute a
                JOIN pg_class c ON c.oid = a.attrelid
                JOIN pg_type t ON t.oid = a.atttypid
                WHERE c.relnamespace = current_schema()::regnamespace
                  AND c.relkind = 'r'
                  AND a.attnum > 0
                  AND NOT a.attisdropped
                  AND t.typname = 'text'
                """)
            .ToListAsync();
        var notCollated = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT c.relname || '.' || a.attname AS "Value"
                FROM pg_attribute a
                JOIN pg_class c ON c.oid = a.attrelid
                JOIN pg_type t ON t.oid = a.atttypid
                JOIN pg_collation o ON o.oid = a.attcollation
                WHERE c.relnamespace = current_schema()::regnamespace
                  AND c.relkind = 'r'
                  AND a.attnum > 0
                  AND NOT a.attisdropped
                  AND t.typname = 'text'
                  AND o.collname <> 'C'
                """)
            .ToListAsync();

        Assert.Empty(expected.Except(textColumns, StringComparer.Ordinal));
        Assert.Empty(notCollated);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task Session_ConnectionStringNamingAnotherTimeZone_StillUsesUtc()
    {
        await using var context = CreateDbContext();
        var configured = new NpgsqlConnectionStringBuilder(context.Database.GetConnectionString())
        {
            Timezone = "Europe/Paris"
        };
        var options = new DbContextOptionsBuilder<JellyfinDbContext>();
        new PostgresDatabaseProvider(NullLogger<PostgresDatabaseProvider>.Instance).Initialise(
            options,
            new DatabaseConfigurationOptions
            {
                DatabaseType = "Jellyfin-PostgreSQL",
                CustomProviderOptions = new CustomDatabaseOptions
                {
                    PluginName = string.Empty,
                    PluginAssembly = string.Empty,
                    ConnectionString = configured.ConnectionString
                }
            });

        await using var connection = new NpgsqlConnection(RelationalOptionsExtension.Extract(options.Options).ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SHOW TimeZone", connection);

        Assert.Equal("UTC", await command.ExecuteScalarAsync());
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task DateTimeColumns_AreTimestampsWithTimeZone()
    {
        await using var context = CreateDbContext();
        var expected = context.Model.GetEntityTypes()
            .Where(e => e.GetTableName() is not null)
            .SelectMany(e => e.GetProperties()
                .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?))
                .Select(p => $"{e.GetTableName()}.{p.GetColumnName()}"))
            .ToHashSet(StringComparer.Ordinal);
        Assert.NotEmpty(expected);

        var timestamps = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT table_name || '.' || column_name AS "Value"
                FROM information_schema.columns
                WHERE table_schema = current_schema()
                  AND data_type = 'timestamp with time zone'
                """)
            .ToListAsync();
        var plainTimestamps = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT table_name || '.' || column_name AS "Value"
                FROM information_schema.columns
                WHERE table_schema = current_schema()
                  AND data_type = 'timestamp without time zone'
                """)
            .ToListAsync();

        Assert.Equal(expected.Order(StringComparer.Ordinal), timestamps.Order(StringComparer.Ordinal));
        Assert.Empty(plainTimestamps);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task ModelIdentifiers_FitThePostgresLimit_AndAreUnique()
    {
        await using var context = CreateDbContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var entityTypes = model.GetEntityTypes().Where(e => e.GetTableName() is not null).ToList();

        var tables = entityTypes.Select(e => e.GetTableName()!).ToList();
        var columns = entityTypes.SelectMany(e => e.GetProperties().Select(p => p.GetColumnName())).ToList();
        var keysAndIndexes = entityTypes
            .SelectMany(e => e.GetKeys().Select(k => k.GetName()!).Concat(e.GetIndexes().Select(i => i.GetDatabaseName()!)))
            .ToList();
        var foreignKeys = entityTypes.SelectMany(e => e.GetForeignKeys().Select(f => f.GetConstraintName()!)).ToList();

        var all = tables.Concat(columns).Concat(keysAndIndexes).Concat(foreignKeys).ToList();
        var tooLong = all.Where(n => Encoding.UTF8.GetByteCount(n) > 63).ToList();
        var truncated = all.Where(n => n.Contains('~', StringComparison.Ordinal)).ToList();
        Assert.Empty(tooLong);
        Assert.Empty(truncated);

        var relationNames = tables.Concat(keysAndIndexes).ToList();
        Assert.Empty(relationNames.GroupBy(n => n, StringComparer.Ordinal).Where(g => g.Count() > 1).Select(g => g.Key));
        Assert.Empty(entityTypes
            .SelectMany(e => e.GetForeignKeys().Select(f => (Table: e.GetTableName(), Name: f.GetConstraintName())))
            .GroupBy(f => f)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key));
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task Database_HoldsEveryIndexAndConstraintNameOfTheModel()
    {
        await using var context = CreateDbContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var entityTypes = model.GetEntityTypes().Where(e => e.GetTableName() is not null).ToList();
        var expectedIndexes = entityTypes
            .SelectMany(e => e.GetIndexes().Select(i => i.GetDatabaseName()!).Concat(e.GetKeys().Select(k => k.GetName()!)))
            .Order(StringComparer.Ordinal)
            .ToList();
        var expectedForeignKeys = entityTypes
            .SelectMany(e => e.GetForeignKeys().Select(f => f.GetConstraintName()!))
            .Order(StringComparer.Ordinal)
            .ToList();

        // The history table is created by EF Core, so it and its key are not in the model.
        var indexes = await context.Database
            .SqlQueryRaw<string>("""SELECT indexname AS "Value" FROM pg_indexes WHERE schemaname = current_schema() AND tablename <> '__EFMigrationsHistory' ORDER BY 1""")
            .ToListAsync();
        var foreignKeys = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT conname AS "Value"
                FROM pg_constraint
                WHERE contype = 'f' AND connamespace = current_schema()::regnamespace
                ORDER BY 1
                """)
            .ToListAsync();

        Assert.Equal(expectedIndexes, indexes.Order(StringComparer.Ordinal));
        Assert.Equal(expectedForeignKeys, foreignKeys.Order(StringComparer.Ordinal));
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task Columns_AreNeverCharacterVarying()
    {
        await using var context = CreateDbContext();

        // The columns of the history table are created by EF Core, so they are not in the model.
        var columns = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT table_name || '.' || column_name AS "Value"
                FROM information_schema.columns
                WHERE table_schema = current_schema()
                  AND table_name <> '__EFMigrationsHistory'
                  AND data_type IN ('character varying', 'character')
                """)
            .ToListAsync();

        Assert.Empty(columns);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task OrderBy_NullableColumn_SpellsOutTheNullPlacementOfSqlite()
    {
        await using var context = CreateDbContext();

        var itemsAscending = context.BaseItems.OrderBy(e => e.PremiereDate).Select(e => e.Id).ToQueryString();
        var itemsDescending = context.BaseItems.OrderByDescending(e => e.PremiereDate).Select(e => e.Id).ToQueryString();
        var userDataAscending = context.UserData.OrderBy(e => e.LastPlayedDate).Select(e => e.ItemId).ToQueryString();
        var userDataDescending = context.UserData.OrderByDescending(e => e.LastPlayedDate).Select(e => e.ItemId).ToQueryString();

        Assert.Contains("\"PremiereDate\" NULLS FIRST", itemsAscending, StringComparison.Ordinal);
        Assert.Contains("\"PremiereDate\" DESC NULLS LAST", itemsDescending, StringComparison.Ordinal);
        Assert.Contains("\"LastPlayedDate\" NULLS FIRST", userDataAscending, StringComparison.Ordinal);
        Assert.Contains("\"LastPlayedDate\" DESC NULLS LAST", userDataDescending, StringComparison.Ordinal);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task OrderBy_NonNullableColumn_HasNoNullPlacement()
    {
        await using var context = CreateDbContext();

        var ascending = context.BaseItems.OrderBy(e => e.Id).Select(e => e.Id).ToQueryString();
        var descending = context.BaseItems.OrderByDescending(e => e.Id).Select(e => e.Id).ToQueryString();

        Assert.Contains("ORDER BY b.\"Id\"", ascending, StringComparison.Ordinal);
        Assert.Contains("ORDER BY b.\"Id\" DESC", descending, StringComparison.Ordinal);
        Assert.DoesNotContain("NULLS", ascending, StringComparison.Ordinal);
        Assert.DoesNotContain("NULLS", descending, StringComparison.Ordinal);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task OrderBy_ExpressionThatIsNotAColumn_SpellsOutTheNullPlacement()
    {
        await using var context = CreateDbContext();

        var ascending = context.BaseItems.OrderBy(e => e.Name!.Length).Select(e => e.Id).ToQueryString();
        var descending = context.BaseItems.OrderByDescending(e => e.Name!.Length).Select(e => e.Id).ToQueryString();

        Assert.Contains("length(b.\"Name\")::int NULLS FIRST", ascending, StringComparison.Ordinal);
        Assert.Contains("length(b.\"Name\")::int DESC NULLS LAST", descending, StringComparison.Ordinal);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task OrderBy_ColumnThroughAnOptionalNavigation_SpellsOutTheNullPlacement()
    {
        await using var context = CreateDbContext();

        var ascending = context.BaseItems.OrderBy(b => b.Owner!.Type).Select(b => b.Id).ToQueryString();
        var descending = context.BaseItems.OrderByDescending(b => b.Owner!.Type).Select(b => b.Id).ToQueryString();

        Assert.Contains("LEFT JOIN", ascending, StringComparison.Ordinal);
        Assert.Contains("\"Type\" NULLS FIRST", ascending, StringComparison.Ordinal);
        Assert.Contains("\"Type\" DESC NULLS LAST", descending, StringComparison.Ordinal);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task OrderBy_PrimaryKeyWithLimit_UsesTheIndexInsteadOfSorting()
    {
        await using (var context = CreateDbContext())
        {
            context.BaseItems.AddRange(
                Enumerable.Range(0, 5000).Select(_ => new BaseItemEntity { Id = Guid.NewGuid(), Type = "Movie" }));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            await context.Database.ExecuteSqlRawAsync("ANALYZE \"BaseItems\"", TestContext.Current.CancellationToken);
        }

        await using (var context = CreateDbContext())
        {
            var query = context.BaseItems.OrderBy(e => e.Id).Select(e => e.Id).ToQueryString();
            var sql = string.Join('\n', query.Split('\n').Where(l => !l.StartsWith("--", StringComparison.Ordinal)));
            var plan = new StringBuilder();
            await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
            await using var command = context.Database.GetDbConnection().CreateCommand();
#pragma warning disable CA2100 // The SQL is the query that EF Core generated from the model above.
            command.CommandText = $"EXPLAIN {sql} LIMIT 10";
#pragma warning restore CA2100
            await using (var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken))
            {
                while (await reader.ReadAsync(TestContext.Current.CancellationToken))
                {
                    plan.AppendLine(reader.GetString(0));
                }
            }

            Assert.Contains("Index", plan.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain("Sort", plan.ToString(), StringComparison.Ordinal);
        }
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task OrderBy_NullableColumn_PutsNullsFirstAscendingAndLastDescending()
    {
        var withoutDate = Guid.NewGuid();
        var early = Guid.NewGuid();
        var late = Guid.NewGuid();
        await using (var context = CreateDbContext())
        {
            context.BaseItems.AddRange(
                new BaseItemEntity { Id = early, Type = "Movie", PremiereDate = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new BaseItemEntity { Id = withoutDate, Type = "Movie" },
                new BaseItemEntity { Id = late, Type = "Movie", PremiereDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
            await context.SaveChangesAsync();
        }

        await using (var context = CreateDbContext())
        {
            var seeded = new[] { withoutDate, early, late };

            var ascending = await context.BaseItems
                .Where(e => seeded.Contains(e.Id))
                .OrderBy(e => e.PremiereDate)
                .Select(e => e.Id)
                .ToListAsync();
            var descending = await context.BaseItems
                .Where(e => seeded.Contains(e.Id))
                .OrderByDescending(e => e.PremiereDate)
                .Select(e => e.Id)
                .ToListAsync();

            Assert.Equal([withoutDate, early, late], ascending);
            Assert.Equal([late, early, withoutDate], descending);
        }
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task Indexes_SortNullsFirstOnNullableColumns_AndKeepTheDefaultOrderOnTheOthers()
    {
        await using var context = CreateDbContext();

        // Bit 0 of indoption is DESC and bit 1 is NULLS FIRST. A column that cannot be null needs the default order,
        // which is also the one of the index behind a key, and one that can be null needs NULLS FIRST.
        var deviating = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT c.relname || '.' || (o.ordinality - 1) AS "Value"
                FROM pg_index i
                JOIN pg_class c ON c.oid = i.indexrelid
                CROSS JOIN LATERAL unnest(i.indoption::int2[], i.indkey::int2[]) WITH ORDINALITY AS o(option, attnum, ordinality)
                LEFT JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = o.attnum
                WHERE c.relnamespace = current_schema()::regnamespace
                  AND o.ordinality <= i.indnkeyatts
                  AND (o.option & 3) <> CASE WHEN a.attnotnull IS TRUE THEN 0 ELSE 2 END
                """)
            .ToListAsync();
        var indexCount = await context.Database
            .SqlQueryRaw<int>(
                """
                SELECT count(*)::int AS "Value"
                FROM pg_index i
                JOIN pg_class c ON c.oid = i.indexrelid
                WHERE c.relnamespace = current_schema()::regnamespace
                """)
            .SingleAsync();

        Assert.True(indexCount > 20, $"Only {indexCount} indexes were found.");
        Assert.Empty(deviating);
    }
}
