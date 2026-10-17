using System.IO;
using MediaBrowser.Model.Lyrics;
using MediaBrowser.Providers.Lyric;
using Xunit;

namespace Jellyfin.Providers.Tests.Lyrics;

public static class TimedTextLyricParserTests
{
    [Fact]
    public static void ParseTtmlCues()
    {
        var parser = new TimedTextLyricParser();
        var fileContents = File.ReadAllText(Path.Combine("Test Data", "Lyrics", "Fleetwood Mac - Storms.ttml"));
        var parsed = parser.ParseLyrics(new LyricFile("Fleetwood Mac - Storms.ttml", fileContents));

        Assert.NotNull(parsed);
        Assert.Equal(40, parsed.Lyrics.Count);

        var line1 = parsed.Lyrics[0];
        Assert.Equal("Every night that goes between", line1.Text);
        Assert.Equal(130220000, line1.Start);
        Assert.NotNull(line1.Cues);
        Assert.Equal(5, line1.Cues.Count);
        Assert.Equal(130220000, line1.Cues[0].Start);
        Assert.Equal(138380000, line1.Cues[0].End);
        Assert.Equal(0, line1.Cues[0].Position);
        Assert.Equal(5, line1.Cues[0].EndPosition);
        Assert.Equal(6, line1.Cues[1].Position);
        Assert.Equal(11, line1.Cues[1].EndPosition);
        Assert.Equal(12, line1.Cues[2].Position);

        var line5 = parsed.Lyrics[4];
        Assert.Equal("Every night you do not come", line5.Text);
        Assert.NotNull(line5.Cues);
        Assert.Equal(6, line5.Cues.Count);
        Assert.Equal(434680000, line5.Cues[2].Start);
        Assert.Equal(441000000, line5.Cues[2].End);

        // Background vocals are appended to the line text and keep their own cues
        var backgroundLine = parsed.Lyrics[33];
        Assert.Equal("\"Every night he will break your...\" (Ooh, ooh, storm)", backgroundLine.Text);
        Assert.NotNull(backgroundLine.Cues);
        Assert.Equal(9, backgroundLine.Cues.Count);
        Assert.Equal(36, backgroundLine.Cues[6].Position);
        Assert.Equal(41, backgroundLine.Cues[6].EndPosition);
        Assert.Equal(2726620000, backgroundLine.Cues[6].Start);
        Assert.Equal(2748970000, backgroundLine.Cues[6].End);
        Assert.Equal(53, backgroundLine.Cues[^1].EndPosition);

        var lastLine = parsed.Lyrics[^1];
        Assert.Equal("Could save us", lastLine.Text);
        Assert.Equal(3162350000, lastLine.Start);
        Assert.NotNull(lastLine.Cues);
        Assert.Equal(3, lastLine.Cues.Count);
        Assert.Equal(11, lastLine.Cues[^1].Position);
        Assert.Equal(13, lastLine.Cues[^1].EndPosition);
        Assert.Equal(3175520000, lastLine.Cues[^1].Start);
        Assert.Equal(3185120000, lastLine.Cues[^1].End);
    }

    [Fact]
    public static void ParseUntimedTtml()
    {
        var parser = new TimedTextLyricParser();
        var parsed = parser.ParseLyrics(new LyricFile(
            "untimed.ttml",
            """<tt xmlns="http://www.w3.org/ns/ttml" xml:lang="en"><body><div><p>First line</p><p>Second line</p></div></body></tt>"""));

        Assert.NotNull(parsed);
        Assert.Equal(2, parsed.Lyrics.Count);
        Assert.Equal("First line", parsed.Lyrics[0].Text);
        Assert.Null(parsed.Lyrics[0].Start);
        Assert.Null(parsed.Lyrics[0].Cues);
    }

    [Theory]
    [InlineData("lyric.ttml", "not xml")]
    [InlineData("lyric.ttml", """<tt xmlns="http://www.w3.org/ns/ttml"><body><div><foo/></div></body></tt>""")]
    [InlineData("lyric.lrc", """<tt xmlns="http://www.w3.org/ns/ttml" xml:lang="en"><body><div><p>Line</p></div></body></tt>""")]
    public static void ParseInvalidTtml_ReturnsNull(string name, string content)
    {
        var parser = new TimedTextLyricParser();
        Assert.Null(parser.ParseLyrics(new LyricFile(name, content)));
    }
}
