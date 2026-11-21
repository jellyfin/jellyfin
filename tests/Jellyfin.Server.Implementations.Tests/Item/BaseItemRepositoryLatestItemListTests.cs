using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class BaseItemRepositoryLatestItemListTests : SqliteDbTestFixture
{
    private readonly BaseItemRepository _repository;
    private readonly string _movieTypeName;
    private readonly string _seriesTypeName;
    private readonly string _seasonTypeName;
    private readonly string _episodeTypeName;

    public BaseItemRepositoryLatestItemListTests()
    {
        var itemTypeLookup = new ItemTypeLookup();
        _movieTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie]!;
        _seriesTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Series]!;
        _seasonTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Season]!;
        _episodeTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Episode]!;

        _repository = CreateBaseItemRepository(itemTypeLookup);
    }

    [Fact]
    public void GetLatestItemList_MixedLibrary_OrdersGroupedShowByNewestEpisode()
    {
        var now = DateTime.UtcNow;
        var movieCreated = now.AddDays(-2);
        var seriesCreated = now.AddDays(-30);
        var seasonCreated = now.AddDays(-30);
        var ep1Created = now.AddHours(-1);
        var ep2Created = now.AddHours(-2);

        var movieId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();
        var ep1Id = Guid.NewGuid();
        var ep2Id = Guid.NewGuid();

        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateMovieEntity(movieId, "Old Movie", movieId.ToString("N"), movieCreated));
            ctx.BaseItems.Add(CreateSeriesEntity(seriesId, "Active Series", seriesCreated));
            ctx.BaseItems.Add(CreateSeasonEntity(seasonId, seriesId, "Season 1", seasonCreated));
            ctx.BaseItems.Add(CreateEpisodeEntity(ep1Id, seriesId, seasonId, "Active Series", "Episode 1", ep1Created));
            ctx.BaseItems.Add(CreateEpisodeEntity(ep2Id, seriesId, seasonId, "Active Series", "Episode 2", ep2Created));
            ctx.SaveChanges();
        }

        var query = new InternalItemsQuery();
        var result = _repository.GetLatestItemList(query, CollectionType.unknown);

        // In a mixed library, the Series should be ranked ahead of the Movie because its newest episode was added today.
        Assert.Equal(2, result.Count);
        Assert.Equal(seriesId, result[0].Id);
        Assert.Equal(movieId, result[1].Id);

        // The returned DTO should preserve the container's original DateCreated.
        Assert.Equal(seriesCreated, result[0].DateCreated);
    }

    [Fact]
    public void GetLatestItemList_MixedLibrary_LimitPreservesNewestShow()
    {
        var now = DateTime.UtcNow;
        var seriesCreated = now.AddDays(-30);
        var seasonCreated = now.AddDays(-30);

        var seriesId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();

        using (var ctx = CreateDbContext())
        {
            // Seed 5 movies created between 2 and 6 days ago
            for (var i = 1; i <= 5; i++)
            {
                var id = Guid.NewGuid();
                ctx.BaseItems.Add(CreateMovieEntity(id, $"Movie {i}", id.ToString("N"), now.AddDays(-1 - i)));
            }

            // Seed an older series with 2 episodes added today
            ctx.BaseItems.Add(CreateSeriesEntity(seriesId, "Active Series", seriesCreated));
            ctx.BaseItems.Add(CreateSeasonEntity(seasonId, seriesId, "Season 1", seasonCreated));
            ctx.BaseItems.Add(CreateEpisodeEntity(Guid.NewGuid(), seriesId, seasonId, "Active Series", "Episode 1", now.AddHours(-1)));
            ctx.BaseItems.Add(CreateEpisodeEntity(Guid.NewGuid(), seriesId, seasonId, "Active Series", "Episode 2", now.AddHours(-2)));
            ctx.SaveChanges();
        }

        var query = new InternalItemsQuery { Limit = 1 };
        var result = _repository.GetLatestItemList(query, CollectionType.unknown);

        var item = Assert.Single(result);
        Assert.Equal(seriesId, item.Id);
    }

    [Fact]
    public void GetLatestItemList_MixedLibrary_SingleEpisode_OrdersByEpisodeDate()
    {
        var now = DateTime.UtcNow;
        var movieCreated = now.AddDays(-2);
        var seriesCreated = now.AddDays(-30);
        var seasonCreated = now.AddDays(-30);
        var oldEpCreated = now.AddDays(-30);
        var newEpCreated = now.AddHours(-1);

        var movieId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();
        var oldEpId = Guid.NewGuid();
        var newEpId = Guid.NewGuid();

        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateMovieEntity(movieId, "Movie", movieId.ToString("N"), movieCreated));
            ctx.BaseItems.Add(CreateSeriesEntity(seriesId, "Series", seriesCreated));
            ctx.BaseItems.Add(CreateSeasonEntity(seasonId, seriesId, "Season 1", seasonCreated));
            ctx.BaseItems.Add(CreateEpisodeEntity(oldEpId, seriesId, seasonId, "Series", "Episode 1", oldEpCreated));
            ctx.BaseItems.Add(CreateEpisodeEntity(newEpId, seriesId, seasonId, "Series", "Episode 2", newEpCreated));
            ctx.SaveChanges();
        }

        var query = new InternalItemsQuery();
        var result = _repository.GetLatestItemList(query, CollectionType.unknown);

        // When only 1 of multiple episodes is newly added, the Episode entity itself is returned and ranked first.
        Assert.Equal(2, result.Count);
        Assert.Equal(newEpId, result[0].Id);
        Assert.Equal(movieId, result[1].Id);
    }

    [Fact]
    public void GetLatestItemList_PureTvShows_OrdersByNewestEpisode()
    {
        var now = DateTime.UtcNow;
        var series1Id = Guid.NewGuid();
        var season1Id = Guid.NewGuid();
        var series2Id = Guid.NewGuid();
        var season2Id = Guid.NewGuid();

        using (var ctx = CreateDbContext())
        {
            // Series 1 created 30 days ago, episodes added today
            ctx.BaseItems.Add(CreateSeriesEntity(series1Id, "Series 1", now.AddDays(-30)));
            ctx.BaseItems.Add(CreateSeasonEntity(season1Id, series1Id, "Season 1", now.AddDays(-30)));
            ctx.BaseItems.Add(CreateEpisodeEntity(Guid.NewGuid(), series1Id, season1Id, "Series 1", "S01E01", now.AddHours(-1)));
            ctx.BaseItems.Add(CreateEpisodeEntity(Guid.NewGuid(), series1Id, season1Id, "Series 1", "S01E02", now.AddHours(-2)));

            // Series 2 created 5 days ago, episodes added 3 days ago
            ctx.BaseItems.Add(CreateSeriesEntity(series2Id, "Series 2", now.AddDays(-5)));
            ctx.BaseItems.Add(CreateSeasonEntity(season2Id, series2Id, "Season 1", now.AddDays(-5)));
            ctx.BaseItems.Add(CreateEpisodeEntity(Guid.NewGuid(), series2Id, season2Id, "Series 2", "S01E01", now.AddDays(-3)));
            ctx.BaseItems.Add(CreateEpisodeEntity(Guid.NewGuid(), series2Id, season2Id, "Series 2", "S01E02", now.AddDays(-4)));

            ctx.SaveChanges();
        }

        var query = new InternalItemsQuery();
        var result = _repository.GetLatestItemList(query, CollectionType.tvshows);

        Assert.Equal(2, result.Count);
        Assert.Equal(series1Id, result[0].Id);
        Assert.Equal(series2Id, result[1].Id);
    }

    private BaseItemEntity CreateMovieEntity(Guid id, string name, string presentationKey, DateTime dateCreated)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = _movieTypeName,
            Name = name,
            PresentationUniqueKey = presentationKey,
            DateCreated = dateCreated,
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false
        };
    }

    private BaseItemEntity CreateSeriesEntity(Guid id, string name, DateTime dateCreated)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = _seriesTypeName,
            Name = name,
            DateCreated = dateCreated,
            IsFolder = true,
            IsVirtualItem = false
        };
    }

    private BaseItemEntity CreateSeasonEntity(Guid id, Guid seriesId, string name, DateTime dateCreated)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = _seasonTypeName,
            Name = name,
            SeriesId = seriesId,
            DateCreated = dateCreated,
            IsFolder = true,
            IsVirtualItem = false
        };
    }

    private BaseItemEntity CreateEpisodeEntity(Guid id, Guid seriesId, Guid seasonId, string seriesName, string name, DateTime dateCreated)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = _episodeTypeName,
            Name = name,
            SeriesName = seriesName,
            SeriesId = seriesId,
            SeasonId = seasonId,
            DateCreated = dateCreated,
            MediaType = "Video",
            IsFolder = false,
            IsVirtualItem = false
        };
    }
}
