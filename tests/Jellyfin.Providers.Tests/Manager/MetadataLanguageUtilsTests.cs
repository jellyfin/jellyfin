using MediaBrowser.Providers.Manager;
using Xunit;

namespace Jellyfin.Providers.Tests.Manager
{
    public class MetadataLanguageUtilsTests
    {
        [Theory]
        [InlineData("es", "es")]
        [InlineData("es-ES", "es")]
        [InlineData("pt-BR", "pt")]
        [InlineData("ES", "es")]
        [InlineData(null, null)]
        [InlineData("", null)]
        public void GetLanguageSubtag_ReturnsLowercasedSubtag(string? language, string? expected)
        {
            Assert.Equal(expected, MetadataLanguageUtils.GetLanguageSubtag(language));
        }

        [Theory]
        [InlineData("es", "es", true)]
        [InlineData("es", "es-ES", true)]
        [InlineData("es-MX", "es-ES", true)]
        [InlineData("ES", "es", true)]
        [InlineData("en", "en", true)]
        [InlineData("en", "es-ES", false)]
        [InlineData("en", "es", false)]
        // An unknown language on either side cannot be judged and is assumed to match
        [InlineData(null, "es", true)]
        [InlineData("", "es", true)]
        [InlineData("en", null, true)]
        [InlineData("en", "", true)]
        public void MatchesPreferredLanguage_ComparesLanguageSubtag(string? resultLanguage, string? preferredLanguage, bool expected)
        {
            Assert.Equal(expected, MetadataLanguageUtils.MatchesPreferredLanguage(resultLanguage, preferredLanguage));
        }
    }
}
