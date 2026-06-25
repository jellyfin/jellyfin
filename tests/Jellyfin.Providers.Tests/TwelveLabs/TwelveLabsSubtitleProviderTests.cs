using MediaBrowser.Providers.Plugins.TwelveLabs;
using Xunit;

namespace Jellyfin.Providers.Tests.TwelveLabs
{
    public static class TwelveLabsSubtitleProviderTests
    {
        [Theory]
        // Standard path under the configured prefix.
        [InlineData("/media/movies/Big Buck Bunny.mp4", "/media/movies", "https://cdn.example.com/movies", "https://cdn.example.com/movies/Big%20Buck%20Bunny.mp4")]
        // Trailing slash on the URL prefix is normalized.
        [InlineData("/media/movies/film.mkv", "/media/movies", "https://cdn.example.com/movies/", "https://cdn.example.com/movies/film.mkv")]
        // Windows-style separators are normalized to forward slashes.
        [InlineData("C:\\media\\film.mp4", "C:\\media", "https://cdn.example.com", "https://cdn.example.com/film.mp4")]
        // Already a URL: passed through untouched.
        [InlineData("https://cdn.example.com/already.mp4", "/media", "https://cdn.example.com", "https://cdn.example.com/already.mp4")]
        public static void BuildPublicMediaUrl_Mappable_ReturnsUrl(string mediaPath, string pathPrefix, string urlPrefix, string expected)
        {
            Assert.Equal(expected, TwelveLabsSubtitleProvider.BuildPublicMediaUrl(mediaPath, pathPrefix, urlPrefix));
        }

        [Theory]
        // No URL prefix configured -> provider disabled.
        [InlineData("/media/movies/film.mp4", "/media/movies", "")]
        // Path outside the mapped prefix -> cannot build a URL.
        [InlineData("/other/film.mp4", "/media/movies", "https://cdn.example.com/movies")]
        // No media path.
        [InlineData(null, "/media/movies", "https://cdn.example.com/movies")]
        public static void BuildPublicMediaUrl_NotMappable_ReturnsNull(string? mediaPath, string pathPrefix, string urlPrefix)
        {
            Assert.Null(TwelveLabsSubtitleProvider.BuildPublicMediaUrl(mediaPath, pathPrefix, urlPrefix));
        }

        [Fact]
        public static void BuildWebVtt_WrapsTranscriptInSingleCue()
        {
            var vtt = TwelveLabsSubtitleProvider.BuildWebVtt("  Hello there.\r\nGeneral Kenobi.  ");

            Assert.StartsWith("WEBVTT", vtt, System.StringComparison.Ordinal);
            Assert.Contains("00:00:00.000 -->", vtt, System.StringComparison.Ordinal);
            Assert.Contains("Hello there.\nGeneral Kenobi.", vtt, System.StringComparison.Ordinal);
            // Surrounding whitespace is trimmed from the transcript body.
            Assert.DoesNotContain("  Hello", vtt, System.StringComparison.Ordinal);
        }
    }
}
