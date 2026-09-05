using System;
using System.IO;
using MediaBrowser.Providers.Books;
using Xunit;

namespace Jellyfin.Providers.Tests.Books;

public class AudioBookCueChapterParserTests
{
    private const long TicksPerSecond = 10_000_000L;
    private const long TicksPerFrame = TicksPerSecond / 75;

    [Fact]
    public void ParseCueSidecar_NoCueFile_ReturnsEmpty()
    {
        using var dir = new TempDir();
        Assert.Empty(AudioBookCueChapterParser.ParseCueSidecar(dir.File("book.mp3")));
    }

    [Fact]
    public void ParseCueSidecar_SameNamedCue_ParsesTracksWithTimestamps()
    {
        using var dir = new TempDir();
        var audioPath = dir.File("book.mp3");
        dir.Write("book.cue", Cue(
            "FILE \"book.mp3\" MP3",
            "  TRACK 01 AUDIO",
            "    TITLE \"Opening Credits\"",
            "    INDEX 01 00:00:00",
            "  TRACK 02 AUDIO",
            "    TITLE \"Chapter 1\"",
            "    INDEX 01 01:30:00",
            "  TRACK 03 AUDIO",
            "    TITLE \"Chapter 2\"",
            "    INDEX 01 03:00:37"));

        var chapters = AudioBookCueChapterParser.ParseCueSidecar(audioPath);

        Assert.Equal(3, chapters.Count);

        Assert.Equal("Opening Credits", chapters[0].Name);
        Assert.Equal(0, chapters[0].StartPositionTicks);

        Assert.Equal("Chapter 1", chapters[1].Name);
        Assert.Equal(90L * TicksPerSecond, chapters[1].StartPositionTicks);

        Assert.Equal("Chapter 2", chapters[2].Name);
        Assert.Equal((180L * TicksPerSecond) + (37L * TicksPerFrame), chapters[2].StartPositionTicks);
    }

    [Fact]
    public void ParseCueSidecar_SingleCueWithDifferentName_IsUsed()
    {
        using var dir = new TempDir();
        var audioPath = dir.File("audiobook.mp3");
        dir.Write("tracklist.cue", Cue(
            "TRACK 01 AUDIO",
            "  TITLE \"Only\"",
            "  INDEX 01 00:10:00"));

        var chapters = AudioBookCueChapterParser.ParseCueSidecar(audioPath);

        Assert.Single(chapters);
        Assert.Equal("Only", chapters[0].Name);
        Assert.Equal(10L * TicksPerSecond, chapters[0].StartPositionTicks);
    }

    [Fact]
    public void ParseCueSidecar_MultipleCuesNoNameMatch_ReturnsEmpty()
    {
        using var dir = new TempDir();
        var audioPath = dir.File("book.mp3");
        dir.Write("a.cue", Cue("TRACK 01 AUDIO", "  INDEX 01 00:00:00"));
        dir.Write("b.cue", Cue("TRACK 01 AUDIO", "  INDEX 01 00:00:00"));

        Assert.Empty(AudioBookCueChapterParser.ParseCueSidecar(audioPath));
    }

    [Fact]
    public void ParseCueSidecar_MalformedTimestamp_IsZero()
    {
        using var dir = new TempDir();
        var audioPath = dir.File("book.mp3");
        dir.Write("book.cue", Cue(
            "TRACK 01 AUDIO",
            "  TITLE \"Bad\"",
            "  INDEX 01 not:a:time"));

        var chapters = AudioBookCueChapterParser.ParseCueSidecar(audioPath);

        Assert.Single(chapters);
        Assert.Equal(0, chapters[0].StartPositionTicks);
    }

    private static string Cue(params string[] lines) => string.Join('\n', lines);

    private sealed class TempDir : IDisposable
    {
        private readonly string _path;

        public TempDir()
        {
            _path = Path.Combine(Path.GetTempPath(), "jf-cue-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_path);
        }

        public string File(string name) => Path.Combine(_path, name);

        public void Write(string name, string content) => System.IO.File.WriteAllText(File(name), content);

        public void Dispose()
        {
            try
            {
                Directory.Delete(_path, true);
            }
            catch (IOException)
            {
            }
        }
    }
}
