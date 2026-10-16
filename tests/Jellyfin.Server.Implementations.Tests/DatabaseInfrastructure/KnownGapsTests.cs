using System;
using System.Linq;
using System.Reflection;
using Jellyfin.Database.Testing;
using Npgsql;
using Xunit;
using Xunit.Sdk;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

public class KnownGapsTests
{
    public static TheoryData<KnownGapDefinition> SortNameRangeGaps => new()
    {
        KnownGaps.SortNameRangeLowersNonAsciiOnMySql,
        KnownGaps.SortNameRangeLowersNonAsciiOnMariaDb
    };

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

    [Theory]
    [MemberData(nameof(SortNameRangeGaps))]
    public void SortNameRangeGap_TheKnownLists_Match(KnownGapDefinition gap)
    {
        var greater = Assert.ThrowsAny<Exception>(
            () => AssertHelpers.AssertSameNames(new[] { "émile", "ölfass" }, new[] { "Émile", "Öland", "émile", "ölfass" }));
        var less = Assert.ThrowsAny<Exception>(
            () => AssertHelpers.AssertSameNames(new[] { "Emile", "Émile", "Öland" }, new[] { "Emile" }));

        Assert.True(gap.Matches(greater));
        Assert.True(gap.Matches(less));
    }

    [Theory]
    [MemberData(nameof(SortNameRangeGaps))]
    public void SortNameRangeGap_AnyOtherListOrFailure_DoesNotMatch(KnownGapDefinition gap)
    {
        var otherList = Assert.ThrowsAny<Exception>(
            () => AssertHelpers.AssertSameNames(new[] { "émile", "ölfass" }, new[] { "Émile", "émile" }));
        var emptyList = Assert.ThrowsAny<Exception>(
            () => AssertHelpers.AssertSameNames(new[] { "Emile" }, Array.Empty<string>()));
        var otherAssertion = Assert.ThrowsAny<Exception>(() => Assert.True(false));

        Assert.IsAssignableFrom<IAssertionException>(otherList);
        Assert.IsAssignableFrom<IAssertionException>(otherAssertion);
        Assert.False(gap.Matches(otherList));
        Assert.False(gap.Matches(emptyList));
        Assert.False(gap.Matches(otherAssertion));
        Assert.False(gap.Matches(new InvalidOperationException("boom")));
    }

    [Fact]
    public void EveryGap_HasAnIdThatIsShortStableAndUnique()
    {
        var gaps = typeof(KnownGaps)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(KnownGapDefinition))
            .Select(f => (KnownGapDefinition)f.GetValue(null)!)
            .ToList();

        Assert.NotEmpty(gaps);
        Assert.All(gaps, gap =>
        {
            Assert.Matches("^[A-Z0-9]+(-[A-Z0-9]+)+$", gap.Id);
            Assert.InRange(gap.Id.Length, 1, 32);
        });
        Assert.Equal(gaps.Count, gaps.Select(g => g.Id).Distinct(StringComparer.Ordinal).Count());
    }

    private static PostgresException CreatePostgresException(string sqlState, string messageText)
        => new(messageText, "ERROR", "ERROR", sqlState);
}
