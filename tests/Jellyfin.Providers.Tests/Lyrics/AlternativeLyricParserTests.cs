using System;
using MediaBrowser.Model.Lyrics;
using MediaBrowser.Providers.Lyric;
using Xunit;

namespace Jellyfin.Providers.Tests.Lyrics;

public static class AlternativeLyricParserTests
{
    [Fact]
    public static void ParseKrcSyllablesAndBackground()
    {
        const string Content = "[100,1000]<0,500,0>Hello <500,500,0>world\n[bg:<0,300,0>(echo)]";

        var parsed = new KugouKrcLyricParser().ParseLyrics(new LyricFile("sample.krc", Content));

        Assert.NotNull(parsed);
        Assert.Equal(2, parsed.Tracks.Count);
        Assert.Equal("Hello world", Assert.Single(parsed.Tracks[0].Lines).Text);
        Assert.Equal(1000000, parsed.Tracks[0].Lines[0].Start);
        Assert.Equal(2, parsed.Tracks[0].Lines[0].Syllables.Count);
        Assert.Equal(LyricTrackType.Background, parsed.Tracks[1].Type);
        Assert.Equal("echo", Assert.Single(parsed.Tracks[1].Lines).Text);
        Assert.Contains(".qrc", new KugouKrcLyricParser().SupportedExtensions);
    }

    [Fact]
    public static void ParseYrcRelativeSyllables()
    {
        const string Content = "[1000,1000](0,500,0)Hello (500,500,0)world";

        var parsed = new NeteaseYrcLyricParser().ParseLyrics(new LyricFile("sample.yrc", Content));

        Assert.NotNull(parsed);
        var line = Assert.Single(parsed.Tracks[0].Lines);
        Assert.Equal("Hello world", line.Text);
        Assert.Equal(10000000, line.Syllables[0].Start);
        Assert.Equal(20000000, line.Syllables[1].End);
    }

    [Fact]
    public static void ParseLyricifySyllableSeparatesBackground()
    {
        const string Content = "[0]Hello(100,200) world(300,200)\n[6](500,200) echo(700,200)";

        var parsed = new LyricifySyllableParser().ParseLyrics(new LyricFile("sample.lys", Content));

        Assert.NotNull(parsed);
        Assert.Equal(2, parsed.Tracks.Count);
        Assert.Equal("Hello world", Assert.Single(parsed.Tracks[0].Lines).Text);
        Assert.Equal(LyricTrackType.Background, parsed.Tracks[1].Type);
        Assert.Equal(" echo", Assert.Single(parsed.Tracks[1].Lines).Text);
        Assert.Contains(".lys", new LyricifySyllableParser().SupportedExtensions);
    }

    [Theory]
    [InlineData(typeof(KugouKrcLyricParser), "bad.krc", "[oops]")]
    [InlineData(typeof(NeteaseYrcLyricParser), "bad.yrc", "[x]")]
    [InlineData(typeof(LyricifySyllableParser), "bad.lrc", "not a syllable line")]
    public static void ParseMalformedAlternativeLyricsDoesNotThrow(Type parserType, string name, string content)
    {
        var parser = (MediaBrowser.Controller.Lyrics.ILyricParser)Activator.CreateInstance(parserType)!;

        var exception = Record.Exception(() => parser.ParseLyrics(new LyricFile(name, content)));

        Assert.Null(exception);
    }
}
