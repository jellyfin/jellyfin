using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
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

        var indexes = await context.Database
            .SqlQueryRaw<string>("""SELECT indexname AS "Value" FROM pg_indexes WHERE schemaname = current_schema() ORDER BY 1""")
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

        var columns = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT table_name || '.' || column_name AS "Value"
                FROM information_schema.columns
                WHERE table_schema = current_schema()
                  AND data_type IN ('character varying', 'character')
                """)
            .ToListAsync();

        Assert.Empty(columns);
    }
}
