using System;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
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
