using System;
using System.Collections.Generic;
using Emby.Server.Implementations.Dto;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Common;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Trickplay;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Dto;

public class DtoServiceTests
{
    private readonly Mock<ILibraryManager> _libraryManagerMock;
    private readonly Mock<IUserDataManager> _userDataManagerMock;
    private readonly DtoService _dtoService;

    public DtoServiceTests()
    {
        _libraryManagerMock = new Mock<ILibraryManager>();
        _userDataManagerMock = new Mock<IUserDataManager>();

        var imageProcessor = new Mock<IImageProcessor>();
        // Deterministic tag derived from the image so each item gets a distinct, assertable tag.
        imageProcessor
            .Setup(x => x.GetImageCacheTag(It.IsAny<BaseItem>(), It.IsAny<ItemImageInfo>()))
            .Returns((BaseItem _, ItemImageInfo image) => "tag:" + image.Path);

        var appHost = new Mock<IApplicationHost>();
        appHost.Setup(x => x.SystemId).Returns("test-server");

        // Video.SourceType probes the active-recording manager; provide one so it doesn't NRE.
        Video.RecordingsManager = new Mock<IRecordingsManager>().Object;

        _dtoService = new DtoService(
            NullLogger<DtoService>.Instance,
            _libraryManagerMock.Object,
            _userDataManagerMock.Object,
            imageProcessor.Object,
            new Mock<IProviderManager>().Object,
            new Mock<IRecordingsManager>().Object,
            appHost.Object,
            new Mock<IMediaSourceManager>().Object,
            new Lazy<ILiveTvManager>(() => new Mock<ILiveTvManager>().Object),
            new Mock<ITrickplayManager>().Object,
            new Mock<IChapterManager>().Object);

        // Episode.Series / Episode.Season resolve through the static BaseItem.LibraryManager.
        BaseItem.LibraryManager = _libraryManagerMock.Object;
    }

    [Fact]
    public void GetBaseItemDto_Episode_AttachesSeasonPosterAsParentPrimaryImage()
    {
        var (episode, season, _) = BuildEpisode(seasonHasPoster: true);
        var options = new DtoOptions(false) { Fields = [ItemFields.PrimaryImageAspectRatio] };

        var dto = _dtoService.GetBaseItemDto(episode, options);

        // The season poster is attached additively; the episode keeps its own primary and 16:9 ratio,
        // and clients decide per view whether to prefer the parent/series poster over the episode still.
        Assert.NotNull(dto.ImageTags);
        Assert.True(dto.ImageTags.ContainsKey(ImageType.Primary));
        Assert.NotNull(dto.SeriesPrimaryImageTag);
        Assert.Equal(season.Id, dto.ParentPrimaryImageItemId);
        Assert.Equal("tag:" + season.GetImageInfo(ImageType.Primary, 0)!.Path, dto.ParentPrimaryImageTag);
        // Aspect ratio stays the episode's own image, not the poster's.
        Assert.Equal(episode.GetDefaultPrimaryImageAspectRatio(), dto.PrimaryImageAspectRatio);
    }

    [Fact]
    public void GetBaseItemDto_Episode_ParentPrimaryImageFallsBackToSeriesWhenSeasonHasNoPoster()
    {
        var (episode, _, series) = BuildEpisode(seasonHasPoster: false);
        var options = new DtoOptions(false);

        var dto = _dtoService.GetBaseItemDto(episode, options);

        // Episode image is retained; ParentPrimaryImage falls back to the series poster.
        Assert.NotNull(dto.ImageTags);
        Assert.True(dto.ImageTags.ContainsKey(ImageType.Primary));
        Assert.NotNull(dto.SeriesPrimaryImageTag);
        Assert.Equal(series.Id, dto.ParentPrimaryImageItemId);
        Assert.Equal("tag:" + series.GetImageInfo(ImageType.Primary, 0)!.Path, dto.ParentPrimaryImageTag);
    }

    [Fact]
    public void GetBaseItemDto_Episode_WithoutParentPosters_KeepsOnlyEpisodePrimary()
    {
        var (episode, _, _) = BuildEpisode(seasonHasPoster: false, seriesHasPoster: false);
        var options = new DtoOptions(false);

        var dto = _dtoService.GetBaseItemDto(episode, options);

        // With no season or series poster there is nothing to attach; the episode keeps its own primary.
        Assert.NotNull(dto.ImageTags);
        Assert.True(dto.ImageTags.ContainsKey(ImageType.Primary));
        Assert.Null(dto.ParentPrimaryImageItemId);
    }

    [Fact]
    public void GetBaseItemDtos_SeasonWithNoRealEpisodes_ReportsVirtualEpisodesAsChildCount()
    {
        // No episode has aired yet, so RecursiveItemCount is 0. ChildCount must still report the
        // virtual episodes clients get back for the season. This deliberately does not track
        // Season.IsVirtualItem: that flag is recomputed only on a full refresh, so a season can
        // carry it while already holding real episodes.
        var (season, user) = BuildSeason(playedCount: 0, totalCount: 0, childCount: 10);
        var options = new DtoOptions(false) { EnableImages = false, Fields = [ItemFields.ChildCount, ItemFields.RecursiveItemCount] };

        var dto = _dtoService.GetBaseItemDtos([season], options, user, skipVisibilityCheck: true)[0];

        Assert.Equal(0, dto.RecursiveItemCount);
        Assert.Equal(10, dto.ChildCount);
    }

    [Fact]
    public void GetBaseItemDtos_SeasonWithRealEpisodes_KeepsRecursiveItemCountAsChildCount()
    {
        var (season, user) = BuildSeason(playedCount: 2, totalCount: 9, childCount: 11);
        var options = new DtoOptions(false) { EnableImages = false, Fields = [ItemFields.ChildCount, ItemFields.RecursiveItemCount] };

        var dto = _dtoService.GetBaseItemDtos([season], options, user, skipVisibilityCheck: true)[0];

        Assert.Equal(9, dto.RecursiveItemCount);
        // The shortcut still wins over the batched child count, which also counts virtual episodes.
        Assert.Equal(9, dto.ChildCount);
    }

    private (Season Season, User User) BuildSeason(int playedCount, int totalCount, int childCount)
    {
        var user = new User("user", "auth-provider", "reset-provider");
        var season = new Season { Id = Guid.NewGuid(), Name = "Season 2", SeriesId = Guid.NewGuid() };

        _userDataManagerMock
            .Setup(x => x.GetUserDataBatch(It.IsAny<IReadOnlyList<BaseItem>>(), user))
            .Returns(new Dictionary<Guid, UserItemData> { [season.Id] = new UserItemData { Key = "key" } });
        _userDataManagerMock
            .Setup(x => x.GetResumeUserDataBatch(It.IsAny<IReadOnlyList<BaseItem>>(), user))
            .Returns(new Dictionary<Guid, VersionResumeData>());

        _libraryManagerMock
            .Setup(x => x.GetPlayedAndTotalCountBatch(It.IsAny<IReadOnlyList<Guid>>(), user))
            .Returns(new Dictionary<Guid, (int Played, int Total)> { [season.Id] = (playedCount, totalCount) });
        _libraryManagerMock
            .Setup(x => x.GetChildCountBatch(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<Guid?>()))
            .Returns(new Dictionary<Guid, int> { [season.Id] = childCount });

        return (season, user);
    }

    private (Episode Episode, Season Season, Series Series) BuildEpisode(bool seasonHasPoster, bool seriesHasPoster = true)
    {
        // Non-local (http) paths keep aspect-ratio resolution off the image processor and on the
        // item's default ratio, which is portrait (2/3) for Season/Series and 16:9 for Episode.
        var series = new Series { Id = Guid.NewGuid(), Name = "Series" };
        if (seriesHasPoster)
        {
            series.SetImage(new ItemImageInfo { Type = ImageType.Primary, Path = "http://test/series.jpg" }, 0);
        }

        var season = new Season { Id = Guid.NewGuid(), Name = "Season", SeriesId = series.Id };
        if (seasonHasPoster)
        {
            season.SetImage(new ItemImageInfo { Type = ImageType.Primary, Path = "http://test/season.jpg" }, 0);
        }

        var episode = new Episode
        {
            Id = Guid.NewGuid(),
            Name = "Episode",
            SeasonId = season.Id,
            SeriesId = series.Id
        };
        episode.SetImage(new ItemImageInfo { Type = ImageType.Primary, Path = "http://test/episode.jpg" }, 0);

        _libraryManagerMock.Setup(x => x.GetItemById(season.Id)).Returns(season);
        _libraryManagerMock.Setup(x => x.GetItemById(series.Id)).Returns(series);

        return (episode, season, series);
    }
}
