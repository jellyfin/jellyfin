using System.Linq;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Providers;
using Xunit;

namespace Jellyfin.Model.Tests.Extensions
{
    public class EnumerableExtensionsTests
    {
        [Fact]
        public void OrderByLanguageDescending_PreferredLanguageFirst()
        {
            var images = new[]
            {
                new RemoteImageInfo { Language = "en", CommunityRating = 5.0, VoteCount = 100 },
                new RemoteImageInfo { Language = "de", CommunityRating = 9.0, VoteCount = 200 },
                new RemoteImageInfo { Language = null, CommunityRating = 7.0, VoteCount = 50 },
                new RemoteImageInfo { Language = "fr", CommunityRating = 8.0, VoteCount = 150 },
            };

            var result = images.OrderByLanguageDescending("de").ToList();

            Assert.Equal("de", result[0].Language);
            Assert.Equal("en", result[1].Language);
            Assert.Null(result[2].Language);
            Assert.Equal("fr", result[3].Language);
        }

        [Fact]
        public void OrderByLanguageDescending_EnglishBeforeNoLanguage()
        {
            var images = new[]
            {
                new RemoteImageInfo { Language = null, CommunityRating = 9.0, VoteCount = 500 },
                new RemoteImageInfo { Language = "en", CommunityRating = 3.0, VoteCount = 10 },
            };

            var result = images.OrderByLanguageDescending("de").ToList();

            // English should come before no-language, even with lower rating
            Assert.Equal("en", result[0].Language);
            Assert.Null(result[1].Language);
        }

        [Fact]
        public void OrderByLanguageDescending_SameLanguageSortedByRatingThenVoteCount()
        {
            var images = new[]
            {
                new RemoteImageInfo { Language = "de", CommunityRating = 5.0, VoteCount = 100 },
                new RemoteImageInfo { Language = "de", CommunityRating = 9.0, VoteCount = 50 },
                new RemoteImageInfo { Language = "de", CommunityRating = 9.0, VoteCount = 200 },
            };

            var result = images.OrderByLanguageDescending("de").ToList();

            Assert.Equal(200, result[0].VoteCount);
            Assert.Equal(50, result[1].VoteCount);
            Assert.Equal(100, result[2].VoteCount);
        }

        [Fact]
        public void OrderByLanguageDescending_NullRequestedLanguage_DefaultsToEnglish()
        {
            var images = new[]
            {
                new RemoteImageInfo { Language = "fr", CommunityRating = 9.0, VoteCount = 500 },
                new RemoteImageInfo { Language = "en", CommunityRating = 5.0, VoteCount = 10 },
            };

            var result = images.OrderByLanguageDescending(null!).ToList();

            // With null requested language, English becomes the preferred language (score 4)
            Assert.Equal("en", result[0].Language);
            Assert.Equal("fr", result[1].Language);
        }

        [Fact]
        public void OrderByLanguageDescending_EnglishRequested_NoDoubleBoost()
        {
            // When requested language IS English, "en" gets score 4 (requested match),
            // no-language gets score 2, others get score 0
            var images = new[]
            {
                new RemoteImageInfo { Language = null, CommunityRating = 9.0, VoteCount = 500 },
                new RemoteImageInfo { Language = "en", CommunityRating = 3.0, VoteCount = 10 },
                new RemoteImageInfo { Language = "fr", CommunityRating = 8.0, VoteCount = 300 },
            };

            var result = images.OrderByLanguageDescending("en").ToList();

            Assert.Equal("en", result[0].Language);
            Assert.Null(result[1].Language);
            Assert.Equal("fr", result[2].Language);
        }

        [Fact]
        public void OrderByLanguageDescending_FullPriorityOrder()
        {
            var images = new[]
            {
                new RemoteImageInfo { Language = "fr", CommunityRating = 9.0, VoteCount = 500 },
                new RemoteImageInfo { Language = null, CommunityRating = 8.0, VoteCount = 400 },
                new RemoteImageInfo { Language = "en", CommunityRating = 7.0, VoteCount = 300 },
                new RemoteImageInfo { Language = "de", CommunityRating = 6.0, VoteCount = 200 },
            };

            var result = images.OrderByLanguageDescending("de").ToList();

            // Expected order: de (requested) > en > no-language > fr (other)
            Assert.Equal("de", result[0].Language);
            Assert.Equal("en", result[1].Language);
            Assert.Null(result[2].Language);
            Assert.Equal("fr", result[3].Language);
        }
    }
}
