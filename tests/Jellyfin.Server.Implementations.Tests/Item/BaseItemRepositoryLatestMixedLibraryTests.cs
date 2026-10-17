using System;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Tests the sort order of GetLatestItemList for CollectionType.unknown (Mixed Movies and Shows).
/// Verifies that TV shows are ordered by their newest episode's date (MaxDate), not by the
/// Season or Series container's own DateCreated — fixes #18415.
/// </summary>
public sealed class BaseItemRepositoryLatestMixedLibraryTests : SqliteDbTestFixture
{
    private readonly BaseItemRepository _repository;
    private readonly string _movieTypeName;
    private readonly string _seriesTypeName;
    private readonly string _seasonTypeName;

    public BaseItemRepositoryLatestMixedLibraryTests()
    {
        var itemTypeLookup = new ItemTypeLookup();
        _movieTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie];
        _seriesTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Series];
        _seasonTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Season];
        _repository = CreateBaseItemRepository(itemTypeLookup);
    }

    /// <summary>
    /// Reproduces #18415: a show with episodes added today should appear before a movie added
    /// last week, even when the show's Series container was created a year ago.
    /// Without the fix the merge key is Series.DateCreated (old), so the movie wins.
    /// With the fix the merge key is MaxDate (newest episode date), so the show wins.
    /// </summary>
    [Fact]
    public void GetLatestItemList_MixedLibrary_TvShowSortsOnNewestEpisodeNotContainerDate()
    {
        var seriesId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
        var seasonId = Guid.Parse("a0000000-0000-0000-0000-000000000002");
        var episode1Id = Guid.Parse("a0000000-0000-0000-0000-000000000003");
        var episode2Id = Guid.Parse("a0000000-0000-0000-0000-000000000004");
        var movieId = Guid.Parse("a0000000-0000-0000-0000-000000000005");

        var oldDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var movieDate = new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc);
        var episodeDate = new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc);

        using (var ctx = CreateDbContext())
        {
            // Series container — created a year ago.
            ctx.BaseItems.Add(new BaseItemEntity
            {
                Id = seriesId,
                Type = _seriesTypeName,
                Name = "Test Show",
                IsFolder = true,
                IsVirtualItem = false,
                DateCreated = oldDate
            });

            // Season — also old.
            ctx.BaseItems.Add(new BaseItemEntity
            {
                Id = seasonId,
                Type = _seasonTypeName,
                Name = "Season 1",
                IsFolder = true,
                IsVirtualItem = false,
                SeriesId = seriesId,
                DateCreated = oldDate
            });

            // Two episodes added today: MaxDate = episodeDate (recent).
            ctx.BaseItems.Add(new BaseItemEntity
            {
                Id = episode1Id,
                Type = "MediaBrowser.Controller.Entities.TV.Episode",
                Name = "S01E01",
                IsFolder = false,
                IsVirtualItem = false,
                SeriesName = "Test Show",
                SeriesId = seriesId,
                SeasonId = seasonId,
                DateCreated = episodeDate
            });

            ctx.BaseItems.Add(new BaseItemEntity
            {
                Id = episode2Id,
                Type = "MediaBrowser.Controller.Entities.TV.Episode",
                Name = "S01E02",
                IsFolder = false,
                IsVirtualItem = false,
                SeriesName = "Test Show",
                SeriesId = seriesId,
                SeasonId = seasonId,
                DateCreated = episodeDate
            });

            // Movie added more recently than the series creation but before the new episodes.
            ctx.BaseItems.Add(new BaseItemEntity
            {
                Id = movieId,
                Type = _movieTypeName,
                Name = "Test Movie",
                IsFolder = false,
                IsVirtualItem = false,
                PresentationUniqueKey = "test-movie-pk",
                DateCreated = movieDate
            });

            ctx.SaveChanges();
        }

        var query = new InternalItemsQuery { Limit = 10 };
        var result = _repository.GetLatestItemList(query, CollectionType.unknown);

        // Both the show group and the movie should appear.
        Assert.Equal(2, result.Count);

        // The show (MaxDate = Oct 14) must sort before the movie (DateCreated = Oct 8).
        Assert.Equal(seriesId, result[0].Id);
        Assert.Equal(movieId, result[1].Id);
    }
}
