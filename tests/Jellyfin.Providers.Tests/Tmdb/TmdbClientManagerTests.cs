using System.Collections.Generic;
using MediaBrowser.Providers.Plugins.Tmdb;
using TMDbLib.Objects.General;
using Xunit;

namespace Jellyfin.Providers.Tests.Tmdb
{
    public static class TmdbClientManagerTests
    {
        [Theory]
        [InlineData("fo")]
        [InlineData("de-DE")]
        [InlineData("he")]
        public static void NeedsVideoFallback_NoVideosNonEnglish_True(string language)
        {
            var empty = new ResultContainer<Video> { Results = new List<Video>() };

            Assert.True(TmdbClientManager.NeedsVideoFallback(empty, language));
            Assert.True(TmdbClientManager.NeedsVideoFallback(null, language));
        }

        [Theory]
        [InlineData("en")]
        [InlineData("en-US")]
        [InlineData("EN-gb")]
        [InlineData("")]
        [InlineData(null)]
        public static void NeedsVideoFallback_EnglishOrNoLanguage_False(string? language)
        {
            var empty = new ResultContainer<Video> { Results = new List<Video>() };

            Assert.False(TmdbClientManager.NeedsVideoFallback(empty, language));
        }

        [Fact]
        public static void NeedsVideoFallback_HasLocalizedVideos_False()
        {
            var videos = new ResultContainer<Video> { Results = new List<Video> { new Video { Iso_639_1 = "fo" } } };

            Assert.False(TmdbClientManager.NeedsVideoFallback(videos, "fo"));
        }
    }
}
