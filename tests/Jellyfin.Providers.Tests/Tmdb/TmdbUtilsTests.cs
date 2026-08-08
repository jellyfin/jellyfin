using MediaBrowser.Providers.Plugins.Tmdb;
using Xunit;

namespace Jellyfin.Providers.Tests.Tmdb
{
    public static class TmdbUtilsTests
    {
        [Theory]
        [InlineData("123", 123)]
        [InlineData("2147483647", 2147483647)]
        public static void TryParseTmdbId_Valid_Success(string input, int expected)
        {
            Assert.True(TmdbUtils.TryParseTmdbId(input, out var actual));
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("nm123")]
        [InlineData("0")]
        [InlineData("-1")]
        [InlineData("2147483648")]
        public static void TryParseTmdbId_Invalid_ReturnsFalse(string? input)
        {
            Assert.False(TmdbUtils.TryParseTmdbId(input, out _));
        }

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
    }
}
