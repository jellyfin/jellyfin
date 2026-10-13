using System;
using Jellyfin.Database.Testing;
using Npgsql;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

public class KnownGapsTests
{
    [Theory]
    [InlineData("invalid byte sequence for encoding \"UTF8\": 0x00")]
    [InlineData("séquence d'octets invalide pour l'encodage « UTF8 » : 0x00")]
    public void NulInExactMatchParameter_NulCharacterError_Matches(string messageText)
    {
        var exception = new InvalidOperationException("query failed", CreatePostgresException("22021", messageText));

        Assert.True(KnownGaps.NulInExactMatchParameter.Matches(exception));
    }

    [Theory]
    [InlineData("42883", "function lower(integer) does not exist")]
    [InlineData("22P02", "invalid input syntax for type uuid")]
    [InlineData("23505", "duplicate key value violates unique constraint")]
    public void NulInExactMatchParameter_UnrelatedPostgresError_DoesNotMatch(string sqlState, string messageText)
    {
        Assert.False(KnownGaps.NulInExactMatchParameter.Matches(CreatePostgresException(sqlState, messageText)));
    }

    [Fact]
    public void NulInExactMatchParameter_UnrelatedErrorOnTheSelectedProvider_IsNotSkipped()
    {
        var gap = KnownGaps.NulInExactMatchParameter with { Provider = TestDatabaseEnvironment.Provider };

        Assert.Throws<PostgresException>(
            () => KnownGap.On(gap, () => throw CreatePostgresException("42883", "function lower(integer) does not exist")));
    }

    private static PostgresException CreatePostgresException(string sqlState, string messageText)
        => new(messageText, "ERROR", "ERROR", sqlState);
}
