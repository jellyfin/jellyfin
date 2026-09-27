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
    public static void ParseYrcAbsoluteSyllables()
    {
        const string Content = "[1000,1000](1000,500,0)Hello (1500,500,0)world";

        var parsed = new NeteaseYrcLyricParser().ParseLyrics(new LyricFile("sample.yrc", Content));

        Assert.NotNull(parsed);
        var line = Assert.Single(parsed.Tracks[0].Lines);
        Assert.Equal(10000000, line.Syllables[0].Start);
        Assert.Equal(20000000, line.Syllables[1].End);
    }

    [Fact]
    public static void ParseYrcUsesFirstSyllableAsLineStart()
    {
        const string Content = "[1000,1000](1100,500,0)Hello";

        var parsed = new NeteaseYrcLyricParser().ParseLyrics(new LyricFile("sample.yrc", Content));

        Assert.NotNull(parsed);
        Assert.Equal(11000000, Assert.Single(parsed.Tracks[0].Lines).Start);
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
        Assert.DoesNotContain(".lrc", new LyricifySyllableParser().SupportedExtensions);
    }

    [Fact]
    public static void ParseQrcWordTimestamps()
    {
        const string Content = "[1000,1000]Hello (1000,500)world(1500,500)";

        var parsed = new QrcLyricParser().ParseLyrics(new LyricFile("sample.qrc", Content));

        Assert.NotNull(parsed);
        var line = Assert.Single(parsed.Tracks[0].Lines);
        Assert.Equal("Hello world", line.Text);
        Assert.Equal(10000000, line.Syllables[0].Start);
        Assert.Equal(20000000, line.Syllables[1].End);
        Assert.DoesNotContain(".qrc", new KugouKrcLyricParser().SupportedExtensions);
    }

    [Fact]
    public static void ParseQrcXmlEnvelope()
    {
        const string Content = "<QrcInfos><Lyric_1 LyricContent=\"[1000,1000]Hello (1000,500)world(1500,500)\" /></QrcInfos>";

        var parsed = new QrcLyricParser().ParseLyrics(new LyricFile("sample.qrc", Content));

        Assert.NotNull(parsed);
        Assert.Equal("Hello world", Assert.Single(parsed.Tracks[0].Lines).Text);
    }

    [Fact]
    public static void AlternativeParsersDoNotCrossParseFormats()
    {
        const string KrcContent = "[1000,1000]<0,500,0>Hello";
        const string QrcContent = "[1000,1000]Hello (1000,500)";

        Assert.Null(new KugouKrcLyricParser().ParseLyrics(new LyricFile("sample.qrc", QrcContent)));
        Assert.Null(new QrcLyricParser().ParseLyrics(new LyricFile("sample.krc", KrcContent)));
    }

    [Theory]
    [InlineData(typeof(KugouKrcLyricParser), "bad.krc", "[oops]")]
    [InlineData(typeof(QrcLyricParser), "bad.qrc", "[oops]")]
    [InlineData(typeof(NeteaseYrcLyricParser), "bad.yrc", "[x]")]
    [InlineData(typeof(LyricifySyllableParser), "bad.lys", "not a syllable line")]
    public static void ParseMalformedAlternativeLyricsDoesNotThrow(Type parserType, string name, string content)
    {
        var parser = (MediaBrowser.Controller.Lyrics.ILyricParser)Activator.CreateInstance(parserType)!;

        var exception = Record.Exception(() => parser.ParseLyrics(new LyricFile(name, content)));

        Assert.Null(exception);
    }
}
