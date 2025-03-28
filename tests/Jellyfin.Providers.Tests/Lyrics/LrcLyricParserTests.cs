using System.IO;
using MediaBrowser.Model.Lyrics;
using MediaBrowser.Providers.Lyric;
using Xunit;

namespace Jellyfin.Providers.Tests.Lyrics;

public static class LrcLyricParserTests
{
    [Fact]
    public static void ParseElrcCues()
    {
        var parser = new LrcLyricParser();
        var fileContents = File.ReadAllText(Path.Combine("Test Data", "Lyrics", "Fleetwood Mac - Rumors.elrc"));
        var parsed = parser.ParseLyrics(new LyricFile("Fleetwood Mac - Rumors.elrc", fileContents));

        Assert.NotNull(parsed);
        Assert.Equal(31, parsed.Lyrics.Count);

        var line1 = parsed.Lyrics[0];
        Assert.Equal("Every night that goes between", line1.Text);
        Assert.NotNull(line1.Cues);
        Assert.Equal(9, line1.Cues.Count);
        Assert.Equal(68400000, line1.Cues[0].Start);
        Assert.Equal(72000000, line1.Cues[0].End);

        var line5 = parsed.Lyrics[4];
        Assert.Equal("Every night you do not come", line5.Text);
        Assert.NotNull(line5.Cues);
        Assert.Equal(11, line5.Cues.Count);
        Assert.Equal(377300000, line5.Cues[5].Start);
        Assert.Equal(380000000, line5.Cues[5].End);

        var lastLine = parsed.Lyrics[^1];
        Assert.Equal("I have always been a storm", lastLine.Text);
        Assert.NotNull(lastLine.Cues);
        Assert.Equal(11, lastLine.Cues.Count);
        Assert.Equal(2358000000, lastLine.Cues[^1].Start);
        Assert.Null(lastLine.Cues[^1].End);
    }
}
