using System;
using System.IO;
using MediaBrowser.Model.Lyrics;
using MediaBrowser.Providers.Lyric;
using Xunit;

namespace Jellyfin.Providers.Tests.Lyrics;

public static class LrcLyricParserTests
{
    [Theory]
    [InlineData("[00:00.00]text[999999999999:99.99]")]
    [InlineData("[00:00.00]<999999999999999999:00.00>text")]
    [InlineData("[00:00.00]<-1:00.00>text")]
    public static void ParseMalformedLrc_DoesNotThrow(string content)
    {
        var parser = new LrcLyricParser();

        var exception = Record.Exception(() => parser.ParseLyrics(new LyricFile("malformed.lrc", content)));

        Assert.Null(exception);
    }

    [Fact]
    public static void ParseElrcSyllables()
    {
        var parser = new LrcLyricParser();
        var fileContents = File.ReadAllText(Path.Combine("Test Data", "Lyrics", "Fleetwood Mac - Rumors.elrc"));
        var parsed = parser.ParseLyrics(new LyricFile("Fleetwood Mac - Rumors.elrc", fileContents));

        Assert.NotNull(parsed);
        Assert.Single(parsed.Tracks);
        Assert.Equal(LyricTrackType.Main, parsed.Tracks[0].Type);
        Assert.Equal(31, parsed.Lyrics.Count);

        var line1 = parsed.Lyrics[0];
        Assert.Equal("Every night that goes between", line1.Text);
        Assert.Empty(line1.ArtistIds);
        Assert.Equal(5, line1.Syllables.Count);
        Assert.Equal("Every", line1.Syllables[0].Text.Trim());
        Assert.Equal(68400000, line1.Syllables[0].Start);
        Assert.Equal(72000000, line1.Syllables[0].End);
        Assert.Equal("night", line1.Syllables[1].Text.Trim());
        Assert.Equal("that", line1.Syllables[2].Text.Trim());

        var line5 = parsed.Lyrics[4];
        Assert.Equal("Every night you do not come", line5.Text);
        Assert.Equal(6, line5.Syllables.Count);
        Assert.Equal(375200000, line5.Syllables[2].Start);
        Assert.Equal(377300000, line5.Syllables[2].End);

        var lastLine = parsed.Lyrics[^1];
        Assert.Equal("I have always been a storm", lastLine.Text);
        Assert.Equal(6, lastLine.Syllables.Count);
        Assert.Equal(2358000000, lastLine.Syllables[^1].Start);
        Assert.Equal("storm", lastLine.Syllables[^1].Text.Trim());
        Assert.Null(lastLine.Syllables[^1].End);
    }

    [Theory]
    [InlineData("100", 9000000)]
    [InlineData("+100", 9000000)]
    [InlineData("-100", 11000000)]
    public static void ParseLrcOffsetAppliesToLinesAndSyllables(string offset, long start)
    {
        var parsed = new LrcLyricParser().ParseLyrics(new LyricFile("sample.elrc", $"[offset:{offset}]\r\n[00:01.00]<00:01.00>Hello<00:01.50>world\r\n[00:02.00]Next"));

        Assert.NotNull(parsed);
        var line = parsed.Tracks[0].Lines[0];
        Assert.Equal(start, line.Start);
        Assert.Equal(start + 10000000, line.End);
        Assert.Equal(2, line.Syllables.Count);
        Assert.Equal(start, line.Syllables[0].Start);
        Assert.Equal(start + 5000000, line.Syllables[0].End);
        Assert.Equal(start + 5000000, line.Syllables[1].Start);
        Assert.Equal(line.End, line.Syllables[1].End);
    }
}
