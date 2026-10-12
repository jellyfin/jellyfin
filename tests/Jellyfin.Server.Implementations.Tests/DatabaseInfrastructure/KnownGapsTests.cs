using System;
using Jellyfin.Database.Testing;
using Npgsql;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

public class KnownGapsTests
{
    [Theory]
    [InlineData("function min(uuid) does not exist")]
    [InlineData("function max(uuid) does not exist")]
    [InlineData("la fonction min(uuid) n'existe pas")]
    public void GuidAggregates_MissingUuidAggregate_Matches(string messageText)
    {
        var exception = new InvalidOperationException("query failed", CreatePostgresException("42883", messageText));

        Assert.True(KnownGaps.GuidAggregates.Matches(exception));
    }

    [Theory]
    [InlineData("42883", "function lower(integer) does not exist")]
    [InlineData("42883", "function min(text) does not exist")]
    [InlineData("42883", "operator does not exist: uuid = integer")]
    [InlineData("42P01", "function min(uuid) does not exist")]
    public void GuidAggregates_UnrelatedPostgresError_DoesNotMatch(string sqlState, string messageText)
    {
        Assert.False(KnownGaps.GuidAggregates.Matches(CreatePostgresException(sqlState, messageText)));
    }

    [Fact]
    public void GuidAggregates_UnrelatedUndefinedFunctionOnTheSelectedProvider_IsNotSkipped()
    {
        var gap = KnownGaps.GuidAggregates with { Provider = TestDatabaseEnvironment.Provider };

        Assert.Throws<PostgresException>(
            () => KnownGap.On(gap, () => throw CreatePostgresException("42883", "function lower(integer) does not exist")));
    }

    [Fact]
    public void PostgresMigrations_MessageOfTheBackend_Matches()
    {
        var exception = Assert.Throws<NotSupportedException>(
            () => PostgresTestBackend.Instance.Create(TestSchemaSource.Migrations, []));

        Assert.True(KnownGaps.PostgresMigrations.Matches(exception));
    }

    [Fact]
    public void PostgresMigrations_OtherNotSupportedException_DoesNotMatch()
    {
        Assert.False(KnownGaps.PostgresMigrations.Matches(new NotSupportedException("Specified method is not supported.")));
    }

    [Fact]
    public void PostgresMigrations_OtherNotSupportedExceptionOnTheSelectedProvider_IsNotSkipped()
    {
        var gap = KnownGaps.PostgresMigrations with { Provider = TestDatabaseEnvironment.Provider };

        Assert.Throws<NotSupportedException>(
            () => KnownGap.On(gap, () => throw new NotSupportedException("Specified method is not supported.")));
    }

    private static PostgresException CreatePostgresException(string sqlState, string messageText)
        => new(messageText, "ERROR", "ERROR", sqlState);
}
