using System;
using Jellyfin.Database.Implementations;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

public class LikePatternsTests
{
    [Theory]
    [InlineData("", "%%")]
    [InlineData("world", "%world%")]
    [InlineData("WORLD", "%world%")]
    [InlineData("wOrLd", "%world%")]
    [InlineData("a b", "%a b%")]
    public void ContainsIgnoreAsciiCase_AsciiLetters_AreLowered(string term, string expected)
    {
        Assert.Equal(expected, LikePatterns.ContainsIgnoreAsciiCase(term));
    }

    [Theory]
    [InlineData("AC\\DC", "%ac\\\\dc%")]
    [InlineData("\\", "%\\\\%")]
    [InlineData("\\\\", "%\\\\\\\\%")]
    [InlineData("a\\", "%a\\\\%")]
    public void ContainsIgnoreAsciiCase_Backslash_IsDoubled(string term, string expected)
    {
        Assert.Equal(expected, LikePatterns.ContainsIgnoreAsciiCase(term));
    }

    [Theory]
    [InlineData("100%", "%100%%")]
    [InlineData("a_b", "%a_b%")]
    [InlineData("%", "%%%")]
    [InlineData("_", "%_%")]
    public void ContainsIgnoreAsciiCase_PercentAndUnderscore_AreLeftAsWildcards(string term, string expected)
    {
        Assert.Equal(expected, LikePatterns.ContainsIgnoreAsciiCase(term));
    }

    [Theory]
    [InlineData("ÉMILE", "%Émile%")]
    [InlineData("émile", "%émile%")]
    [InlineData("İSTANBUL", "%İstanbul%")]
    [InlineData("K", "%K%")]
    public void ContainsIgnoreAsciiCase_NonAsciiLetters_AreLeftAsTheyAre(string term, string expected)
    {
        Assert.Equal(expected, LikePatterns.ContainsIgnoreAsciiCase(term));
    }

    [Fact]
    public void ContainsIgnoreAsciiCase_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => LikePatterns.ContainsIgnoreAsciiCase(null!));
    }
}
