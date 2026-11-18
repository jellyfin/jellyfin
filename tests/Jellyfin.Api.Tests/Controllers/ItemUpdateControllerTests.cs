using System;
using System.Threading.Tasks;
using Jellyfin.Api.Controllers;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

[Collection("LibraryManagerTests")]
public sealed class ItemUpdateControllerTests : IDisposable
{
    private readonly ItemUpdateController _subject;
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly ILocalizationManager? _previousLocalizationManager;

    public ItemUpdateControllerTests()
    {
        // Children are scored and saved through BaseItem's process-wide statics; restored in Dispose.
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousLocalizationManager = BaseItem.LocalizationManager;
        BaseItem.LibraryManager = Mock.Of<ILibraryManager>();
        BaseItem.LocalizationManager = Mock.Of<ILocalizationManager>();

        _subject = new ItemUpdateController(
            Mock.Of<IFileSystem>(),
            Mock.Of<ILibraryManager>(),
            Mock.Of<IProviderManager>(),
            Mock.Of<ILocalizationManager>(),
            Mock.Of<IServerConfigurationManager>());
    }

    [Fact]
    public async Task UpdateItem_WhenOnlyTagsFieldSupplied_DoesNotThrowAndAppliesTags()
    {
        // Regression test for https://github.com/jellyfin/jellyfin/issues/17366
        // A partial update payload that only sets "Tags" leaves every other
        // BaseItemDto collection property null (they have no default
        // initializer). Genres and ProviderIds used to be fed straight into
        // Distinct()/ToList() without a null check, so this call used to throw
        // ArgumentNullException before the fix below was applied.
        var movie = new Movie();
        var request = new BaseItemDto
        {
            Tags = new[] { "new-tag-1", "new-tag-2" }
        };

        await InvokeUpdateItem(request, movie);

        Assert.Equal(new[] { "new-tag-1", "new-tag-2" }, movie.Tags);
        Assert.Empty(movie.Genres);
        Assert.Empty(movie.ProviderIds);
    }

    [Fact]
    public async Task UpdateItem_WhenGenresAndProviderIdsOmitted_LeavesExistingValuesUnchanged()
    {
        var movie = new Movie
        {
            Genres = new[] { "Action" }
        };
        movie.ProviderIds["Imdb"] = "tt1234567";

        var request = new BaseItemDto
        {
            Tags = Array.Empty<string>()
        };

        await InvokeUpdateItem(request, movie);

        Assert.Equal(new[] { "Action" }, movie.Genres);
        Assert.Equal("tt1234567", movie.ProviderIds["Imdb"]);
    }

    public void Dispose()
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.LocalizationManager = _previousLocalizationManager!;
    }

    [Fact]
    public async Task UpdateItem_SeriesRatingUnchanged_KeepsEpisodeRatings()
    {
        // Regression test for https://github.com/jellyfin/jellyfin/issues/18241
        var (series, season, episodes) = CreateSeries("TV-PG", "TV-PG", "TV-14", null);

        await InvokeUpdateItem(new BaseItemDto { Overview = "New overview", OfficialRating = "TV-PG" }, series);

        Assert.Equal("TV-PG", season.OfficialRating);
        Assert.Equal("TV-14", episodes[1].OfficialRating);
        Assert.Null(episodes[2].OfficialRating);
    }

    [Fact]
    public async Task UpdateItem_SeriesRatingChanged_PropagatesOnlyToChildrenFollowingIt()
    {
        var (series, season, episodes) = CreateSeries("TV-PG", "TV-PG", "TV-14", null);
        var locked = new Episode { OfficialRating = "TV-PG", PreferredMetadataCountryCode = "us", LockedFields = [MetadataField.OfficialRating] };
        season.Children = [.. episodes, locked];

        await InvokeUpdateItem(new BaseItemDto { OfficialRating = "TV-MA" }, series);

        Assert.Equal("TV-MA", season.OfficialRating);
        Assert.Equal("TV-MA", episodes[0].OfficialRating);
        Assert.Equal("TV-14", episodes[1].OfficialRating);
        Assert.Equal("TV-MA", episodes[2].OfficialRating);
        Assert.Equal("TV-PG", locked.OfficialRating);
    }

    [Fact]
    public async Task UpdateItem_SeriesCustomRatingChanged_PropagatesIt()
    {
        var (series, season, episodes) = CreateSeries("TV-PG", "TV-PG", "TV-14", null);
        episodes[0].CustomRating = "TV-Y";

        await InvokeUpdateItem(new BaseItemDto { OfficialRating = "TV-PG", CustomRating = "XXX" }, series);

        Assert.Equal("XXX", season.CustomRating);
        Assert.Equal("TV-Y", episodes[0].CustomRating);
        Assert.Equal("XXX", episodes[1].CustomRating);
        Assert.Equal("TV-14", episodes[1].OfficialRating);
    }

    [Fact]
    public async Task UpdateItem_SeasonRatingChanged_KeepsEpisodesWithTheirOwnRating()
    {
        var (_, season, episodes) = CreateSeries("TV-PG", "TV-PG", "TV-14", null);

        await InvokeUpdateItem(new BaseItemDto { OfficialRating = "TV-MA" }, season);

        Assert.Equal("TV-MA", episodes[0].OfficialRating);
        Assert.Equal("TV-14", episodes[1].OfficialRating);
        Assert.Equal("TV-MA", episodes[2].OfficialRating);
    }

    private static (Series Series, Season Season, Episode[] Episodes) CreateSeries(string seriesRating, params string?[] episodeRatings)
    {
        var episodes = Array.ConvertAll(episodeRatings, r => new Episode { OfficialRating = r, PreferredMetadataCountryCode = "us" });
        var season = new Season { OfficialRating = seriesRating, PreferredMetadataCountryCode = "us", Children = episodes };
        var series = new Series { OfficialRating = seriesRating, PreferredMetadataCountryCode = "us", Children = [season] };
        return (series, season, episodes);
    }

    private Task InvokeUpdateItem(BaseItemDto request, BaseItem item)
    {
        return _subject.UpdateItem(request, item);
    }
}
