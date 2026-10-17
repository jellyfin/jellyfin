using System;
using System.Linq;
using MediaBrowser.Model.Lyrics;
using MediaBrowser.Providers.Lyric;
using Xunit;

namespace Jellyfin.Providers.Tests.Lyrics;

public static class TtmlLyricParserTests
{
    [Fact]
    public static void ParseTtml_SplitsAuxiliaryTracksAndBackgroundTrack()
    {
        const string Ttml = """
            <tt xmlns="http://www.w3.org/ns/ttml" xmlns:ttm="http://www.w3.org/ns/ttml#metadata" xmlns:itunes="http://music.apple.com/lyric-ttml-internal">
              <head>
                <metadata>
                  <ttm:agent type="person" xml:id="v1">Lead</ttm:agent>
                  <iTunesMetadata xmlns="http://music.apple.com/lyric-ttml-internal">
                    <transliterations>
                      <transliteration>
                        <text for="L1">
                          <span begin="00:01.000" end="00:01.500">Halo</span>
                          <span begin="00:01.500" end="00:02.000">waludo</span>
                        </text>
                      </transliteration>
                    </transliterations>
                  </iTunesMetadata>
                </metadata>
              </head>
              <body>
                <div>
                  <p begin="00:01.000" end="00:03.000" ttm:agent="v1" itunes:key="L1">
                    <span begin="00:01.000" end="00:01.500">Hello </span><span begin="00:01.500" end="00:02.000">World</span>
                    <span ttm:role="x-translation" xml:lang="zh-CN">你好世界</span>
                    <span ttm:role="x-roman">Halo Waludo</span>
                    <span begin="00:02.000" end="00:03.000" ttm:role="x-bg"><span begin="00:02.000" end="00:02.500">Echo</span></span>
                  </p>
                </div>
              </body>
            </tt>
            """;

        var parsed = new TtmlLyricParser().ParseLyrics(new LyricFile("sample.ttml", Ttml));

        Assert.NotNull(parsed);
        Assert.Equal(4, parsed.Tracks.Count);
        Assert.Equal(LyricTrackType.Main, parsed.Tracks[0].Type);
        Assert.Equal(LyricTrackType.Translation, parsed.Tracks[1].Type);
        Assert.Equal("zh-CN", parsed.Tracks[1].Language);
        Assert.Equal(LyricTrackType.Phonetic, parsed.Tracks[2].Type);
        Assert.Equal(LyricTrackType.Background, parsed.Tracks[3].Type);

        Assert.Single(parsed.Metadata.Artists);
        Assert.Equal("v1", parsed.Metadata.Artists[0].Id);

        var mainLines = parsed.Tracks[0].Lines;
        Assert.Single(mainLines);
        Assert.Equal("Hello World", mainLines[0].Text);
        Assert.Equal("v1", Assert.Single(mainLines[0].ArtistIds));

        Assert.Equal(2, mainLines[0].Syllables.Count);
        Assert.Equal("Halo", mainLines[0].Syllables[0].Phonetic);
        Assert.Equal("waludo", mainLines[0].Syllables[1].Phonetic);

        Assert.Equal("你好世界", Assert.Single(parsed.Tracks[1].Lines).Text);
        Assert.Equal("Halo Waludo", Assert.Single(parsed.Tracks[2].Lines).Text);
        Assert.Equal("Echo", Assert.Single(parsed.Tracks[3].Lines).Text);
    }

    [Fact]
    public static void ParseTtml_CollapsesLayoutWhitespaceInSyncedLines()
    {
        const string Ttml = """
            <tt xmlns="http://www.w3.org/ns/ttml" xmlns:tts="http://www.w3.org/ns/ttml#styling" xmlns:itunes="http://itunes.apple.com/lyric-ttml-extensions" xmlns:ttm="http://www.w3.org/ns/ttml#metadata" xml:lang="en-US">
                <body><div>
                    <p begin="00:42.723" end="00:48.909">Oh, and when your little
                        legs rest on my shoulders</p>
                    <p begin="02:02.567" end="02:04.112">Breathe, breathe into
                        me</p>
                </div></body>
            </tt>
            """;

        var parsed = new TtmlLyricParser().ParseLyrics(new LyricFile("sample.ttml", Ttml));

        Assert.NotNull(parsed);
        var lines = parsed.Tracks[0].Lines;
        Assert.Equal("Oh, and when your little legs rest on my shoulders", lines[0].Text);
        Assert.Equal("Breathe, breathe into me", lines[1].Text);
    }

    [Fact]
    public static void ParseTtml_PreservesXmlSpacePreserve()
    {
        const string Ttml = "<tt xmlns=\"http://www.w3.org/ns/ttml\"><body><div><p xml:space=\"preserve\">  Keep  spacing  </p></div></body></tt>";

        var parsed = new TtmlLyricParser().ParseLyrics(new LyricFile("sample.ttml", Ttml));

        Assert.NotNull(parsed);
        Assert.Equal("  Keep  spacing  ", Assert.Single(parsed.Tracks[0].Lines).Text);
    }

    [Fact]
    public static void ParseTtml_KaraokeSyllableSpacingUsesTextNodes()
    {
        const string Ttml = """
            <tt xmlns="http://www.w3.org/ns/ttml" xmlns:tts="http://www.w3.org/ns/ttml#styling" xmlns:itunes="http://itunes.apple.com/lyric-ttml-extensions" xmlns:ttm="http://www.w3.org/ns/ttml#metadata" xml:lang="en-US">
                <body><div>
                    <p begin="00:00.000" end="00:02.000">
                        <span begin="00:00.000" end="00:01.000">Hello</span> <span begin="00:01.000" end="00:02.000">world</span>
                    </p>
                </div></body>
            </tt>
            """;

        var parsed = new TtmlLyricParser().ParseLyrics(new LyricFile("sample.ttml", Ttml));

        Assert.NotNull(parsed);
        var syllables = Assert.Single(parsed.Tracks[0].Lines).Syllables;
        Assert.Equal(2, syllables.Count);
        Assert.Equal("Hello ", syllables[0].Text);
        Assert.Equal("world", syllables[1].Text);
    }

    [Fact]
    public static void ParseTtml_PreservesTextBeforeFirstKaraokeSpan()
    {
        const string Ttml = "<tt xmlns=\"http://www.w3.org/ns/ttml\"><body><div><p begin=\"00:00.000\" end=\"00:02.000\">Intro <span begin=\"00:00.000\" end=\"00:01.000\">hello</span> <span begin=\"00:01.000\" end=\"00:02.000\">world</span></p></div></body></tt>";

        var parsed = new TtmlLyricParser().ParseLyrics(new LyricFile("sample.ttml", Ttml));

        Assert.NotNull(parsed);
        var line = Assert.Single(parsed.Tracks[0].Lines);
        Assert.Equal("Intro hello world", line.Text);
        Assert.Equal("Intro hello ", line.Syllables[0].Text);
    }

    [Fact]
    public static void ParseTtml_KeepsBackgroundOnlyParagraph()
    {
        const string Ttml = "<tt xmlns=\"http://www.w3.org/ns/ttml\" xmlns:ttm=\"http://www.w3.org/ns/ttml#metadata\"><body><div><p begin=\"00:00.000\" end=\"00:01.000\"><span ttm:role=\"x-bg\">Echo</span></p></div></body></tt>";

        var parsed = new TtmlLyricParser().ParseLyrics(new LyricFile("sample.ttml", Ttml));

        Assert.NotNull(parsed);
        var track = Assert.Single(parsed.Tracks);
        Assert.Equal(LyricTrackType.Background, track.Type);
        Assert.Equal("Echo", Assert.Single(track.Lines).Text);
    }

    [Fact]
    public static void ParseTtml_PreservesInlineSpaceBeforePrettyPrintedSpan()
    {
        const string Ttml = """
            <tt xmlns="http://www.w3.org/ns/ttml"><body><div><p begin="00:00.000" end="00:01.000"><span begin="00:00.000" end="00:00.500">Get</span> 
            <span begin="00:00.500" end="00:01.000">around</span></p></div></body></tt>
            """;

        var parsed = new TtmlLyricParser().ParseLyrics(new LyricFile("sample.ttml", Ttml));

        Assert.NotNull(parsed);
        var syllables = Assert.Single(parsed.Tracks[0].Lines).Syllables;
        Assert.Equal("Get ", syllables[0].Text);
        Assert.Equal("Get around", string.Concat(syllables.Select(i => i.Text)));
    }

    [Fact]
    public static void ParseTtml_KeepsUntimedLines()
    {
        const string Ttml = """
            <tt xmlns="http://www.w3.org/ns/ttml" xmlns:ttm="http://www.w3.org/ns/ttml#metadata"><body><div>
                <p>Untimed lyric line</p>
            </div></body></tt>
            """;

        var parsed = new TtmlLyricParser().ParseLyrics(new LyricFile("sample.ttml", Ttml));

        Assert.NotNull(parsed);
        var line = Assert.Single(parsed.Tracks[0].Lines);
        Assert.Equal("Untimed lyric line", line.Text);
        Assert.Null(line.Start);
        Assert.Null(line.End);
    }

    [Fact]
    public static void ParseTtml_PreservesSeparateTranslationLanguages()
    {
        const string Ttml = "<tt xmlns=\"http://www.w3.org/ns/ttml\" xmlns:ttm=\"http://www.w3.org/ns/ttml#metadata\"><body><div><p begin=\"00:00.000\" end=\"00:01.000\">Main <span ttm:role=\"x-translation\" xml:lang=\"zh-CN\">中文</span><span ttm:role=\"x-translation\" xml:lang=\"ja-JP\">日本語</span></p></div></body></tt>";

        var parsed = new TtmlLyricParser().ParseLyrics(new LyricFile("sample.ttml", Ttml));

        Assert.NotNull(parsed);
        Assert.Equal(3, parsed.Tracks.Count);
        Assert.Equal("zh-CN", parsed.Tracks[1].Language);
        Assert.Equal("ja-JP", parsed.Tracks[2].Language);
    }

    [Fact]
    public static void ParseTtml_PreservesExternalTranslationLanguage()
    {
        const string Ttml = "<tt xmlns=\"http://www.w3.org/ns/ttml\" xmlns:itunes=\"http://music.apple.com/lyric-ttml-internal\"><head><metadata><iTunesMetadata xmlns=\"http://music.apple.com/lyric-ttml-internal\"><translations><translation xml:lang=\"zh-Hans\"><text for=\"L1\">你好</text></translation><translation xml:lang=\"ja-JP\"><text for=\"L1\">こんにちは</text></translation></translations></iTunesMetadata></metadata></head><body><div><p begin=\"00:00.000\" end=\"00:01.000\" itunes:key=\"L1\">Hello</p></div></body></tt>";

        var parsed = new TtmlLyricParser().ParseLyrics(new LyricFile("sample.ttml", Ttml));

        Assert.NotNull(parsed);
        Assert.Equal(3, parsed.Tracks.Count);
        Assert.Equal("zh-Hans", parsed.Tracks[1].Language);
        Assert.Equal("你好", Assert.Single(parsed.Tracks[1].Lines).Text);
        Assert.Equal("ja-JP", parsed.Tracks[2].Language);
        Assert.Equal("こんにちは", Assert.Single(parsed.Tracks[2].Lines).Text);
    }

    [Fact]
    public static void ParseTtml_StrictValidationIsOptIn()
    {
        const string Ttml = "<tt xmlns=\"http://www.w3.org/ns/ttml\"><body><div><p begin=\"00:00.0000\" end=\"00:01.000\">Line</p></div></body></tt>";

        var parser = new TtmlLyricParser();
        Assert.NotNull(parser.ParseLyrics(new LyricFile("sample.ttml", Ttml)));

        parser.StrictValidation = true;
        Assert.Null(parser.ParseLyrics(new LyricFile("sample.ttml", Ttml)));
    }

    [Fact]
    public static void ParseTtml_StrictValidationAcceptsAppleDocument()
    {
        const string Ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt xmlns="http://www.w3.org/ns/ttml" xmlns:tts="http://www.w3.org/ns/ttml#styling" xmlns:itunes="http://itunes.apple.com/lyric-ttml-extensions" xmlns:ttm="http://www.w3.org/ns/ttml#metadata" xml:lang="en-US">
              <head><metadata><ttm:title>Song</ttm:title></metadata></head>
              <body dur="00:02.000"><div begin="00:00.000" end="00:02.000"><p begin="00:00.000" end="00:01.000">Line</p></div></body>
            </tt>
            """;

        var parser = new TtmlLyricParser { StrictValidation = true };

        var parsed = parser.ParseLyrics(new LyricFile("sample.ttml", Ttml));

        Assert.NotNull(parsed);
        Assert.Equal(TimeSpan.FromSeconds(2).Ticks, parsed.Metadata.Duration);
    }

    [Fact]
    public static void ParseTtml_StrictValidationPreservesXmlSpace()
    {
        const string Ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt xmlns="http://www.w3.org/ns/ttml" xmlns:tts="http://www.w3.org/ns/ttml#styling" xmlns:itunes="http://itunes.apple.com/lyric-ttml-extensions" xmlns:ttm="http://www.w3.org/ns/ttml#metadata" xml:lang="en-US">
              <head><metadata><ttm:title>Song</ttm:title></metadata></head>
              <body><div><p xml:space="preserve">  Keep  spacing  </p></div></body>
            </tt>
            """;

        var parser = new TtmlLyricParser { StrictValidation = true };

        var parsed = parser.ParseLyrics(new LyricFile("sample.ttml", Ttml));

        Assert.NotNull(parsed);
        Assert.Equal("  Keep  spacing  ", Assert.Single(parsed.Tracks[0].Lines).Text);
    }

    [Fact]
    public static void ParseTtml_StrictValidationRejectsLineOutsideSongDuration()
    {
        const string Ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt xmlns="http://www.w3.org/ns/ttml" xmlns:tts="http://www.w3.org/ns/ttml#styling" xmlns:itunes="http://itunes.apple.com/lyric-ttml-extensions" xmlns:ttm="http://www.w3.org/ns/ttml#metadata" xml:lang="en-US">
              <head><metadata><ttm:title>Song</ttm:title></metadata></head>
              <body dur="00:01.000"><div begin="00:00.000" end="00:01.000"><p begin="00:00.000" end="00:02.000">Line</p></div></body>
            </tt>
            """;

        var parser = new TtmlLyricParser { StrictValidation = true };

        Assert.Null(parser.ParseLyrics(new LyricFile("sample.ttml", Ttml)));
        Assert.NotNull(parser.ParseLyrics(new LyricFile("sample.ttml", Ttml.Replace("end=\"00:02.000\"", "end=\"00:01.000\"", StringComparison.Ordinal))));
    }

    [Fact]
    public static void ParseTtml_StrictValidationRejectsUnknownAgent()
    {
        const string Ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt xmlns="http://www.w3.org/ns/ttml" xmlns:tts="http://www.w3.org/ns/ttml#styling" xmlns:itunes="http://itunes.apple.com/lyric-ttml-extensions" xmlns:ttm="http://www.w3.org/ns/ttml#metadata" xml:lang="en-US">
              <head><metadata><ttm:title>Song</ttm:title></metadata></head>
              <body><div><p ttm:agent="missing">Line</p></div></body>
            </tt>
            """;

        var parser = new TtmlLyricParser { StrictValidation = true };

        Assert.Null(parser.ParseLyrics(new LyricFile("sample.ttml", Ttml)));
        Assert.NotNull(parser.ParseLyrics(new LyricFile("sample.ttml", Ttml.Replace(" ttm:agent=\"missing\"", string.Empty, StringComparison.Ordinal))));
    }

    [Fact]
    public static void ParseTtml_StrictValidationAcceptsTimedSpansInsideAgentLine()
    {
        const string Ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt xmlns="http://www.w3.org/ns/ttml" xmlns:tts="http://www.w3.org/ns/ttml#styling" xmlns:itunes="http://itunes.apple.com/lyric-ttml-extensions" xmlns:ttm="http://www.w3.org/ns/ttml#metadata" xml:lang="en-US">
              <head><metadata><ttm:title>Song</ttm:title><ttm:agent type="person" xml:id="v1">Singer</ttm:agent></metadata></head>
              <body><div begin="00:00.000" end="00:02.000"><p begin="00:00.000" end="00:02.000" ttm:agent="v1"><span begin="00:00.000" end="00:01.000">One</span><span begin="00:01.000" end="00:02.000">two</span></p></div></body>
            </tt>
            """;

        var parser = new TtmlLyricParser { StrictValidation = true };

        Assert.NotNull(parser.ParseLyrics(new LyricFile("sample.ttml", Ttml)));
    }

    [Fact]
    public static void ParseTtml_StrictValidationRejectsAgentOnDiv()
    {
        const string Ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt xmlns="http://www.w3.org/ns/ttml" xmlns:tts="http://www.w3.org/ns/ttml#styling" xmlns:itunes="http://itunes.apple.com/lyric-ttml-extensions" xmlns:ttm="http://www.w3.org/ns/ttml#metadata" xml:lang="en-US">
              <head><metadata><ttm:title>Song</ttm:title><ttm:agent type="person" xml:id="v1"><ttm:name type="full">Singer</ttm:name></ttm:agent></metadata></head>
              <body><div begin="00:00.000" end="00:02.000" ttm:agent="v1"><p begin="00:00.000" end="00:02.000">Line</p></div></body>
            </tt>
            """;

        var parser = new TtmlLyricParser { StrictValidation = true };

        Assert.Null(parser.ParseLyrics(new LyricFile("sample.ttml", Ttml)));
        Assert.NotNull(parser.ParseLyrics(new LyricFile("sample.ttml", Ttml.Replace(" ttm:agent=\"v1\"", string.Empty, StringComparison.Ordinal))));
    }

    [Fact]
    public static void ParseTtml_StrictValidationIgnoresForeignNamespaceParagraph()
    {
        const string Ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt xmlns="http://www.w3.org/ns/ttml" xmlns:tts="http://www.w3.org/ns/ttml#styling" xmlns:itunes="http://itunes.apple.com/lyric-ttml-extensions" xmlns:ttm="http://www.w3.org/ns/ttml#metadata" xmlns:evil="urn:evil" xml:lang="en-US">
              <head><metadata><ttm:title>Song</ttm:title></metadata></head>
              <body><div><p begin="00:00.000" end="00:01.000">Valid</p><evil:p begin="00:00.000" end="00:01.000">Fake</evil:p></div></body>
            </tt>
            """;

        var parser = new TtmlLyricParser { StrictValidation = true };

        var parsed = parser.ParseLyrics(new LyricFile("sample.ttml", Ttml));

        Assert.NotNull(parsed);
        Assert.Equal("Valid", Assert.Single(parsed.Tracks[0].Lines).Text);
    }

    [Fact]
    public static void ParseTtml_InvalidTimeDoesNotThrowInDefaultMode()
    {
        const string Ttml = "<tt xmlns=\"http://www.w3.org/ns/ttml\"><body><div><p begin=\"not-a-time\" end=\"00:01.000\">Line</p></div></body></tt>";

        var parsed = new TtmlLyricParser().ParseLyrics(new LyricFile("sample.ttml", Ttml));

        Assert.NotNull(parsed);
        Assert.Null(Assert.Single(parsed.Tracks[0].Lines).Start);
    }

    [Theory]
    [InlineData("19.704", "20.704", 19.704)]
    [InlineData("2:54.285", "2:55.285", 174.285)]
    public static void ParseTtml_CompatibilityModeParsesAppleDownloadClockValues(string value, string end, double seconds)
    {
        var ttml = $"<tt xmlns=\"http://www.w3.org/ns/ttml\"><body><div><p begin=\"{value}\" end=\"{end}\">Line</p></div></body></tt>";

        var parsed = new TtmlLyricParser().ParseLyrics(new LyricFile("sample.ttml", ttml));

        Assert.NotNull(parsed);
        Assert.Equal(TimeSpan.FromSeconds(seconds).Ticks, Assert.Single(parsed.Tracks[0].Lines).Start);
    }

    [Fact]
    public static void ParseTtml_StrictValidationRejectsInvalidTimingRange()
    {
        var ttml = CreateAppleDocument("<div><p begin=\"00:02.000\" end=\"00:01.000\">Line</p></div>");

        var parser = new TtmlLyricParser { StrictValidation = true };

        Assert.Null(parser.ParseLyrics(new LyricFile("sample.ttml", ttml)));
        Assert.NotNull(parser.ParseLyrics(new LyricFile("sample.ttml", ttml.Replace("begin=\"00:02.000\"", "begin=\"00:00.000\"", StringComparison.Ordinal))));
    }

    [Fact]
    public static void ParseTtml_StrictValidationRejectsNamespacedTimingAttribute()
    {
        var ttml = CreateAppleDocument("<div><p foo:begin=\"00:00.000\" end=\"00:01.000\">Line</p></div>");

        var parser = new TtmlLyricParser { StrictValidation = true };

        Assert.Null(parser.ParseLyrics(new LyricFile("sample.ttml", ttml)));
        Assert.NotNull(parser.ParseLyrics(new LyricFile("sample.ttml", ttml.Replace("foo:begin", "begin", StringComparison.Ordinal))));
    }

    [Fact]
    public static void ParseTtml_StrictValidationRejectsOverflowingTime()
    {
        var ttml = CreateAppleDocument("<div><p begin=\"999999999999:00:00\" end=\"999999999999:00:01\">Line</p></div>");

        var parser = new TtmlLyricParser { StrictValidation = true };

        Assert.Null(parser.ParseLyrics(new LyricFile("sample.ttml", ttml)));
        Assert.NotNull(parser.ParseLyrics(new LyricFile("sample.ttml", ttml.Replace("999999999999:", string.Empty, StringComparison.Ordinal))));
    }

    [Theory]
    [InlineData("<span>Intro </span>", "", "", "Intro hello world", "Intro hello ", "world")]
    [InlineData("", "<span>dear </span>", "", "hello dear world", "hello dear ", "world")]
    [InlineData("", "", "<span>!</span>", "hello world!", "hello ", "world!")]
    [InlineData("<span>Intro </span>", "<span>dear </span>", "<span>!</span>", "Intro hello dear world!", "Intro hello dear ", "world!")]
    [InlineData("", "<span begin=\"00:00.500\">dear </span>", "", "hello dear world", "hello dear ", "world")]
    public static void ParseTtml_PreservesUntimedSpansAmongSyllables(
        string prefix,
        string middle,
        string suffix,
        string expectedText,
        string firstSyllable,
        string secondSyllable)
    {
        var ttml = $"<tt xmlns=\"http://www.w3.org/ns/ttml\"><body><div><p begin=\"00:00.000\" end=\"00:02.000\">{prefix}<span begin=\"00:00.000\" end=\"00:01.000\">hello</span> {middle}<span begin=\"00:01.000\" end=\"00:02.000\">world</span>{suffix}</p></div></body></tt>";
        var parsed = new TtmlLyricParser().ParseLyrics(new LyricFile("sample.ttml", ttml));

        Assert.NotNull(parsed);
        var line = Assert.Single(Assert.Single(parsed.Tracks).Lines);
        Assert.Equal(expectedText, line.Text);
        Assert.Equal(line.Text, string.Concat(line.Syllables.Select(i => i.Text)));
        Assert.Collection(
            line.Syllables,
            syllable =>
            {
                Assert.Equal(firstSyllable, syllable.Text);
                Assert.Equal(0, syllable.Start);
                Assert.Equal(TimeSpan.FromSeconds(1).Ticks, syllable.End);
            },
            syllable =>
            {
                Assert.Equal(secondSyllable, syllable.Text);
                Assert.Equal(TimeSpan.FromSeconds(1).Ticks, syllable.Start);
                Assert.Equal(TimeSpan.FromSeconds(2).Ticks, syllable.End);
            });
    }

    [Fact]
    public static void ParseTtml_PreservesMixedBackgroundSpansWithoutIncludingAuxiliaryText()
    {
        const string Ttml = """
            <tt xmlns="http://www.w3.org/ns/ttml" xmlns:ttm="http://www.w3.org/ns/ttml#metadata"><body><div>
              <p begin="00:00.000" end="00:03.000"><span>Lead </span><span begin="00:00.000" end="00:01.000">voice</span><span>!</span><span ttm:role="x-translation">Translation</span><span ttm:role="x-roman">Phonetic</span><span ttm:role="x-bg" begin="00:01.000" end="00:03.000"><span>Soft </span><span begin="00:01.000" end="00:02.000">echo</span><span> and </span><span begin="00:02.000" end="00:03.000">reply</span><span>!</span><span ttm:role="x-translation">Background translation</span></span></p>
            </div></body></tt>
            """;

        var parsed = new TtmlLyricParser().ParseLyrics(new LyricFile("sample.ttml", Ttml));

        Assert.NotNull(parsed);
        var main = Assert.Single(parsed.Tracks.Single(i => i.Type == LyricTrackType.Main).Lines);
        Assert.Equal("Lead voice!", main.Text);
        Assert.Equal(main.Text, Assert.Single(main.Syllables).Text);
        var background = Assert.Single(parsed.Tracks.Single(i => i.Type == LyricTrackType.Background).Lines);
        Assert.Equal("Soft echo and reply!", background.Text);
        Assert.Equal(background.Text, string.Concat(background.Syllables.Select(i => i.Text)));
        Assert.Equal("Soft echo and ", background.Syllables[0].Text);
        Assert.Equal("reply!", background.Syllables[1].Text);
        Assert.Equal(TimeSpan.FromSeconds(1).Ticks, background.Syllables[0].Start);
        Assert.Equal(TimeSpan.FromSeconds(2).Ticks, background.Syllables[0].End);
        Assert.Equal(TimeSpan.FromSeconds(2).Ticks, background.Syllables[1].Start);
        Assert.Equal(TimeSpan.FromSeconds(3).Ticks, background.Syllables[1].End);
        Assert.Equal(2, parsed.Tracks.Single(i => i.Type == LyricTrackType.Translation).Lines.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("begin=\"00:00.000\" end=\"00:02.000\"")]
    public static void ParseTtml_NestedWrappersPreserveLeafTiming(string timing)
    {
        var ttml = CreateAppleDocument($"<div><p begin=\"00:00.000\" end=\"00:02.000\"><span {timing}>Intro <span begin=\"00:00.000\" end=\"00:01.000\">hel</span><span><span begin=\"00:01.000\" end=\"00:02.000\">lo</span>!</span></span></p></div>");
        var parsed = new TtmlLyricParser { StrictValidation = true }.ParseLyrics(new LyricFile("sample.ttml", ttml));

        Assert.NotNull(parsed);
        var line = Assert.Single(parsed.Tracks[0].Lines);
        Assert.Equal("Intro hello!", line.Text);
        Assert.Equal(2, line.Syllables.Count);
        Assert.Equal("Intro hel", line.Syllables[0].Text);
        Assert.Equal(0, line.Syllables[0].Start);
        Assert.Equal(10000000, line.Syllables[0].End);
        Assert.Equal("lo!", line.Syllables[1].Text);
        Assert.Equal(10000000, line.Syllables[1].Start);
        Assert.Equal(20000000, line.Syllables[1].End);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public static void ParseTtml_NestedAuxiliaryTextIsSeparated(bool timed)
    {
        var timing = timed ? "begin=\"00:01.000\" end=\"00:02.000\"" : string.Empty;
        var ttml = CreateAppleDocument($"<div><p begin=\"00:01.000\" end=\"00:03.000\"><span {timing}>hello<span ttm:role=\"x-translation\" xml:lang=\"zh-CN\">你好</span><span ttm:role=\"x-roman\">halo</span></span></p></div>");
        var parsed = new TtmlLyricParser { StrictValidation = true }.ParseLyrics(new LyricFile("sample.ttml", ttml));

        Assert.NotNull(parsed);
        Assert.Equal("hello", Assert.Single(parsed.Tracks.Single(i => i.Type == LyricTrackType.Main).Lines).Text);
        var translationTrack = parsed.Tracks.Single(i => i.Type == LyricTrackType.Translation);
        Assert.Equal("zh-CN", translationTrack.Language);
        var translation = Assert.Single(translationTrack.Lines);
        Assert.Equal("你好", translation.Text);
        Assert.Equal(10000000, translation.Start);
        Assert.Equal(timed ? 20000000 : 30000000, translation.End);
        Assert.Equal("halo", Assert.Single(parsed.Tracks.Single(i => i.Type == LyricTrackType.Phonetic).Lines).Text);
    }

    [Fact]
    public static void ParseTtml_BackgroundAgentDoesNotLeakIntoMainOrTranslation()
    {
        var ttml = CreateAppleDocument("<div><p begin=\"00:00.000\" end=\"00:02.000\"><span ttm:agent=\"v1\">Lead</span><span><span ttm:role=\"x-bg\" ttm:agent=\"v2\">Echo<span ttm:role=\"x-translation\">背景翻译</span></span></span></p></div>", "<ttm:agent type=\"person\" xml:id=\"v1\">Lead</ttm:agent><ttm:agent type=\"person\" xml:id=\"v2\">Backing</ttm:agent>");
        var parsed = new TtmlLyricParser().ParseLyrics(new LyricFile("sample.ttml", ttml));

        Assert.NotNull(parsed);
        var main = Assert.Single(parsed.Tracks.Single(i => i.Type == LyricTrackType.Main).Lines);
        var background = Assert.Single(parsed.Tracks.Single(i => i.Type == LyricTrackType.Background).Lines);
        var translation = Assert.Single(parsed.Tracks.Single(i => i.Type == LyricTrackType.Translation).Lines);
        Assert.Equal("v1", Assert.Single(main.ArtistIds));
        Assert.Equal("v2", Assert.Single(background.ArtistIds));
        Assert.Equal("v2", Assert.Single(translation.ArtistIds));
        Assert.Equal("Lead", main.Text);
        Assert.Equal("Echo", background.Text);
    }

    [Theory]
    [InlineData("00:01.000", "00:02.000", false)]
    [InlineData("00:05.000", "00:07.000", false)]
    [InlineData("00:05.000", "00:05.500", true)]
    public static void ParseTtml_StrictValidationChecksImmediateTimedParent(string begin, string end, bool valid)
    {
        var ttml = CreateAppleDocument($"<div begin=\"00:00.000\" end=\"00:10.000\"><p begin=\"00:00.000\" end=\"00:10.000\">Main<span ttm:role=\"x-bg\" begin=\"00:05.000\" end=\"00:06.000\"><span><span begin=\"{begin}\" end=\"{end}\">Echo</span></span></span></p></div>");
        var parsed = new TtmlLyricParser { StrictValidation = true }.ParseLyrics(new LyricFile("sample.ttml", ttml));

        Assert.Equal(valid, parsed is not null);
    }

    [Theory]
    [InlineData("foo:begin=\"99:00.000\" begin=\"00:00.000\"")]
    [InlineData("begin=\"00:00.000\" foo:begin=\"99:00.000\"")]
    public static void ParseTtml_StrictValidationAndParsingUseTheSameAttributes(string beginAttributes)
    {
        var ttml = CreateAppleDocument($"<div><p {beginAttributes} end=\"00:01.000\"><span foo:begin=\"88:00.000\" begin=\"00:00.000\" end=\"00:01.000\">Line</span></p></div>")
            .Replace("<body>", "<body foo:dur=\"99:00.000\" dur=\"00:02.000\">", StringComparison.Ordinal);
        var parsed = new TtmlLyricParser { StrictValidation = true }.ParseLyrics(new LyricFile("sample.ttml", ttml));

        Assert.NotNull(parsed);
        Assert.Equal(20000000, parsed.Metadata.Duration);
        var line = Assert.Single(parsed.Tracks[0].Lines);
        Assert.Equal(0, line.Start);
        Assert.Equal(10000000, line.End);
        Assert.Equal(0, Assert.Single(line.Syllables).Start);
    }

    [Fact]
    public static void ParseTtml_StrictValidationCanParseLongHoursItAccepts()
    {
        var ttml = CreateAppleDocument("<div><p begin=\"100:00:00.000\" end=\"100:00:01.000\">Line</p></div>");
        var parsed = new TtmlLyricParser { StrictValidation = true }.ParseLyrics(new LyricFile("sample.ttml", ttml));

        Assert.NotNull(parsed);
        var line = Assert.Single(parsed.Tracks[0].Lines);
        Assert.Equal(TimeSpan.FromHours(100).Ticks, line.Start);
        Assert.Equal(TimeSpan.FromHours(100).Ticks + TimeSpan.TicksPerSecond, line.End);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public static void ParseTtml_TimedLinesPreserveXmlSpace(bool strict)
    {
        var ttml = CreateAppleDocument("<div><p begin=\"00:00.000\" end=\"00:01.000\" xml:space=\"preserve\">  <span begin=\"00:00.000\" end=\"00:01.000\">hello  </span> ! </p></div>");
        var parsed = new TtmlLyricParser { StrictValidation = strict }.ParseLyrics(new LyricFile("sample.ttml", ttml));

        Assert.NotNull(parsed);
        var line = Assert.Single(parsed.Tracks[0].Lines);
        Assert.Equal("  hello   ! ", line.Text);
        Assert.Equal(line.Text, Assert.Single(line.Syllables).Text);
    }

    [Fact]
    public static void ParseTtml_InlineTranslationInheritsNearestLanguage()
    {
        var ttml = CreateAppleDocument("<div><p>Main<span xml:lang=\"zh-CN\"><span ttm:role=\"x-translation\">中文</span></span></p></div>");
        var parsed = new TtmlLyricParser().ParseLyrics(new LyricFile("sample.ttml", ttml));

        Assert.NotNull(parsed);
        var translation = parsed.Tracks.Single(i => i.Type == LyricTrackType.Translation);
        Assert.Equal("zh-CN", translation.Language);
        Assert.Equal("中文", Assert.Single(translation.Lines).Text);
    }

    [Fact]
    public static void ParseTtml_CompatibilityModeDropsReversedTimingButPreservesText()
    {
        var ttml = CreateAppleDocument("<div><p begin=\"00:02.000\" end=\"00:01.000\">Line</p></div>");
        var parsed = new TtmlLyricParser().ParseLyrics(new LyricFile("sample.ttml", ttml));

        Assert.NotNull(parsed);
        var line = Assert.Single(parsed.Tracks[0].Lines);
        Assert.Equal("Line", line.Text);
        Assert.Null(line.Start);
        Assert.Null(line.End);
    }

    [Fact]
    public static void ParseTtml_DeeplyNestedSpansDoNotLoseTiming()
    {
        var nested = string.Concat(Enumerable.Repeat("<span>", 3000))
            + "<span begin=\"00:00.000\" end=\"00:01.000\">Line</span>"
            + string.Concat(Enumerable.Repeat("</span>", 3000));
        var parsed = new TtmlLyricParser().ParseLyrics(new LyricFile("sample.ttml", CreateAppleDocument($"<div><p>{nested}</p></div>")));

        Assert.NotNull(parsed);
        var line = Assert.Single(parsed.Tracks[0].Lines);
        Assert.Equal("Line", line.Text);
        Assert.Equal(10000000, Assert.Single(line.Syllables).End);
    }

    [Theory]
    [InlineData(true, "Na\u2005pause")]
    [InlineData(false, "Na\u2005pause")]
    [InlineData(true, "\u2005hello\u2005")]
    [InlineData(false, "\u2005hello\u2005")]
    public static void ParseTtml_DoesNotCollapseNonXmlUnicodeWhitespace(bool timed, string text)
    {
        var content = timed ? $"<span begin=\"00:00.000\" end=\"00:01.000\">{text}</span>" : text;
        var ttml = CreateAppleDocument($"<div><p>{content}</p></div>");
        var parsed = new TtmlLyricParser().ParseLyrics(new LyricFile("sample.ttml", ttml));

        Assert.NotNull(parsed);
        var line = Assert.Single(parsed.Tracks[0].Lines);
        Assert.Equal(text, line.Text);
        if (timed)
        {
            Assert.Equal(text, Assert.Single(line.Syllables).Text);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public static void ParseTtml_PreservesConflictingDuplicateTranslations(bool strict)
    {
        const string Metadata = """
            <itunes:translations><itunes:translation xml:lang="zh-Hans">
              <itunes:text for="L1">第一段翻译</itunes:text>
              <itunes:text for="L1">第二段翻译</itunes:text>
              <itunes:text for="L1">第一段翻译</itunes:text>
            </itunes:translation><itunes:translation xml:lang="ja-JP">
              <itunes:text for="L1">日本語</itunes:text>
            </itunes:translation></itunes:translations>
            """;
        var ttml = CreateAppleDocument("<div><p begin=\"00:00.000\" end=\"00:01.000\" itunes:key=\"L1\">Main</p></div>", Metadata);
        var parsed = new TtmlLyricParser { StrictValidation = strict }.ParseLyrics(new LyricFile("sample.ttml", ttml));

        Assert.NotNull(parsed);
        var chinese = parsed.Tracks.Single(i => i.Type == LyricTrackType.Translation && i.Language == "zh-Hans");
        Assert.Equal(new[] { "第一段翻译", "第二段翻译" }, chinese.Lines.Select(i => i.Text));
        Assert.All(chinese.Lines, line =>
        {
            Assert.Equal(0, line.Start);
            Assert.Equal(10000000, line.End);
        });
        var japanese = parsed.Tracks.Single(i => i.Type == LyricTrackType.Translation && i.Language == "ja-JP");
        Assert.Equal("日本語", Assert.Single(japanese.Lines).Text);
    }

    private static string CreateAppleDocument(string body, string metadata = "")
        => $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <tt xmlns="http://www.w3.org/ns/ttml" xmlns:tts="http://www.w3.org/ns/ttml#styling" xmlns:itunes="http://itunes.apple.com/lyric-ttml-extensions" xmlns:ttm="http://www.w3.org/ns/ttml#metadata" xmlns:foo="urn:foreign" xml:lang="en-US">
              <head><metadata><ttm:title>Song</ttm:title>{metadata}</metadata></head>
              <body>{body}</body>
            </tt>
            """;
}
