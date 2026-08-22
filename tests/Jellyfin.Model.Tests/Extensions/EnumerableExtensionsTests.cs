using System.Linq;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Providers;
using Xunit;

namespace Jellyfin.Model.Tests.Extensions;

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

        // With null requested language, English becomes the preferred language (score 2)
        Assert.Equal("en", result[0].Language);
        Assert.Equal("fr", result[1].Language);
    }

    [Fact]
    public void OrderByLanguageDescending_EnglishRequested_NoDoubleBoost()
    {
        // When requested language IS English, "en" gets score 2 (requested match),
        // no-language gets score 1, others get score 0
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

    [Fact]
    public void OrderByLanguageDescending_WithPreferredImageLanguages_RespectsCustomOrder()
    {
        var images = new[]
        {
            new RemoteImageInfo { Language = "fr", CommunityRating = 8.0, VoteCount = 100 },
            new RemoteImageInfo { Language = "de", CommunityRating = 8.0, VoteCount = 100 },
            new RemoteImageInfo { Language = "es", CommunityRating = 8.0, VoteCount = 100 },
        };

        var preferredOptions = new[]
        {
            new ImageLanguageOption { OptionType = ImageLanguageType.LanguageCode, Language = "es" },
            new ImageLanguageOption { OptionType = ImageLanguageType.LanguageCode, Language = "de" }
        };

        var result = images.OrderByLanguageDescending("en", preferredImageLanguages: preferredOptions).ToList();

        // Should completely ignore the "en" requested fallback and prioritize "es" then "de"
        Assert.Equal("es", result[0].Language);
        Assert.Equal("de", result[1].Language);
        Assert.Equal("fr", result[2].Language);
    }

    [Fact]
    public void OrderByLanguageDescending_WithOriginalLanguage_PrefersOriginal()
    {
        var images = new[]
        {
            new RemoteImageInfo { Language = "de", CommunityRating = 8.0, VoteCount = 100 },
            new RemoteImageInfo { Language = "en", CommunityRating = 9.0, VoteCount = 200 },
        };

        var preferredOptions = new[]
        {
            new ImageLanguageOption { OptionType = ImageLanguageType.OriginalLanguage }
        };

        var result = images.OrderByLanguageDescending("fr", originalLanguage: "de", preferredImageLanguages: preferredOptions).ToList();

        // "de" is the original language and should be preferred, even with a lower rating.
        Assert.Equal("de", result[0].Language);
        Assert.Equal("en", result[1].Language);
    }

    [Fact]
    public void OrderByLanguageDescending_WithOriginalLanguage_IgnoresNull()
    {
        var images = new[]
        {
            new RemoteImageInfo { Language = "en", CommunityRating = 9.0, VoteCount = 200 },
            new RemoteImageInfo { Language = null, CommunityRating = 8.0, VoteCount = 100 },
        };

        var preferredOptions = new[]
        {
            new ImageLanguageOption { OptionType = ImageLanguageType.OriginalLanguage },
            new ImageLanguageOption { OptionType = ImageLanguageType.LanguageCode, Language = "en" }
        };

        // A null originalLanguage should not match the image with no language.
        var result = images.OrderByLanguageDescending("fr", originalLanguage: null, preferredImageLanguages: preferredOptions).ToList();

        Assert.Equal("en", result[0].Language);
        Assert.Null(result[1].Language);
    }

    [Fact]
    public void OrderByLanguageDescending_WithNoLanguageOption_PrioritizesImagesWithoutLanguage()
    {
        var images = new[]
        {
            new RemoteImageInfo { Language = "en", CommunityRating = 8.0, VoteCount = 100 },
            new RemoteImageInfo { Language = null, CommunityRating = 8.0, VoteCount = 100 },
        };

        var preferredOptions = new[]
        {
            new ImageLanguageOption { OptionType = ImageLanguageType.NoLanguage }
        };

        var result = images.OrderByLanguageDescending("en", preferredImageLanguages: preferredOptions).ToList();

        // No language should come first based on explicit configuration
        Assert.Null(result[0].Language);
        Assert.Equal("en", result[1].Language);
    }
}
