using System;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Xunit.Sdk;

namespace Jellyfin.Database.Testing;

/// <summary>
/// The known differences between providers. An entry is deleted when the difference is closed, which breaks the
/// build wherever a test is still marked with it.
/// </summary>
public static class KnownGaps
{
    /// <summary>
    /// The message a test throws when the server orders text by UTF-16 code unit.
    /// </summary>
    public const string Utf16OrderMessage = "Ordered by UTF-16 code unit: z, U+1F600, U+E000, U+FFFD";

    private const string ActualPrefix = "Actual:";

    private const string SortNameGreaterOutcome = "\"Émile, Öland, émile, ölfass\"";

    private const string SortNameLessOutcome = "\"Emile\"";

    public static readonly KnownGapDefinition NulInExactMatchParameter = new(
        TestDbProvider.Postgres,
        "PG-NUL-PARAMETER",
        "PostgreSQL rejects a NUL character in any string parameter",
        ex => PostgresErrors.HasSqlState(ex, "22021"));

    // An index entry that is not clustered holds at most 1700 bytes of key on SQL Server, 850 characters of a string that
    // is indexed without a bound of its own. The server refuses such a row with error 1946 and nothing else.
    public static readonly KnownGapDefinition IndexKeyOverTheLimitOnSqlServer = new(
        TestDbProvider.SqlServer,
        "SQLSERVER-INDEX-KEY-1700",
        "SQL Server refuses a row whose key of an index that is not clustered takes more than 1700 bytes",
        ex => HasSqlServerError(ex, 1946));

    // The strings that are indexed without a bound of their own are nvarchar(4000) on SQL Server, the longest type an index
    // takes, so a longer value is refused at the column (2628), before any index sees it.
    public static readonly KnownGapDefinition PathOverTheColumnOnSqlServer = new(
        TestDbProvider.SqlServer,
        "SQLSERVER-PATH-OVER-4000",
        "SQL Server refuses an item whose path is longer than the 4000 characters of its column",
        ex => HasSqlServerError(ex, 2628) && ex.ToString().Contains("BaseItems", StringComparison.Ordinal) && ex.ToString().Contains("'Path'", StringComparison.Ordinal));

    // SQL Server compares strings with ANSI padding under every collation, so values that differ only by trailing spaces
    // are equal and collide in the unique index of item values.
    public static readonly KnownGapDefinition TrailingSpacesOnSqlServer = new(
        TestDbProvider.SqlServer,
        "SQLSERVER-TRAILING-SPACE",
        "SQL Server compares strings that differ only by trailing spaces as equal",
        ex => HasSqlServerError(ex, 2601) && ex.ToString().Contains("IX_ItemValues_Type_Value", StringComparison.Ordinal));

    // SQL Server orders nvarchar by UTF-16 code unit under its binary collation, so a character outside the Basic
    // Multilingual Plane sorts before U+E000 to U+FFFF. The test throws this message for exactly that order.
    public static readonly KnownGapDefinition Utf16OrderOnSqlServer = new(
        TestDbProvider.SqlServer,
        "SQLSERVER-UTF16-ORDER",
        "SQL Server orders text by UTF-16 code unit, not by code point",
        ex => ex is InvalidOperationException && string.Equals(ex.Message, Utf16OrderMessage, StringComparison.Ordinal));

    public static readonly KnownGapDefinition ContainsOverManyValues = new(
        TestDbProvider.Sqlite,
        "SQLITE-CONTAINS-PARAMETER-LIMIT",
        "SQLite gets a parameter for every value of a plain Contains, and takes at most 32,766",
        ex => ex is SqliteException { SqliteErrorCode: 1 } sqlite && sqlite.Message.Contains("too many SQL variables", StringComparison.Ordinal));

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

    // LOWER folds letters outside ASCII under the binary collation of SQL Server too, with the same lists.
    public static readonly KnownGapDefinition SortNameRangeLowersNonAsciiOnSqlServer = new(
        TestDbProvider.SqlServer,
        "SQLSERVER-SORTNAME-RANGE-CASE",
        "The sort name range filters lower-case non-ASCII letters of the sort name on SQL Server",
        IsSortNameRangeOutcome);

    private static bool HasSqlServerError(Exception ex, int number)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is SqlException sqlException)
            {
                return sqlException.Errors.Cast<SqlError>().Any(error => error.Number == number);
            }
        }

        return false;
    }

    // The three servers return the same lists for the two tests that use this gap. The lists are compared in full, so a
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
}
