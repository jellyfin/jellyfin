using MediaBrowser.Providers.Plugins.Tmdb;
using TMDbLib.Objects.TvShows;
using Xunit;

namespace Jellyfin.Providers.Tests.Tmdb
{
    public static class TmdbUtilsTests
    {
        [Theory]
        [InlineData("de", "de")]
        [InlineData("En", "En")]
        [InlineData("de-de", "de-DE")]
        [InlineData("en-US", "en-US")]
        [InlineData("de-CH", "de")]
        public static void NormalizeLanguage_Valid_Success(string input, string expected)
        {
            Assert.Equal(expected, TmdbUtils.NormalizeLanguage(input));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        public static void NormalizeLanguage_Invalid_Equal(string? input, string? expected)
        {
            Assert.Equal(expected, TmdbUtils.NormalizeLanguage(input!));
        }

        [Theory]
        [InlineData("en", "en-US", "en-US")]
        [InlineData("fr-CA", "fr-BE", "fr-CA")]
        [InlineData("fr-CA", "fr", "fr-CA")]
        [InlineData("de", "en-US", "de")]
        [InlineData("", "en-US", "")]
        public static void AdjustImageLanguage_Valid_Success(string imageLanguage, string requestLanguage, string? expected)
        {
            Assert.Equal(expected, TmdbUtils.AdjustImageLanguage(imageLanguage, requestLanguage));
        }

        [Theory]
        // The values the web client writes.
        [InlineData("originalAirDate", TvGroupType.OriginalAirDate)]
        [InlineData("absolute", TvGroupType.Absolute)]
        [InlineData("dvd", TvGroupType.DVD)]
        [InlineData("digital", TvGroupType.Digital)]
        [InlineData("storyArc", TvGroupType.StoryArc)]
        [InlineData("production", TvGroupType.Production)]
        [InlineData("tv", TvGroupType.TV)]
        // An NFO written by a third party tool can use any casing.
        [InlineData("OriginalAirDate", TvGroupType.OriginalAirDate)]
        [InlineData("DVD", TvGroupType.DVD)]
        // Tvdb-only orders and the default have no TMDb episode group.
        [InlineData("alternate", null)]
        [InlineData("regional", null)]
        [InlineData("altdvd", null)]
        [InlineData("", null)]
        [InlineData(null, null)]
        public static void GetEpisodeGroupType_ReturnsMatchingGroupType(string? displayOrder, TvGroupType? expected)
        {
            Assert.Equal(expected, TmdbUtils.GetEpisodeGroupType(displayOrder));
        }
    }
}
