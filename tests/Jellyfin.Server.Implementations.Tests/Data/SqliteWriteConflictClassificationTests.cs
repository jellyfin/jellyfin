using System;
using Jellyfin.Database.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

/// <summary>
/// The exceptions here come from real constraint violations on an in-memory database, so the extended result codes
/// the provider classifies on are the ones SQLite reports, not ones assumed by the test.
/// </summary>
public sealed class SqliteWriteConflictClassificationTests : DatabaseTestFixture
{
    private const int ConstraintCheck = 275;
    private const int ConstraintForeignKey = 787;
    private const int ConstraintNotNull = 1299;
    private const int ConstraintPrimaryKey = 1555;
    private const int ConstraintUnique = 2067;

    public SqliteWriteConflictClassificationTests()
    {
        Execute("PRAGMA foreign_keys = ON");
        Execute("CREATE TABLE Parents (Id INTEGER NOT NULL PRIMARY KEY)");
        Execute(
            """
            CREATE TABLE Children (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL UNIQUE,
                Label TEXT NOT NULL,
                ParentId INTEGER NOT NULL REFERENCES Parents (Id),
                Size INTEGER NOT NULL CHECK (Size >= 0))
            """);
        Execute("INSERT INTO Parents (Id) VALUES (1)");
        Execute("INSERT INTO Children (Id, Name, Label, ParentId, Size) VALUES ('a', 'alpha', 'x', 1, 1)");
    }

    [ProviderTheory(TestDbProvider.Sqlite)]
    [InlineData("INSERT INTO Children VALUES ('b', 'alpha', 'x', 1, 1)", ConstraintUnique, true)]
    [InlineData("INSERT INTO Children VALUES ('a', 'beta', 'x', 1, 1)", ConstraintPrimaryKey, true)]
    [InlineData("INSERT INTO Children VALUES ('b', 'beta', 'x', 2, 1)", ConstraintForeignKey, false)]
    [InlineData("INSERT INTO Children VALUES ('b', 'beta', NULL, 1, 1)", ConstraintNotNull, false)]
    [InlineData("INSERT INTO Children VALUES ('b', 'beta', 'x', 1, -1)", ConstraintCheck, false)]
    public void IsRetryableWriteConflict_ConstraintViolation_MatchesTheViolatedConstraint(
        string statement,
        int expectedExtendedCode,
        bool expectedRetryable)
    {
        var exception = Assert.Throws<SqliteException>(() => Execute(statement));
        Assert.Equal(expectedExtendedCode, exception.SqliteExtendedErrorCode);

        Assert.Equal(expectedRetryable, DatabaseProvider.IsRetryableWriteConflict(exception));
    }

    [ProviderTheory(TestDbProvider.Sqlite)]
    [InlineData("INSERT INTO Children VALUES ('b', 'alpha', 'x', 1, 1)", true)]
    [InlineData("INSERT INTO Children VALUES ('a', 'beta', 'x', 1, 1)", true)]
    [InlineData("INSERT INTO Children VALUES ('b', 'beta', 'x', 2, 1)", false)]
    [InlineData("INSERT INTO Children VALUES ('b', 'beta', NULL, 1, 1)", false)]
    public void IsRetryableWriteConflict_ViolationWrappedInDbUpdateException_IsClassifiedByTheInnerException(
        string statement,
        bool expectedRetryable)
    {
        var inner = Assert.Throws<SqliteException>(() => Execute(statement));
        var wrapped = new DbUpdateException("An error occurred while saving the entity changes.", inner);

        Assert.Equal(expectedRetryable, DatabaseProvider.IsRetryableWriteConflict(wrapped));
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void IsRetryableWriteConflict_ViolationNestedDeeper_IsFound()
    {
        var inner = Assert.Throws<SqliteException>(() => Execute("INSERT INTO Children VALUES ('b', 'alpha', 'x', 1, 1)"));
        var nested = new InvalidOperationException(
            "outer",
            new DbUpdateException("middle", inner));

        Assert.True(DatabaseProvider.IsRetryableWriteConflict(nested));
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void IsRetryableWriteConflict_UnrelatedExceptions_AreNotRetryable()
    {
        Assert.False(DatabaseProvider.IsRetryableWriteConflict(new InvalidOperationException("unique")));
        Assert.False(DatabaseProvider.IsRetryableWriteConflict(new DbUpdateException("unique")));
        Assert.False(DatabaseProvider.IsRetryableWriteConflict(new DbUpdateException("wrapped", new TimeoutException())));
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void IsRetryableWriteConflict_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => DatabaseProvider.IsRetryableWriteConflict(null!));
    }

    private void Execute(string sql)
    {
        using var context = CreateDbContext();
        context.Database.ExecuteSqlRaw(sql);
    }
}
