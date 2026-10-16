using System;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

public class PostgresDataErrorClassificationTests
{
    private readonly IJellyfinDatabaseProvider _provider = new PostgresDatabaseProvider(new CollectingLogger<PostgresDatabaseProvider>());

    [Theory]
    [InlineData("22001", true)] // string_data_right_truncation
    [InlineData("22021", true)] // character_not_in_repertoire
    [InlineData("22P02", true)] // invalid_text_representation
    [InlineData("23502", true)] // not_null_violation
    [InlineData("23503", true)] // foreign_key_violation
    [InlineData("23505", true)] // unique_violation, which the retry policy takes first
    [InlineData("23514", true)] // check_violation
    [InlineData("54000", true)] // program_limit_exceeded, as for an index row over the btree maximum
    [InlineData("P0001", true)] // raise_exception
    [InlineData("08006", false)] // connection_failure
    [InlineData("40001", false)] // serialization_failure
    [InlineData("40P01", false)] // deadlock_detected
    [InlineData("53300", false)] // too_many_connections
    [InlineData("55P03", false)] // lock_not_available
    [InlineData("57014", false)] // query_canceled
    [InlineData("57P01", false)] // admin_shutdown
    [InlineData("42P01", false)] // undefined_table
    [InlineData("58030", false)] // io_error
    public void IsDataError_PostgresException_IsDecidedBySqlState(string sqlState, bool expected)
    {
        var exception = new PostgresException("refused", "ERROR", "ERROR", sqlState);

        Assert.Equal(expected, _provider.IsDataError(exception));
        Assert.Equal(expected, _provider.IsDataError(new DbUpdateException("An error occurred while saving the entity changes.", exception)));
    }

    [Fact]
    public void IsDataError_ConnectionOrTimeout_IsNotDataError()
    {
        Assert.False(_provider.IsDataError(new NpgsqlException("Exception while reading from stream", new System.IO.IOException("Connection reset by peer"))));
        Assert.False(_provider.IsDataError(new NpgsqlException("Exception while reading from stream", new TimeoutException("Timeout during reading attempt"))));
        Assert.False(_provider.IsDataError(new DbUpdateException("save failed", new InvalidOperationException("boom"))));
    }

    [Fact]
    public void IsDataError_NullException_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _provider.IsDataError(null!));
    }
}
