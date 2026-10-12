using Xunit.Sdk;

namespace Jellyfin.Database.Testing;

/// <summary>
/// The known differences between providers. An entry is deleted when the difference is closed, which breaks the
/// build wherever a test is still marked with it.
/// </summary>
public static class KnownGaps
{
    public static readonly KnownGapDefinition GuidAggregates = new(
        TestDbProvider.Postgres,
        "PostgreSQL has no min/max over uuid",
        ex => PostgresErrors.HasSqlState(ex, "42883", "min(uuid)", "max(uuid)"));

    public static readonly KnownGapDefinition NulInExactMatchParameter = new(
        TestDbProvider.Postgres,
        "PostgreSQL rejects a NUL character in any string parameter",
        ex => PostgresErrors.HasSqlState(ex, "22021"));
}
