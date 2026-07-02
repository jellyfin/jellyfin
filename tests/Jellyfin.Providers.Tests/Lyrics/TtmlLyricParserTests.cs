using MediaBrowser.Model.Lyrics;
using MediaBrowser.Providers.Lyric;
using Xunit;

namespace Jellyfin.Providers.Tests.Lyrics;

public static class TtmlLyricParserTests
{
    [Fact]
    public static void ParseTtml_SplitsAuxiliaryTracksAndKeepsBackgroundInMainTrack()
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
        Assert.Equal(3, parsed.Tracks.Count);
        Assert.Equal(LyricTrackType.Main, parsed.Tracks[0].Type);
        Assert.Equal(LyricTrackType.Translation, parsed.Tracks[1].Type);
        Assert.Equal("zh-CN", parsed.Tracks[1].Language);
        Assert.Equal(LyricTrackType.Phonetic, parsed.Tracks[2].Type);

        Assert.Single(parsed.Metadata.Artists);
        Assert.Equal("v1", parsed.Metadata.Artists[0].Id);

        var mainLines = parsed.Tracks[0].Lines;
        Assert.Equal(2, mainLines.Count);
        Assert.Equal("Hello World", mainLines[0].Text);
        Assert.Equal("Echo", mainLines[1].Text);
        Assert.Equal("v1", Assert.Single(mainLines[0].ArtistIds));
        Assert.Equal("v1", Assert.Single(mainLines[1].ArtistIds));

        Assert.Equal(2, mainLines[0].Syllables.Count);
        Assert.Equal("Halo", mainLines[0].Syllables[0].Phonetic);
        Assert.Equal("waludo", mainLines[0].Syllables[1].Phonetic);

        Assert.Equal("你好世界", Assert.Single(parsed.Tracks[1].Lines).Text);
        Assert.Equal("Halo Waludo", Assert.Single(parsed.Tracks[2].Lines).Text);
    }
}
