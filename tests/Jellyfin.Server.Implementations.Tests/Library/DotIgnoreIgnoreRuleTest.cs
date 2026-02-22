using Emby.Server.Implementations.Library;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class DotIgnoreIgnoreRuleTest
{
    private static readonly string[] _rule1 = ["SPs"];
    private static readonly string[] _rule2 = ["SPs", "!thebestshot.mkv"];
    private static readonly string[] _rule3 = ["*.txt", @"{\colortbl;\red255\green255\blue255;}", "videos/", @"\invalid\escape\sequence", "*.mkv"];
    private static readonly string[] _rule4 = [@"{\colortbl;\red255\green255\blue255;}", @"\invalid\escape\sequence"];

    public static TheoryData<string[], string, bool, bool> CheckIgnoreRulesTestData =>
        new()
        {
            // Basic pattern matching
            { _rule1, "f:/cd/sps/ffffff.mkv", false, true },
            { _rule1, "cd/sps/ffffff.mkv", false, true },
            { _rule1, "/cd/sps/ffffff.mkv", false, true },

            // Negate pattern
            { _rule2, "f:/cd/sps/ffffff.mkv", false, true },
            { _rule2, "cd/sps/ffffff.mkv", false, true },
            { _rule2, "/cd/sps/ffffff.mkv", false, true },
            { _rule2, "f:/cd/sps/thebestshot.mkv", false, false },
            { _rule2, "cd/sps/thebestshot.mkv", false, false },
            { _rule2, "/cd/sps/thebestshot.mkv", false, false },

            // Mixed valid and invalid patterns - skips invalid, applies valid
            { _rule3, "test.txt", false, true },
            { _rule3, "videos/movie.mp4", false, true },
            { _rule3, "movie.mkv", false, true },
            { _rule3, "test.mp3", false, false },

            // Only invalid patterns - falls back to ignore all
            { _rule4, "any-file.txt", false, true },
            { _rule4, "any/path/to/file.mkv", false, true },
        };

    public static TheoryData<string[], string, bool, bool> WindowsPathNormalizationTestData =>
        new()
        {
            // Windows paths with backslashes - should match when normalizePath is true
            { _rule1, @"C:\cd\sps\ffffff.mkv", false, true },
            { _rule1, @"D:\media\sps\movie.mkv", false, true },
            { _rule1, @"\\server\share\sps\file.mkv", false, true },

            // Negate pattern with Windows paths
            { _rule2, @"C:\cd\sps\ffffff.mkv", false, true },
            { _rule2, @"C:\cd\sps\thebestshot.mkv", false, false },

            // Directory matching with Windows paths
            { _rule3, @"C:\videos\movie.mp4", false, true },
            { _rule3, @"D:\documents\test.txt", false, true },
            { _rule3, @"E:\music\song.mp3", false, false },
        };

    [Theory]
    [MemberData(nameof(CheckIgnoreRulesTestData))]
    public void CheckIgnoreRules_ReturnsExpectedResult(string[] rules, string path, bool isDirectory, bool expectedIgnored)
    {
        Assert.Equal(expectedIgnored, DotIgnoreIgnoreRule.CheckIgnoreRules(path, rules, isDirectory));
    }

    [Theory]
    [MemberData(nameof(WindowsPathNormalizationTestData))]
    public void CheckIgnoreRules_WithWindowsPaths_NormalizesBackslashes(string[] rules, string path, bool isDirectory, bool expectedIgnored)
    {
        // With normalizePath=true, backslashes should be converted to forward slashes
        Assert.Equal(expectedIgnored, DotIgnoreIgnoreRule.CheckIgnoreRules(path, rules, isDirectory, normalizePath: true));
    }

    [Theory]
    [InlineData(@"C:\cd\sps\ffffff.mkv")]
    [InlineData(@"D:\media\sps\movie.mkv")]
    public void CheckIgnoreRules_WithWindowsPaths_WithoutNormalization_DoesNotMatch(string path)
    {
        // Without normalization, Windows paths with backslashes won't match patterns expecting forward slashes
        Assert.False(DotIgnoreIgnoreRule.CheckIgnoreRules(path, _rule1, isDirectory: false, normalizePath: false));
    }
}
