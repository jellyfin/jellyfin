using System;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public class MatchNormalizerTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("Hello World", "hello world")]
    [InlineData("Émile Zola", "emile zola")]
    [InlineData("ÉMILE", "emile")]
    [InlineData("AC\\DC", "ac dc")]
    [InlineData("Hello,   World!", "hello world")]
    [InlineData("---", "")]
    [InlineData("a\0b", "ab")]
    [InlineData("日本語", "日本語")]
    public void CleanOriginalTitle_Text_IsInTheFormOfCleanName(string? originalTitle, string? expected)
    {
        Assert.Equal(expected, MatchNormalizer.CleanOriginalTitle(originalTitle));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("Bob", "bob")]
    [InlineData("Émile", "émile")]
    [InlineData("ÀÉÎ", "àéî")]
    [InlineData("A\0B", "ab")]
    public void NameLower_Name_IsInLowerCase(string name, string expected)
    {
        Assert.Equal(expected, MatchNormalizer.NameLower(name));
    }

    [Fact]
    public void NameLower_NullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => MatchNormalizer.NameLower(null!));
    }

    [Theory]
    [InlineData("ÉMILE", "%emile%")]
    [InlineData("émile", "%emile%")]
    [InlineData("Al%ga", "%al%ga%")]
    [InlineData("Al_ha", "%al_ha%")]
    [InlineData("50% off", "%50% off%")]
    [InlineData("AC\\DC", "%ac dc%")]
    [InlineData("  hello   world ", "%hello world%")]
    [InlineData("a'b[c]^d", "%a b c d%")]
    [InlineData("\0x", "%x%")]
    [InlineData("%", "%%%")]
    [InlineData("_", "%_%")]
    public void ContainsCleanOriginalTitlePattern_Term_KeepsOnlyWildcardsOfLike(string term, string expected)
    {
        Assert.Equal(expected, MatchNormalizer.ContainsCleanOriginalTitlePattern(term));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-")]
    [InlineData("\\")]
    [InlineData("[ ]^")]
    [InlineData("\0")]
    public void ContainsCleanOriginalTitlePattern_TermWithoutLetterDigitOrWildcard_HasNothingToLookFor(string term)
    {
        Assert.Null(MatchNormalizer.ContainsCleanOriginalTitlePattern(term));
    }

    [Theory]
    [InlineData("Émile Zola", "émile")]
    [InlineData("Hello, World!", "o w")]
    [InlineData("AC\\DC", "ac_dc")]
    [InlineData("Alpha Omega", "al%ga")]
    public void ContainsCleanOriginalTitlePattern_TermInTheTitle_MatchesTheCleanTitleAsLikeWould(string title, string term)
    {
        var clean = MatchNormalizer.CleanOriginalTitle(title)!;
        var pattern = MatchNormalizer.ContainsCleanOriginalTitlePattern(term)!;

        var regex = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("%", ".*", StringComparison.Ordinal)
            .Replace("_", ".", StringComparison.Ordinal) + "$";
        Assert.Matches(regex, clean);
    }
}
