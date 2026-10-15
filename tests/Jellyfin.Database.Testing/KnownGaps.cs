using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Xunit.Sdk;

namespace Jellyfin.Database.Testing;

/// <summary>
/// The known differences between providers. An entry is deleted when the difference is closed, which breaks the
/// build wherever a test is still marked with it.
/// </summary>
public static class KnownGaps
{
    private const string ActualPrefix = "Actual:";

    private const string SortNameGreaterOutcome = "\"Émile, Öland, émile, ölfass\"";

    private const string SortNameLessOutcome = "\"Emile\"";

    public static readonly KnownGapDefinition NulInExactMatchParameter = new(
        TestDbProvider.Postgres,
        "PG-NUL-PARAMETER",
        "PostgreSQL rejects a NUL character in any string parameter",
        ex => PostgresErrors.HasSqlState(ex, "22021"));

    public static readonly KnownGapDefinition SortNameRangeLowersNonAsciiOnMySql = new(
        TestDbProvider.MySql,
        "MYSQL-SORTNAME-RANGE-CASE",
        "The sort name range filters lower-case non-ASCII letters of the sort name on MySQL",
        IsSortNameRangeOutcome);

    public static readonly KnownGapDefinition SortNameRangeLowersNonAsciiOnMariaDb = new(
        TestDbProvider.MariaDb,
        "MARIADB-SORTNAME-RANGE-CASE",
        "The sort name range filters lower-case non-ASCII letters of the sort name on MariaDB",
        IsSortNameRangeOutcome);

    // Both servers return the same lists for the two tests that use this gap. The lists are compared in full, so a
    // different list, or a different kind of failure, is not the known difference and fails the test.
    private static bool IsSortNameRangeOutcome(Exception ex)
    {
        if (ex is not EqualException)
        {
            return false;
        }

        var actual = ex.Message
            .Split('\n')
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.StartsWith(ActualPrefix, StringComparison.Ordinal));
        return actual is not null
            && actual[ActualPrefix.Length..].Trim() is SortNameGreaterOutcome or SortNameLessOutcome;
    }

    public static readonly KnownGapDefinition ContainsOverManyValues = new(
        TestDbProvider.Sqlite,
        "SQLITE-CONTAINS-PARAMETER-LIMIT: SQLite gets a parameter for every value of a plain Contains, and takes at most 32,766",
        ex => ex is SqliteException { SqliteErrorCode: 1 } sqlite && sqlite.Message.Contains("too many SQL variables", StringComparison.Ordinal));}
