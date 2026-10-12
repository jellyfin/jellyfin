using System;
using Xunit.Sdk;

namespace Jellyfin.Database.Testing;

/// <summary>
/// The known differences between providers. An entry is deleted when the difference is closed, which breaks the
/// build wherever a test is still marked with it.
/// </summary>
public static class KnownGaps
{
    public static readonly KnownGapDefinition PostgresMigrations = new(
        TestDbProvider.Postgres,
        "PostgreSQL has no migrations",
        ex => ex is NotSupportedException && string.Equals(ex.Message, PostgresTestBackend.NoMigrationsMessage, StringComparison.Ordinal));

    public static readonly KnownGapDefinition GuidAggregates = new(
        TestDbProvider.Postgres,
        "PostgreSQL has no min/max over uuid",
        ex => PostgresErrors.HasSqlState(ex, "42883", "min(uuid)", "max(uuid)"));

    public static readonly KnownGapDefinition NulInExactMatchParameter = new(
        TestDbProvider.Postgres,
        "PostgreSQL rejects a NUL character in any string parameter",
        ex => PostgresErrors.HasSqlState(ex, "22021"));

    public static readonly KnownGapDefinition StringColumnLength = new(
        TestDbProvider.Postgres,
        "PostgreSQL enforces the declared length of a string column",
        ex => PostgresErrors.HasSqlState(ex, "22001"));

    public static readonly KnownGapDefinition StringCollation = new(
        TestDbProvider.Postgres,
        "PostgreSQL orders strings by collation",
        ex => ex is IAssertionException);

    public static readonly KnownGapDefinition NullOrdering = new(
        TestDbProvider.Postgres,
        "PostgreSQL sorts NULL first in descending order",
        ex => ex is IAssertionException);

    public static readonly KnownGapDefinition UnicodeLower = new(
        TestDbProvider.Postgres,
        "PostgreSQL lower() follows the database collation",
        ex => ex is IAssertionException);
}
