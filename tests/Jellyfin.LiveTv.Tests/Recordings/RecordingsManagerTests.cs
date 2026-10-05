using System.IO;
using Jellyfin.LiveTv.Recordings;
using Xunit;

namespace Jellyfin.LiveTv.Tests.Recordings;

public static class RecordingsManagerTests
{
    [Theory]
    // Episode title contains a "." — previously Path.ChangeExtension would truncate at it.
    [InlineData(
        "/recordings",
        "My Show S01E02 Mr. Smith Goes Home",
        ".ts",
        1,
        "/recordings/My Show S01E02 Mr. Smith Goes Home - 1.ts")]
    [InlineData(
        "/recordings",
        "My Show S01E02 Mr. Smith Goes Home",
        ".ts",
        3,
        "/recordings/My Show S01E02 Mr. Smith Goes Home - 3.ts")]
    // Each call is independent — index is explicit, no suffix accumulation.
    [InlineData(
        "/recordings",
        "My Show S01E02 Mr. Smith Goes Home",
        ".ts",
        2,
        "/recordings/My Show S01E02 Mr. Smith Goes Home - 2.ts")]
    // Plain title without "." — baseline correctness.
    [InlineData(
        "/recordings",
        "Simple Episode",
        ".ts",
        1,
        "/recordings/Simple Episode - 1.ts")]
    [InlineData(
        "/recordings",
        "Simple Episode",
        ".ts",
        2,
        "/recordings/Simple Episode - 2.ts")]
    public static void BuildSuffixedPath_PreservesFullTitle(
        string parent,
        string baseName,
        string extension,
        int index,
        string expected)
    {
        var result = RecordingsManager.BuildSuffixedPath(parent, baseName, extension, index);
        Assert.Equal(expected, result);
    }
}
