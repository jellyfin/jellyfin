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
    public static void ParseTtml_StrictValidationRejectsNonDefaultXmlSpace()
    {
        const string Ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt xmlns="http://www.w3.org/ns/ttml" xmlns:tts="http://www.w3.org/ns/ttml#styling" xmlns:itunes="http://itunes.apple.com/lyric-ttml-extensions" xmlns:ttm="http://www.w3.org/ns/ttml#metadata" xml:lang="en-US" xml:space="preserve">
              <head><metadata><ttm:title>Song</ttm:title></metadata></head>
              <body><div><p>Line</p></div></body>
            </tt>
            """;

        var parser = new TtmlLyricParser { StrictValidation = true };

        Assert.Null(parser.ParseLyrics(new LyricFile("sample.ttml", Ttml)));
    }

    [Fact]
    public static void ParseTtml_StrictValidationRejectsLineOutsideSongDuration()
    {
        const string Ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt xmlns="http://www.w3.org/ns/ttml" xmlns:ttm="http://www.w3.org/ns/ttml#metadata" xml:lang="en-US">
              <head><metadata><ttm:title>Song</ttm:title></metadata></head>
              <body dur="00:01.000"><div begin="00:00.000" end="00:01.000"><p begin="00:00.000" end="00:02.000">Line</p></div></body>
            </tt>
            """;

        var parser = new TtmlLyricParser { StrictValidation = true };

        Assert.Null(parser.ParseLyrics(new LyricFile("sample.ttml", Ttml)));
    }

    [Fact]
    public static void ParseTtml_StrictValidationRejectsUnknownAgent()
    {
        const string Ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt xmlns="http://www.w3.org/ns/ttml" xmlns:ttm="http://www.w3.org/ns/ttml#metadata" xml:lang="en-US">
              <head><metadata><ttm:title>Song</ttm:title></metadata></head>
              <body><div><p ttm:agent="missing">Line</p></div></body>
            </tt>
            """;

        var parser = new TtmlLyricParser { StrictValidation = true };

        Assert.Null(parser.ParseLyrics(new LyricFile("sample.ttml", Ttml)));
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
            <tt xmlns="http://www.w3.org/ns/ttml" xmlns:ttm="http://www.w3.org/ns/ttml#metadata" xml:lang="en-US">
              <head><metadata><ttm:title>Song</ttm:title><ttm:agent type="person" xml:id="v1"><ttm:name type="full">Singer</ttm:name></ttm:agent></metadata></head>
              <body><div begin="00:00.000" end="00:02.000" ttm:agent="v1"><p begin="00:00.000" end="00:02.000">Line</p></div></body>
            </tt>
            """;

        var parser = new TtmlLyricParser { StrictValidation = true };

        Assert.Null(parser.ParseLyrics(new LyricFile("sample.ttml", Ttml)));
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

    [Fact]
    public static void ParseTtml_StrictValidationRejectsInvalidTimingRange()
    {
        const string Ttml = "<tt xmlns=\"http://www.w3.org/ns/ttml\"><body><div><p begin=\"00:02.000\" end=\"00:01.000\">Line</p></div></body></tt>";

        var parser = new TtmlLyricParser { StrictValidation = true };

        Assert.Null(parser.ParseLyrics(new LyricFile("sample.ttml", Ttml)));
    }

    [Fact]
    public static void ParseTtml_StrictValidationRejectsNamespacedTimingAttribute()
    {
        const string Ttml = "<tt xmlns=\"http://www.w3.org/ns/ttml\" xmlns:ttm=\"http://www.w3.org/ns/ttml#metadata\" xmlns:foo=\"urn:invalid\" xml:lang=\"en-US\"><head><metadata><ttm:title>Song</ttm:title></metadata></head><body><div><p foo:begin=\"00:00.000\" end=\"00:01.000\">Line</p></div></body></tt>";

        var parser = new TtmlLyricParser { StrictValidation = true };

        Assert.Null(parser.ParseLyrics(new LyricFile("sample.ttml", Ttml)));
    }

    [Fact]
    public static void ParseTtml_StrictValidationRejectsOverflowingTime()
    {
        const string Ttml = "<tt xmlns=\"http://www.w3.org/ns/ttml\" xmlns:ttm=\"http://www.w3.org/ns/ttml#metadata\" xml:lang=\"en-US\"><head><metadata><ttm:title>Song</ttm:title></metadata></head><body><div><p begin=\"999999999999:00:00\" end=\"999999999999:00:01\">Line</p></div></body></tt>";

        var parser = new TtmlLyricParser { StrictValidation = true };

        Assert.Null(parser.ParseLyrics(new LyricFile("sample.ttml", Ttml)));
    }
}
