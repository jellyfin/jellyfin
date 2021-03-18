using MediaBrowser.Common.Providers;
using Xunit;

namespace Jellyfin.Common.Tests.Providers
{
    public class ProviderIdParserTests
    {
        [Theory]
        [InlineData("tt123456", false, null)]
        [InlineData("tt1234567", true, "tt1234567")]
        [InlineData("tt12345678", true, "tt12345678")]
        [InlineData("https://www.imdb.com/title/tt123456", false, null)]
        [InlineData("https://www.imdb.com/title/tt1234567", true, "tt1234567")]
        [InlineData("https://www.imdb.com/title/tt12345678", true, "tt12345678")]
        [InlineData(@"multiline\nhttps://www.imdb.com/title/tt1234567", true, "tt1234567")]
        [InlineData(@"multiline\nhttps://www.imdb.com/title/tt12345678", true, "tt12345678")]
        [InlineData("Jellyfin", false, null)]
        [InlineData("tt1234567tt7654321", true, "tt1234567")]
        [InlineData("tt12345678tt7654321", true, "tt12345678")]
        [InlineData("tt123456789", true, "tt12345678")]
        public void Parse_Imdb(string text, bool shouldSucceed, string? imdbId)
        {
            var succeeded = ProviderIdParsers.TryParseImdbId(text, out string? parsedId);
            Assert.Equal(shouldSucceed, succeeded);
            Assert.Equal(imdbId, parsedId);
        }

        [Theory]
        [InlineData("https://www.themoviedb.org/movie/30287-fallo", true, "30287")]
        [InlineData("themoviedb.org/movie/30287", true, "30287")]
        [InlineData("https://www.themoviedb.org/movie/fallo-30287", false, null)]
        [InlineData("https://www.themoviedb.org/tv/1668-friends", false, null)]
        public void Parse_TmdbMovie(string text, bool shouldSucceed, string? tmdbId)
        {
            var succeeded = ProviderIdParsers.TryParseTmdbMovieId(text, out string? parsedId);
            Assert.Equal(shouldSucceed, succeeded);
            Assert.Equal(tmdbId, parsedId);
        }

        [Theory]
        [InlineData("https://www.themoviedb.org/tv/1668-friends", true, "1668")]
        [InlineData("themoviedb.org/tv/1668", true, "1668")]
        [InlineData("https://www.themoviedb.org/tv/friends-1668", false, null)]
        [InlineData("https://www.themoviedb.org/movie/30287-fallo", false, null)]
        public void Parse_TmdbSeries(string text, bool shouldSucceed, string? tmdbId)
        {
            var succeeded = ProviderIdParsers.TryParseTmdbSeriesId(text, out string? parsedId);
            Assert.Equal(shouldSucceed, succeeded);
            Assert.Equal(tmdbId, parsedId);
        }

        [Theory]
        [InlineData("https://www.thetvdb.com/?tab=series&id=121361", true, "121361")]
        [InlineData("thetvdb.com/?tab=series&id=121361", true, "121361")]
        [InlineData("thetvdb.com/?tab=series&id=Jellyfin121361", false, null)]
        [InlineData("https://www.themoviedb.org/tv/1668-friends", false, null)]
        public void Parse_Tvdb(string text, bool shouldSucceed, string? tvdbId)
        {
            var succeeded = ProviderIdParsers.TryParseTvdbId(text, out string? parsedId);
            Assert.Equal(shouldSucceed, succeeded);
            Assert.Equal(tvdbId, parsedId);
        }
    }
}
