using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Plugins.Tmdb;
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
        [InlineData("11", true, 11)]
        // An id another provider filed under the TMDb key must not throw, it is simply not a TMDb id.
        [InlineData("nm0000123", false, 0)]
        [InlineData("tt0113375", false, 0)]
        [InlineData("11.0", false, 0)]
        [InlineData("-11", false, 0)]
        [InlineData("0", false, 0)]
        [InlineData("", false, 0)]
        [InlineData(null, false, 0)]
        public static void TryParseTmdbId_OnlyAcceptsTmdbIds(string? value, bool expected, int expectedId)
        {
            Assert.Equal(expected, TmdbUtils.TryParseTmdbId(value, out var tmdbId));
            Assert.Equal(expectedId, tmdbId);
        }

        [Theory]
        [InlineData("11", true, 11)]
        [InlineData("nm0000123", false, 0)]
        public static void TryGetTmdbId_OnlyAcceptsTmdbIds(string value, bool expected, int expectedId)
        {
            var item = new Movie();
            item.ProviderIds[MetadataProvider.Tmdb.ToString()] = value;

            Assert.Equal(expected, item.TryGetTmdbId(out var tmdbId));
            Assert.Equal(expectedId, tmdbId);
        }

        [Fact]
        public static void TryGetTmdbId_NoId_False()
        {
            Assert.False(new Movie().TryGetTmdbId(out var tmdbId));
            Assert.Equal(0, tmdbId);
        }
    }
}
