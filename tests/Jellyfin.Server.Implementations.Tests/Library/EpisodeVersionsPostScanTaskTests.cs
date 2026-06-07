using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Library.Validators;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class EpisodeVersionsPostScanTaskTests
{
    private const string SeriesKey = "tvdb-12345";

    private readonly Mock<ILibraryManager> _libraryManager;
    private readonly Mock<IVideoVersionManager> _videoVersionManager;
    private readonly Mock<ILinkedChildrenService> _linkedChildrenService;
    private readonly EpisodeVersionsPostScanTask _task;

    private readonly List<Series> _series = [];
    private readonly List<Season> _seasons = [];
    private readonly List<Episode> _episodes = [];

    public EpisodeVersionsPostScanTaskTests()
    {
        _libraryManager = new Mock<ILibraryManager>();
        _videoVersionManager = new Mock<IVideoVersionManager>();
        _linkedChildrenService = new Mock<ILinkedChildrenService>();

        _libraryManager
            .Setup(x => x.GetItemList(It.Is<InternalItemsQuery>(q => q.IncludeItemTypes.Contains(BaseItemKind.Series))))
            .Returns(() => _series.ToList<BaseItem>());
        _libraryManager
            .Setup(x => x.GetItemList(It.Is<InternalItemsQuery>(q => q.IncludeItemTypes.Contains(BaseItemKind.Season))))
            .Returns(() => _seasons.ToList<BaseItem>());
        _libraryManager
            .Setup(x => x.GetItemList(It.Is<InternalItemsQuery>(q => q.IncludeItemTypes.Contains(BaseItemKind.Episode) && q.SeriesPresentationUniqueKey == SeriesKey)))
            .Returns(() => _episodes.ToList<BaseItem>());

        _linkedChildrenService
            .Setup(x => x.GetParentIdsWithChildType(LinkedChildType.AutoLinkedAlternateVersion))
            .Returns([]);

        _videoVersionManager
            .Setup(x => x.MergeVersionsAsync(It.IsAny<IReadOnlyList<Video>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Video> videos, bool _, CancellationToken _) => videos[0]);

        _task = new EpisodeVersionsPostScanTask(
            _libraryManager.Object,
            _videoVersionManager.Object,
            _linkedChildrenService.Object,
            NullLogger<EpisodeVersionsPostScanTask>.Instance);
    }

    [Fact]
    public async Task Run_SeriesSpreadAcrossTwoFolders_MergesSameNumberedEpisodes()
    {
        AddSeries("/Shows/Spider Noir S01 (BW)");
        AddSeries("/Shows/Spider Noir S01 (Color)");
        var bw1 = AddEpisode("/Shows/Spider Noir S01 (BW)/S01E01.mkv", 1, 1);
        var bw2 = AddEpisode("/Shows/Spider Noir S01 (BW)/S01E02.mkv", 1, 2);
        var color1 = AddEpisode("/Shows/Spider Noir S01 (Color)/S01E01.mkv", 1, 1);
        var color2 = AddEpisode("/Shows/Spider Noir S01 (Color)/S01E02.mkv", 1, 2);

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _videoVersionManager.Verify(
            x => x.MergeVersionsAsync(
                It.Is<IReadOnlyList<Video>>(v => v.Count == 2 && v.Contains(bw1) && v.Contains(color1)),
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _videoVersionManager.Verify(
            x => x.MergeVersionsAsync(
                It.Is<IReadOnlyList<Video>>(v => v.Count == 2 && v.Contains(bw2) && v.Contains(color2)),
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _videoVersionManager.Verify(
            x => x.MergeVersionsAsync(It.IsAny<IReadOnlyList<Video>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Run_SingleSeriesWithDuplicateSeasonIndexes_MergesSameNumberedEpisodes()
    {
        var series = AddSeries("/Shows/Spider Noir");
        AddSeason(series, 1);
        AddSeason(series, 1);
        var bw = AddEpisode("/Shows/Spider Noir/S01 (BW)/S01E01.mkv", 1, 1);
        var color = AddEpisode("/Shows/Spider Noir/S01 (Color)/S01E01.mkv", 1, 1);

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _videoVersionManager.Verify(
            x => x.MergeVersionsAsync(
                It.Is<IReadOnlyList<Video>>(v => v.Count == 2 && v.Contains(bw) && v.Contains(color)),
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_SingleFolderWithoutDuplicateSeasons_DoesNothing()
    {
        var series = AddSeries("/Shows/Spider Noir");
        AddSeason(series, 1);
        AddSeason(series, 2);
        AddEpisode("/Shows/Spider Noir/Season 01/S01E01.mkv", 1, 1);

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _videoVersionManager.Verify(
            x => x.MergeVersionsAsync(It.IsAny<IReadOnlyList<Video>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Run_AlreadyMergedGroup_IsLeftAlone()
    {
        AddSeries("/Shows/Spider Noir S01 (BW)");
        AddSeries("/Shows/Spider Noir S01 (Color)");
        var primary = AddEpisode("/Shows/Spider Noir S01 (BW)/S01E01.mkv", 1, 1);
        var alternate = AddEpisode("/Shows/Spider Noir S01 (Color)/S01E01.mkv", 1, 1);
        Link(primary, alternate);

        _linkedChildrenService
            .Setup(x => x.GetParentIdsWithChildType(LinkedChildType.AutoLinkedAlternateVersion))
            .Returns([primary.Id]);

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _videoVersionManager.Verify(
            x => x.MergeVersionsAsync(It.IsAny<IReadOnlyList<Video>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _videoVersionManager.Verify(
            x => x.RemoveVersionLinkAsync(It.IsAny<Video>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Run_EpisodeNoLongerSameNumber_StaleLinkIsRemoved()
    {
        AddSeries("/Shows/Spider Noir S01 (BW)");
        AddSeries("/Shows/Spider Noir S01 (Color)");
        var primary = AddEpisode("/Shows/Spider Noir S01 (BW)/S01E01.mkv", 1, 1);
        // The alternate was renumbered to E03 after the link was created.
        var alternate = AddEpisode("/Shows/Spider Noir S01 (Color)/S01E03.mkv", 1, 3);
        Link(primary, alternate);

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _videoVersionManager.Verify(x => x.RemoveVersionLinkAsync(primary, alternate.Id, It.IsAny<CancellationToken>()), Times.Once);
        _videoVersionManager.Verify(
            x => x.MergeVersionsAsync(It.IsAny<IReadOnlyList<Video>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Run_OrphanedAutoLinks_AreCleanedUp()
    {
        // No candidate series groups at all, but a leftover auto-linked pair exists,
        // e.g. because the series are no longer grouped together.
        var primary = CreateEpisode("/Shows/Spider Noir S01 (BW)/S01E01.mkv", 1, 1);
        var alternate = CreateEpisode("/Shows/Spider Noir S01 (Color)/S01E01.mkv", 1, 1);
        Link(primary, alternate);

        _linkedChildrenService
            .Setup(x => x.GetParentIdsWithChildType(LinkedChildType.AutoLinkedAlternateVersion))
            .Returns([primary.Id]);

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _videoVersionManager.Verify(x => x.RemoveVersionLinkAsync(primary, alternate.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    private Series AddSeries(string path)
    {
        var series = new Series
        {
            Id = Guid.NewGuid(),
            Path = path,
            Name = "Spider Noir",
            PresentationUniqueKey = SeriesKey
        };
        _series.Add(series);
        _libraryManager.Setup(x => x.GetItemById(series.Id)).Returns(series);
        return series;
    }

    private Season AddSeason(Series series, int indexNumber)
    {
        var season = new Season
        {
            Id = Guid.NewGuid(),
            IndexNumber = indexNumber,
            SeriesId = series.Id,
            SeriesPresentationUniqueKey = SeriesKey
        };
        _seasons.Add(season);
        return season;
    }

    private Episode AddEpisode(string path, int seasonNumber, int episodeNumber)
    {
        var episode = CreateEpisode(path, seasonNumber, episodeNumber);
        _episodes.Add(episode);
        return episode;
    }

    private Episode CreateEpisode(string path, int seasonNumber, int episodeNumber)
    {
        var episode = new Episode
        {
            Id = Guid.NewGuid(),
            Path = path,
            ParentIndexNumber = seasonNumber,
            IndexNumber = episodeNumber,
            SeriesName = "Spider Noir",
            SeriesPresentationUniqueKey = SeriesKey
        };
        _libraryManager.Setup(x => x.GetItemById(episode.Id)).Returns(episode);
        return episode;
    }

    private static void Link(Episode primary, Episode alternate)
    {
        alternate.PrimaryVersionId = primary.Id;
        primary.LinkedAlternateVersions = [new LinkedChild { ItemId = alternate.Id, Type = LinkedChildType.AutoLinkedAlternateVersion }];
    }
}
