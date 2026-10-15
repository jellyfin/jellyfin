using System;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

public class PostgresWriteConflictClassificationTests
{
    private const string UniqueViolation = "23505";
    private const string SerializationFailure = "40001";
    private const string DeadlockDetected = "40P01";

    private readonly IJellyfinDatabaseProvider _provider = new PostgresDatabaseProvider(new CollectingLogger<PostgresDatabaseProvider>());

    [Theory]
    [InlineData(UniqueViolation, true)]
    [InlineData(SerializationFailure, true)]
    [InlineData(DeadlockDetected, true)]
    [InlineData("23503", false)]
    [InlineData("23502", false)]
    [InlineData("23514", false)]
    [InlineData("23P01", false)]
    [InlineData("22001", false)]
    [InlineData("42P01", false)]
    [InlineData("42601", false)]
    [InlineData("40003", false)]
    [InlineData("57014", false)]
    [InlineData("08006", false)]
    [InlineData("53300", false)]
    public void IsRetryableWriteConflict_PostgresException_IsDecidedBySqlState(string sqlState, bool expectedRetryable)
    {
        var exception = CreatePostgresException(sqlState);

        Assert.Equal(expectedRetryable, _provider.IsRetryableWriteConflict(exception));
    }

    [Theory]
    [InlineData(UniqueViolation, true)]
    [InlineData(SerializationFailure, true)]
    [InlineData(DeadlockDetected, true)]
    [InlineData("23503", false)]
    [InlineData("23502", false)]
    public void IsRetryableWriteConflict_ViolationWrappedInDbUpdateException_IsClassifiedByTheInnerException(
        string sqlState,
        bool expectedRetryable)
    {
        var wrapped = new DbUpdateException("An error occurred while saving the entity changes.", CreatePostgresException(sqlState));

        Assert.Equal(expectedRetryable, _provider.IsRetryableWriteConflict(wrapped));
    }

    [Fact]
    public void IsRetryableWriteConflict_ViolationNestedDeeper_IsFound()
    {
        var nested = new InvalidOperationException(
            "outer",
            new DbUpdateException("middle", CreatePostgresException(UniqueViolation)));

        Assert.True(_provider.IsRetryableWriteConflict(nested));
    }

    [Fact]
    public void IsRetryableWriteConflict_ConnectionFailure_IsNotRetryable()
    {
        var exception = new NpgsqlException("Exception while reading from stream", new System.IO.IOException("Connection reset by peer"));

        Assert.False(_provider.IsRetryableWriteConflict(exception));
        Assert.False(_provider.IsRetryableWriteConflict(new DbUpdateException("save failed", exception)));
    }

    [Fact]
    public void IsRetryableWriteConflict_TimeoutWithoutAPostgresException_IsNotRetryable()
    {
        Assert.False(_provider.IsRetryableWriteConflict(new TimeoutException("Timeout during reading attempt")));
    }

    [Fact]
    public void IsRetryableWriteConflict_UnrelatedException_IsNotRetryable()
    {
        Assert.False(_provider.IsRetryableWriteConflict(new InvalidOperationException("boom")));
        Assert.False(_provider.IsRetryableWriteConflict(new DbUpdateException("save failed", new InvalidOperationException("boom"))));
    }

    [Fact]
    public void IsRetryableWriteConflict_NullException_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _provider.IsRetryableWriteConflict(null!));
    }

    private static PostgresException CreatePostgresException(string sqlState)
        => new("conflict", "ERROR", "ERROR", sqlState);
}
