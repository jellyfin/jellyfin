using System.Text.RegularExpressions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class DotIgnoreIgnoreRuleTest
{
    [Fact]
    public void Test()
    {
        var ignore = new Ignore.Ignore();
        ignore.Add("SPs");
        Assert.True(ignore.IsIgnored("f:/cd/sps/ffffff.mkv"));
        Assert.True(ignore.IsIgnored("cd/sps/ffffff.mkv"));
        Assert.True(ignore.IsIgnored("/cd/sps/ffffff.mkv"));
    }

    [Fact]
    public void TestNegatePattern()
    {
        var ignore = new Ignore.Ignore();
        ignore.Add("SPs");
        ignore.Add("!thebestshot.mkv");
        Assert.True(ignore.IsIgnored("f:/cd/sps/ffffff.mkv"));
        Assert.True(ignore.IsIgnored("cd/sps/ffffff.mkv"));
        Assert.True(ignore.IsIgnored("/cd/sps/ffffff.mkv"));
        Assert.False(ignore.IsIgnored("f:/cd/sps/thebestshot.mkv"));
        Assert.False(ignore.IsIgnored("cd/sps/thebestshot.mkv"));
        Assert.False(ignore.IsIgnored("/cd/sps/thebestshot.mkv"));
    }

    [Fact]
    public void TestInvalidPatternThrowsRegexParseException()
    {
        // This test verifies that invalid patterns throw RegexParseException,
        // which is the exception we catch in DotIgnoreIgnoreRule.CheckIgnoreRules
        var ignore = new Ignore.Ignore();

        // Pattern with invalid regex escape sequence (like RTF content)
        var invalidPattern = @"{\colortbl;\red255\green255\blue255;}";

        Assert.Throws<RegexParseException>(() => ignore.Add(invalidPattern));
    }

    [Fact]
    public void TestValidPatternsWorkAfterSkippingInvalid()
    {
        // Simulates the behavior in CheckIgnoreRules where we skip invalid patterns
        var ignore = new Ignore.Ignore();
        var patterns = new[]
        {
            "*.txt",
            @"{\colortbl;\red255\green255\blue255;}", // Invalid - RTF content
            "videos/",
            @"\invalid\escape\sequence", // Invalid
            "*.mkv"
        };

        foreach (var pattern in patterns)
        {
            try
            {
                ignore.Add(pattern);
            }
            catch (RegexParseException)
            {
                // Skip invalid patterns (as we do in the actual implementation)
            }
        }

        // Valid patterns should still work
        Assert.True(ignore.IsIgnored("test.txt"));
        Assert.True(ignore.IsIgnored("videos/movie.mp4"));
        Assert.True(ignore.IsIgnored("movie.mkv"));

        // Non-matching paths should not be ignored
        Assert.False(ignore.IsIgnored("test.mp3"));
    }

    [Fact]
    public void TestFallbackToIgnoreAllWhenNoValidPatterns()
    {
        // When all patterns are invalid, we should fall back to ignoring everything
        var ignore = new Ignore.Ignore();
        var invalidPatterns = new[]
        {
            @"{\colortbl;\red255\green255\blue255;}",
            @"\invalid\escape\sequence"
        };

        var validRulesAdded = 0;
        foreach (var pattern in invalidPatterns)
        {
            try
            {
                ignore.Add(pattern);
                validRulesAdded++;
            }
            catch (RegexParseException)
            {
                // Skip invalid patterns
            }
        }

        // No valid rules were added
        Assert.Equal(0, validRulesAdded);

        // Fall back to ignoring everything
        if (validRulesAdded == 0)
        {
            ignore.Add("*");
        }

        // Now everything should be ignored
        Assert.True(ignore.IsIgnored("any-file.txt"));
        Assert.True(ignore.IsIgnored("any/path/to/file.mkv"));
    }
}
