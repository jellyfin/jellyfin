using System;
using System.Linq;
using System.Text;
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
    public static void ParseQrcOffsetAndPlainTimedLine()
    {
        const string Content = "[offset:100]\n[1000,1000]Timed line";

        var parsed = new QrcLyricParser().ParseLyrics(new LyricFile("sample.qrc", Content));

        Assert.NotNull(parsed);
        var line = Assert.Single(parsed.Tracks[0].Lines);
        Assert.Equal("Timed line", line.Text);
        Assert.Equal(9000000, line.Start);
        Assert.Equal(19000000, line.End);
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

    [Theory]
    [InlineData("[0]你(100,200)好(300,200)", "你好", 2, 5000000)]
    [InlineData("Hello!(100,200)", "Hello!", 1, 3000000)]
    [InlineData("[0]こんにちは(100,200)", "こんにちは", 1, 3000000)]
    [InlineData("[0]Hello(100,200)!", "Hello!", 1, 3000000)]
    public static void ParseLyricifySyllableAcceptsUnicodeAndPunctuation(string content, string text, int syllableCount, long end)
    {
        var parsed = new LyricifySyllableParser().ParseLyrics(new LyricFile("sample.lys", content));

        Assert.NotNull(parsed);
        var line = Assert.Single(Assert.Single(parsed.Tracks).Lines);
        Assert.Equal(text, line.Text);
        Assert.Equal(1000000, line.Start);
        Assert.Equal(end, line.End);
        Assert.Equal(syllableCount, line.Syllables.Count);
        Assert.Equal(text, string.Concat(line.Syllables.Select(i => i.Text)));
        Assert.Equal(1000000, line.Syllables[0].Start);
        Assert.Equal(end, line.Syllables[^1].End);
    }

    [Theory]
    [InlineData("sample.lys", "")]
    [InlineData("sample.lys", "[0]你好")]
    [InlineData("sample.lys", "[0]你(x,200)")]
    [InlineData("sample.lys", "[0]你(100,-200)")]
    [InlineData("sample.lys", "[0]你(9223372036854775807,200)")]
    [InlineData("sample.lys", "[ti:Hello(100,200)]")]
    [InlineData("sample.lys", "[9]Hello(100,200)")]
    [InlineData("sample.lrc", "[0]你(100,200)好(300,200)")]
    public static void ParseLyricifySyllableRejectsInvalidContentAndExtensions(string name, string content)
    {
        Assert.Null(new LyricifySyllableParser().ParseLyrics(new LyricFile(name, content)));
    }

    [Theory]
    [InlineData("\n", "100", 9000000, 19000000)]
    [InlineData("\r\n", "100", 9000000, 19000000)]
    [InlineData("\r", "100", 9000000, 19000000)]
    [InlineData("\n", "+100", 9000000, 19000000)]
    [InlineData("\r\n", "+100", 9000000, 19000000)]
    [InlineData("\r", "+100", 9000000, 19000000)]
    [InlineData("\n", "-100", 11000000, 21000000)]
    [InlineData("\r\n", "-100", 11000000, 21000000)]
    [InlineData("\r", "-100", 11000000, 21000000)]
    public static void ParseQrcOffsetAcrossLineEndings(string newline, string offset, long start, long end)
    {
        var content = $"[ti:Sample]{newline}[offset:{offset}]{newline}[1000,1000]Hello (1000,500)world(1500,500){newline}[1000,1000]Timed line";
        var parsed = new QrcLyricParser().ParseLyrics(new LyricFile("sample.qrc", content));

        Assert.NotNull(parsed);
        var lines = Assert.Single(parsed.Tracks).Lines;
        Assert.Equal(2, lines.Count);
        Assert.All(lines, line =>
        {
            Assert.Equal(start, line.Start);
            Assert.Equal(end, line.End);
        });
        Assert.Equal("Hello world", lines[0].Text);
        Assert.Equal(2, lines[0].Syllables.Count);
        Assert.Equal(start, lines[0].Syllables[0].Start);
        Assert.Equal(start + 5000000, lines[0].Syllables[0].End);
        Assert.Equal(start + 5000000, lines[0].Syllables[1].Start);
        Assert.Equal(end, lines[0].Syllables[1].End);
        Assert.Equal("Timed line", lines[1].Text);
        Assert.Empty(lines[1].Syllables);
    }

    [Theory]
    [InlineData("&#10;", "100", 9000000, 19000000)]
    [InlineData("&#13;&#10;", "100", 9000000, 19000000)]
    [InlineData("&#13;", "100", 9000000, 19000000)]
    [InlineData("&#10;", "-100", 11000000, 21000000)]
    [InlineData("&#13;&#10;", "-100", 11000000, 21000000)]
    [InlineData("&#13;", "-100", 11000000, 21000000)]
    public static void ParseQrcXmlOffsetAcrossEscapedLineEndings(string newline, string offset, long start, long end)
    {
        var content = $"<QrcInfos><Lyric_1 LyricContent=\"[offset:{offset}]{newline}[1000,1000]Hello (1000,500)world(1500,500)\" /></QrcInfos>";
        var parsed = new QrcLyricParser().ParseLyrics(new LyricFile("sample.qrc", content));

        Assert.NotNull(parsed);
        var line = Assert.Single(Assert.Single(parsed.Tracks).Lines);
        Assert.Equal("Hello world", line.Text);
        Assert.Equal(start, line.Start);
        Assert.Equal(end, line.End);
        Assert.Equal(start, line.Syllables[0].Start);
        Assert.Equal(end, line.Syllables[1].End);
    }

    [Theory]
    [InlineData("'")]
    [InlineData("\"")]
    public static void ParseQrcXmlSupportsBothQuotesAndEntities(string quote)
    {
        var content = $"<QrcInfos><Lyric_1 LyricContent={quote}[1000,1000]It&apos;s &amp; fine(1000,500){quote} /></QrcInfos>";
        var parsed = new QrcLyricParser().ParseLyrics(new LyricFile("sample.qrc", content));

        Assert.NotNull(parsed);
        Assert.Equal("It's & fine", Assert.Single(parsed.Tracks[0].Lines).Text);
    }

    [Fact]
    public static void ParseQrcPreservesTrailingText()
    {
        var parsed = new QrcLyricParser().ParseLyrics(new LyricFile("sample.qrc", "[1000,1000]Hello(1000,500)!"));

        Assert.NotNull(parsed);
        var line = Assert.Single(parsed.Tracks[0].Lines);
        Assert.Equal("Hello!", line.Text);
        Assert.Equal(line.Text, Assert.Single(line.Syllables).Text);
    }

    [Theory]
    [InlineData("[1000,1000](1000,500,0)Hello (echo)(1500,500,0)world!", "Hello (echo)world!", 10000000, 20000000)]
    [InlineData("[1000,2000](0,1000,0)(1200,100,0)Hello", "Hello", 22000000, 23000000)]
    [InlineData("[1000,1000](990,500,0)Hello", "Hello", 9900000, 14900000)]
    public static void ParseYrcPreservesTextAndUsesOriginalFirstMarker(string content, string text, long start, long syllableEnd)
    {
        var parsed = new NeteaseYrcLyricParser().ParseLyrics(new LyricFile("sample.yrc", content));

        Assert.NotNull(parsed);
        var line = Assert.Single(parsed.Tracks[0].Lines);
        Assert.Equal(text, line.Text);
        Assert.Equal(start, line.Start);
        Assert.Equal(start, line.Syllables[0].Start);
        Assert.Equal(syllableEnd, line.Syllables[^1].End);
    }

    [Theory]
    [InlineData(typeof(QrcLyricParser), "bad.qrc", "[1000,1000]Hello(9223372036854775807,500)")]
    [InlineData(typeof(NeteaseYrcLyricParser), "bad.yrc", "[1000,1000](9223372036854775807,500,0)Hello")]
    [InlineData(typeof(QrcLyricParser), "bad.qrc", "[1000,1000]Hello(1000,-500)")]
    [InlineData(typeof(QrcLyricParser), "bad.qrc", "[1000,1000]Hello(-1000,500)")]
    [InlineData(typeof(NeteaseYrcLyricParser), "bad.yrc", "[1000,1000](1000,-500,0)Hello")]
    [InlineData(typeof(NeteaseYrcLyricParser), "bad.yrc", "[1000,1000](-1000,500,0)Hello")]
    [InlineData(typeof(QrcLyricParser), "bad.qrc", "<QrcInfos><Lyric_1 LyricContent='broken' >")]
    public static void InvalidTimedContentDoesNotBecomePlainControlMarkup(Type parserType, string name, string content)
    {
        var parser = (MediaBrowser.Controller.Lyrics.ILyricParser)Activator.CreateInstance(parserType)!;
        Assert.Null(parser.ParseLyrics(new LyricFile(name, content)));
    }

    [Theory]
    [InlineData("[\"he\",\"llo\"]")]
    [InlineData("[[\"he\"],[\"l\",\"lo\"]]")]
    public static void ParseKrcPhoneticsDoesNotDiscardFollowingTranslation(string phonetics)
    {
        var encoded = EncodeKrcMetadata($"{{\"content\":[{{\"type\":0,\"lyricContent\":[{phonetics}]}},{{\"type\":1,\"lyricContent\":[[\"translated\"]]}}]}}");
        var parsed = new KugouKrcLyricParser().ParseLyrics(new LyricFile("sample.krc", $"[language:{encoded}]\n[1000,1000]<0,500,0>he<500,500,0>llo"));

        Assert.NotNull(parsed);
        var main = Assert.Single(parsed.Tracks.Single(i => i.Type == LyricTrackType.Main).Lines);
        Assert.Equal("he", main.Syllables[0].Phonetic);
        Assert.Equal("llo", main.Syllables[1].Phonetic);
        Assert.Equal("translated", Assert.Single(parsed.Tracks.Single(i => i.Type == LyricTrackType.Translation).Lines).Text);
    }

    [Theory]
    [InlineData("[9223372036854775807,100]<0,100,0>Bad")]
    [InlineData("[922337203685477,1]<0,0,0>Bad")]
    public static void ParseKrcSkippedLineDoesNotShiftTranslation(string badLine)
    {
        var encoded = EncodeKrcMetadata("{\"content\":[{\"type\":1,\"lyricContent\":[[\"bad translation\"],[\"good translation\"]]}]}");
        var parsed = new KugouKrcLyricParser().ParseLyrics(new LyricFile("sample.krc", $"[language:{encoded}]\n{badLine}\n[1000,100]<0,100,0>Good"));

        Assert.NotNull(parsed);
        Assert.Equal("Good", Assert.Single(parsed.Tracks[0].Lines).Text);
        Assert.Equal("good translation", Assert.Single(parsed.Tracks[1].Lines).Text);
    }

    [Fact]
    public static void ParseKrcMalformedMetadataPreservesOtherRowsAndBlocks()
    {
        var encoded = EncodeKrcMetadata("{\"content\":[null,{\"type\":\"bad\"},{\"type\":0,\"lyricContent\":[42,[\"good\"]]},{\"type\":1,\"lyricContent\":[42,[\"translated\"]]}]}");
        var parsed = new KugouKrcLyricParser().ParseLyrics(new LyricFile("sample.krc", $"[language:{encoded}]\n[1000,100]<0,100,0>First\n[2000,100]<0,100,0>Second"));

        Assert.NotNull(parsed);
        Assert.Equal("good", Assert.Single(parsed.Tracks[0].Lines[1].Syllables).Phonetic);
        var translation = Assert.Single(parsed.Tracks[1].Lines);
        Assert.Equal("translated", translation.Text);
        Assert.Equal(20000000, translation.Start);
    }

    [Fact]
    public static void ParseKrcLineEndIncludesAllSyllables()
    {
        var parsed = new KugouKrcLyricParser().ParseLyrics(new LyricFile("sample.krc", "[1000,100]<500,500,0>Hello"));

        Assert.NotNull(parsed);
        var line = Assert.Single(parsed.Tracks[0].Lines);
        Assert.Equal(20000000, line.End);
        Assert.Equal(20000000, Assert.Single(line.Syllables).End);
    }

    [Theory]
    [InlineData(typeof(KugouKrcLyricParser), "sample.krc", "[1000,100]<0,100,0>Main\n[bg:<0,100,0>(echo)]", "echo")]
    [InlineData(typeof(KugouKrcLyricParser), "sample.krc", "[1000,100]<0,100,0>Main\n[bg:<0,100,0>(ooh) yeah (ooh)]", "(ooh) yeah (ooh)")]
    [InlineData(typeof(KugouKrcLyricParser), "sample.krc", "[1000,100]<0,100,0>Main\n[bg:<0,100,0>（ec<100,100,0>ho）]", "echo")]
    [InlineData(typeof(LyricifySyllableParser), "sample.lys", "[7](echo)(100,100)", "echo")]
    [InlineData(typeof(LyricifySyllableParser), "sample.lys", "[7](ooh) yeah (ooh)(100,100)", "(ooh) yeah (ooh)")]
    [InlineData(typeof(LyricifySyllableParser), "sample.lys", "[7]（ec(100,100)ho）(200,100)", "echo")]
    [InlineData(typeof(LyricifySyllableParser), "sample.lys", "[7](echo(100,100)", "(echo")]
    public static void BackgroundTextAndSyllablesStayConsistent(Type parserType, string name, string content, string text)
    {
        var parser = (MediaBrowser.Controller.Lyrics.ILyricParser)Activator.CreateInstance(parserType)!;
        var parsed = parser.ParseLyrics(new LyricFile(name, content));

        Assert.NotNull(parsed);
        var line = Assert.Single(parsed.Tracks.Single(i => i.Type == LyricTrackType.Background).Lines);
        Assert.Equal(text, line.Text);
        Assert.Equal(text, string.Concat(line.Syllables.Select(i => i.Text)));
    }

    [Theory]
    [InlineData("100", 9000000)]
    [InlineData("+100", 9000000)]
    [InlineData("-100", 11000000)]
    public static void ParseKrcOffsetAppliesToMainBackgroundAndTranslation(string offset, long start)
    {
        var encoded = EncodeKrcMetadata("{\"content\":[{\"type\":1,\"lyricContent\":[[\"translated\"]]}]}");
        var content = $"[offset:{offset}]\r\n[language:{encoded}]\r\n[1000,100]<0,100,0>Main\r\n[bg:<1000,100,0>(echo)]";
        var parsed = new KugouKrcLyricParser().ParseLyrics(new LyricFile("sample.krc", content));

        Assert.NotNull(parsed);
        Assert.Equal(3, parsed.Tracks.Count);
        Assert.All(parsed.Tracks, track =>
        {
            var line = Assert.Single(track.Lines);
            Assert.Equal(start, line.Start);
            Assert.Equal(start + 1000000, line.End);
            Assert.All(line.Syllables, syllable => Assert.Equal(start, syllable.Start));
        });
    }

    [Fact]
    public static void ParseLyricifyOffsetAppliesToSyllables()
    {
        var parsed = new LyricifySyllableParser().ParseLyrics(new LyricFile("sample.lys", "[offset:+100]\r[0]Hello(1000,100)"));

        Assert.NotNull(parsed);
        var line = Assert.Single(parsed.Tracks[0].Lines);
        Assert.Equal(9000000, line.Start);
        Assert.Equal(10000000, line.End);
        Assert.Equal(line.Start, Assert.Single(line.Syllables).Start);
    }

    [Theory]
    [InlineData(typeof(KugouKrcLyricParser), "sample.krc", "[1000,1000]<0,500,0>Hello<500,-100,0>Bad<600,100,0>Good")]
    [InlineData(typeof(QrcLyricParser), "sample.qrc", "[1000,1000]Hello(1000,500)Bad(1500,-100)Good(1600,100)")]
    [InlineData(typeof(NeteaseYrcLyricParser), "sample.yrc", "[1000,1000](1000,500,0)Hello(1500,-100,0)Bad(1600,100,0)Good")]
    [InlineData(typeof(LyricifySyllableParser), "sample.lys", "Hello(1000,500)Bad(1500,-100)Good(1600,100)")]
    public static void InvalidSyllableDoesNotLeakItsMarkerIntoValidSyllables(Type parserType, string name, string content)
    {
        var parser = (MediaBrowser.Controller.Lyrics.ILyricParser)Activator.CreateInstance(parserType)!;
        var parsed = parser.ParseLyrics(new LyricFile(name, content));

        Assert.NotNull(parsed);
        var line = Assert.Single(parsed.Tracks[0].Lines);
        Assert.Equal("HelloGood", line.Text);
        Assert.Equal(line.Text, string.Concat(line.Syllables.Select(i => i.Text)));
    }

    private static string EncodeKrcMetadata(string json)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
}
