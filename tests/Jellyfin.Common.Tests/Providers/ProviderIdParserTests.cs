using System;
using MediaBrowser.Common.Providers;
using Xunit;

namespace Jellyfin.Common.Tests.Providers
{
    public class ProviderIdParserTests
    {
        [Theory]
        [InlineData("tt1234567", "tt1234567")]
        [InlineData("tt12345678", "tt12345678")]
        [InlineData("https://www.imdb.com/title/tt1234567", "tt1234567")]
        [InlineData("https://www.imdb.com/title/tt12345678", "tt12345678")]
        [InlineData(@"multiline\nhttps://www.imdb.com/title/tt1234567", "tt1234567")]
        [InlineData(@"multiline\nhttps://www.imdb.com/title/tt12345678", "tt12345678")]
        [InlineData("tt1234567tt7654321", "tt1234567")]
        [InlineData("tt12345678tt7654321", "tt12345678")]
        [InlineData("tt123456789", "tt12345678")]
        public void FindImdbId_Valid_Success(string text, string expected)
        {
            Assert.True(ProviderIdParsers.TryFindImdbId(text, out ReadOnlySpan<char> parsedId));
            Assert.Equal(expected, parsedId.ToString());
        }

        [Theory]
        [InlineData("tt123456")]
        [InlineData("https://www.imdb.com/title/tt123456")]
        [InlineData("Jellyfin")]
        public void FindImdbId_Invalid_Success(string text)
        {
            Assert.False(ProviderIdParsers.TryFindImdbId(text, out _));
        }

        [Theory]
        [InlineData("https://www.themoviedb.org/movie/30287-fallo", "30287")]
        [InlineData("themoviedb.org/movie/30287", "30287")]
        public void FindTmdbMovieId_Valid_Success(string text, string expected)
        {
            Assert.True(ProviderIdParsers.TryFindTmdbMovieId(text, out ReadOnlySpan<char> parsedId));
            Assert.Equal(expected, parsedId.ToString());
        }

        [Theory]
        [InlineData("https://www.themoviedb.org/movie/fallo-30287")]
        [InlineData("https://www.themoviedb.org/tv/1668-friends")]
        public void FindTmdbMovieId_Invalid_Success(string text)
        {
            Assert.False(ProviderIdParsers.TryFindTmdbMovieId(text, out _));
        }

        [Theory]
        [InlineData("https://www.themoviedb.org/tv/1668-friends", "1668")]
        [InlineData("themoviedb.org/tv/1668", "1668")]
        public void FindTmdbSeriesId_Valid_Success(string text, string expected)
        {
            Assert.True(ProviderIdParsers.TryFindTmdbSeriesId(text, out ReadOnlySpan<char> parsedId));
            Assert.Equal(expected, parsedId.ToString());
        }

        [Theory]
        [InlineData("https://www.themoviedb.org/tv/friends-1668")]
        [InlineData("https://www.themoviedb.org/movie/30287-fallo")]
        public void FindTmdbSeriesId_Invalid_Success(string text)
        {
            Assert.False(ProviderIdParsers.TryFindTmdbSeriesId(text, out _));
        }

        [Theory]
        [InlineData("https://www.thetvdb.com/?tab=series&id=121361", "121361")]
        [InlineData("thetvdb.com/?tab=series&id=121361", "121361")]
        public void FindTvdbId_Valid_Success(string text, string expected)
        {
            Assert.True(ProviderIdParsers.TryFindTvdbId(text, out ReadOnlySpan<char> parsedId));
            Assert.Equal(expected, parsedId.ToString());
        }

        [Theory]
        [InlineData("thetvdb.com/?tab=series&id=Jellyfin121361")]
        [InlineData("https://www.themoviedb.org/tv/1668-friends")]
        public void FindTvdbId_Invalid_Success(string text)
        {
            Assert.False(ProviderIdParsers.TryFindTvdbId(text, out _));
        }
    }
}
