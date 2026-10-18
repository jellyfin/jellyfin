using System.IO;
using Jellyfin.LiveTv.Recordings;
using Xunit;

namespace Jellyfin.LiveTv.Tests.Recordings;

public static class RecordingsManagerTests
{
    private static readonly string _dir = Path.GetTempPath();

    [Theory]
    // Episode title contains a "." — previously Path.ChangeExtension would truncate at it.
    [InlineData("My Show S01E02 Mr. Smith Goes Home", ".ts", 1)]
    [InlineData("My Show S01E02 Mr. Smith Goes Home", ".ts", 3)]
    // Each call is independent — index is explicit, no suffix accumulation.
    [InlineData("My Show S01E02 Mr. Smith Goes Home", ".ts", 2)]
    // Plain title without "." — baseline correctness.
    [InlineData("Simple Episode", ".ts", 1)]
    [InlineData("Simple Episode", ".ts", 2)]
    public static void BuildSuffixedPath_PreservesFullTitle(
        string baseName,
        string extension,
        int index)
    {
        var result = RecordingsManager.BuildSuffixedPath(_dir, baseName, extension, index);
        var fileName = Path.GetFileName(result);
        var expectedFileName = baseName + " - " + index + extension;

        Assert.Equal(expectedFileName, fileName);
        Assert.Equal(_dir, Path.GetDirectoryName(result) + Path.DirectorySeparatorChar);
    }

    [Fact]
    public static void BuildSuffixedPath_TrimsBaseNameWhenTooLong()
    {
        // A name right at the 250-byte RecordingHelper cap.
        var longName = new string('A', 250);
        var result = RecordingsManager.BuildSuffixedPath(_dir, longName, ".ts", 1);
        var fileName = Path.GetFileName(result);

        // The resulting filename must fit within 255 bytes.
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(fileName) <= 255);
        // And must end with the correct suffix.
        Assert.EndsWith(" - 1.ts", fileName);
    }

    [Fact]
    public static void BuildSuffixedPath_ReturnsDistinctPathsForEachIndex()
    {
        // Each loop iteration must produce a different path so the loop terminates.
        var paths = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        for (var i = 1; i <= 5; i++)
        {
            var path = RecordingsManager.BuildSuffixedPath(_dir, "Episode", ".ts", i);
            Assert.True(paths.Add(path), $"Index {i} produced a duplicate path: {path}");
        }
    }
}
