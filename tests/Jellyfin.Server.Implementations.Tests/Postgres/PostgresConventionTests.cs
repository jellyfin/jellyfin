using System;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
