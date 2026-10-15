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
    [InlineData("[", "%\\[%")]
    [InlineData("[ab]", "%\\[ab]%")]
    [InlineData("[^A]", "%\\[^a]%")]
    [InlineData("]", "%]%")]
    [InlineData("\\[", "%\\\\\\[%")]
    public void ContainsIgnoreAsciiCase_OpeningBracket_IsEscaped(string term, string expected)
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

    [Theory]
    [InlineData("a\0b", "%ab%")]
    [InlineData("\0world", "%world%")]
    [InlineData("WORLD\0", "%world%")]
    [InlineData("\0", "%%")]
    public void ContainsIgnoreAsciiCase_Nul_IsDropped(string term, string expected)
    {
        Assert.Equal(expected, LikePatterns.ContainsIgnoreAsciiCase(term));
    }

    [Fact]
    public void ContainsIgnoreAsciiCase_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => LikePatterns.ContainsIgnoreAsciiCase(null!));
    }

    [Theory]
    [InlineData("", "%")]
    [InlineData("Alpha", "alpha%")]
    [InlineData("ALPHA B", "alpha b%")]
    [InlineData("ÉMILE", "Émile%")]
    [InlineData("é", "é%")]
    [InlineData("\u212A", "\u212A%")]
    public void StartsWithIgnoreAsciiCase_Term_LowersAsciiLettersAndEndsWithAWildcard(string term, string expected)
    {
        Assert.Equal(expected, LikePatterns.StartsWithIgnoreAsciiCase(term));
    }

    [Theory]
    [InlineData("AC\\DC", "ac\\\\dc%")]
    [InlineData("\\", "\\\\%")]
    [InlineData("50%", "50\\%%")]
    [InlineData("a_b", "a\\_b%")]
    [InlineData("%", "\\%%")]
    [InlineData("_", "\\_%")]
    [InlineData("\\%_", "\\\\\\%\\_%")]
    public void StartsWithIgnoreAsciiCase_BackslashPercentAndUnderscore_AreEscaped(string term, string expected)
    {
        Assert.Equal(expected, LikePatterns.StartsWithIgnoreAsciiCase(term));
    }

    [Theory]
    [InlineData("[", "\\[%")]
    [InlineData("[A]_", "\\[a]\\_%")]
    [InlineData("]", "]%")]
    public void StartsWithIgnoreAsciiCase_OpeningBracket_IsEscaped(string term, string expected)
    {
        Assert.Equal(expected, LikePatterns.StartsWithIgnoreAsciiCase(term));
    }

    [Theory]
    [InlineData("a\0b", "ab%")]
    [InlineData("\0", "%")]
    public void StartsWithIgnoreAsciiCase_Nul_IsDropped(string term, string expected)
    {
        Assert.Equal(expected, LikePatterns.StartsWithIgnoreAsciiCase(term));
    }

    [Fact]
    public void StartsWithIgnoreAsciiCase_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => LikePatterns.StartsWithIgnoreAsciiCase(null!));
    }
}
