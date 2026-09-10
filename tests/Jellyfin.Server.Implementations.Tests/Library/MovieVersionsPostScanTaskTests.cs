using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Library.Validators;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class MovieVersionsPostScanTaskTests
{
    private readonly Mock<ILibraryManager> _libraryManager;
    private readonly Mock<IVideoVersionManager> _videoVersionManager;
    private readonly Mock<ILinkedChildrenService> _linkedChildrenService;
    private readonly MovieVersionsPostScanTask _task;

    private readonly List<Movie> _movies = [];

    public MovieVersionsPostScanTaskTests()
    {
        _libraryManager = new Mock<ILibraryManager>();
        _videoVersionManager = new Mock<IVideoVersionManager>();
        _linkedChildrenService = new Mock<ILinkedChildrenService>();

        _libraryManager
            .Setup(x => x.GetItemList(It.Is<InternalItemsQuery>(q => q.IncludeItemTypes.Contains(BaseItemKind.Movie))))
            .Returns(_movies.ToList<BaseItem>);
        _libraryManager
            .Setup(x => x.GetLibraryOptions(It.IsAny<BaseItem>()))
            .Returns(new LibraryOptions { EnableAutomaticMovieVersionGrouping = true });

        _linkedChildrenService
            .Setup(x => x.GetParentIdsWithChildType(LinkedChildType.AutoLinkedAlternateVersion))
            .Returns([]);
        _linkedChildrenService
            .Setup(x => x.GetAutoMergeExclusions())
            .Returns(new Dictionary<Guid, IReadOnlyList<Guid>>());

        _videoVersionManager
            .Setup(x => x.MergeVersionsAsync(It.IsAny<IReadOnlyList<Video>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Video> videos, bool _, CancellationToken _) => videos[0]);

        _task = new MovieVersionsPostScanTask(
            _libraryManager.Object,
            _videoVersionManager.Object,
            _linkedChildrenService.Object,
            NullLogger<MovieVersionsPostScanTask>.Instance);
    }

    [Fact]
    public async Task Run_SameMovieInTwoLibraries_MergesVersions()
    {
        var hd = AddMovie("/Movies/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");
        var uhd = AddMovie("/Movies 4K/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _videoVersionManager.Verify(
            x => x.MergeVersionsAsync(
                It.Is<IReadOnlyList<Video>>(v => v.Count == 2 && v.Contains(hd) && v.Contains(uhd)),
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_SameMovieInSeparateEditionFolders_MergesVersions()
    {
        var theatrical = AddMovie("/Movies/Blade Runner (1982) - Theatrical/movie.mkv", tmdbId: "78");
        var finalCut = AddMovie("/Movies/Blade Runner (1982) - Final Cut/movie.mkv", tmdbId: "78");

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _videoVersionManager.Verify(
            x => x.MergeVersionsAsync(
                It.Is<IReadOnlyList<Video>>(v => v.Count == 2 && v.Contains(theatrical) && v.Contains(finalCut)),
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_UnidentifiedMoviesWithSameNameAndYear_MergesVersions()
    {
        var hd = AddMovie("/Movies/Blade Runner (1982)/Blade Runner (1982).mkv");
        var uhd = AddMovie("/Movies 4K/Blade Runner (1982)/Blade Runner (1982).mkv");

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _videoVersionManager.Verify(
            x => x.MergeVersionsAsync(
                It.Is<IReadOnlyList<Video>>(v => v.Count == 2 && v.Contains(hd) && v.Contains(uhd)),
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_UnidentifiedRemakeWithDifferentYear_DoesNothing()
    {
        AddMovie("/Movies/The Thing (1982)/The Thing (1982).mkv", name: "The Thing", year: 1982);
        AddMovie("/Movies/The Thing (2011)/The Thing (2011).mkv", name: "The Thing", year: 2011);

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _videoVersionManager.Verify(
            x => x.MergeVersionsAsync(It.IsAny<IReadOnlyList<Video>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Run_UnidentifiedMoviesWithoutYear_DoesNothing()
    {
        AddMovie("/Movies/Blade Runner/Blade Runner.mkv", year: null);
        AddMovie("/Movies 4K/Blade Runner/Blade Runner.mkv", year: null);

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _videoVersionManager.Verify(
            x => x.MergeVersionsAsync(It.IsAny<IReadOnlyList<Video>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Run_SameNameAndYearButDifferentProviderIds_DoesNothing()
    {
        AddMovie("/Movies/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");
        AddMovie("/Movies 4K/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "335984");

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _videoVersionManager.Verify(
            x => x.MergeVersionsAsync(It.IsAny<IReadOnlyList<Video>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Run_LocalAlternateVersion_IsIgnored()
    {
        var primary = AddMovie("/Movies/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");
        var localAlternate = AddMovie("/Movies/Blade Runner (1982)/Blade Runner (1982) - 4K.mkv", tmdbId: "78");
        localAlternate.OwnerId = primary.Id;

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _videoVersionManager.Verify(
            x => x.MergeVersionsAsync(It.IsAny<IReadOnlyList<Video>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Run_AutoGroupingDisabled_DoesNothing()
    {
        AddMovie("/Movies/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");
        AddMovie("/Movies 4K/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");
        _libraryManager
            .Setup(x => x.GetLibraryOptions(It.IsAny<BaseItem>()))
            .Returns(new LibraryOptions { EnableAutomaticMovieVersionGrouping = false });

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _videoVersionManager.Verify(
            x => x.MergeVersionsAsync(It.IsAny<IReadOnlyList<Video>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Run_AutoGroupingTurnedOff_UndoesTheEarlierMerge()
    {
        var primary = AddMovie("/Movies/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");
        var alternate = AddMovie("/Movies 4K/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");
        Link(primary, alternate);
        _libraryManager
            .Setup(x => x.GetLibraryOptions(It.IsAny<BaseItem>()))
            .Returns(new LibraryOptions { EnableAutomaticMovieVersionGrouping = false });

        _linkedChildrenService
            .Setup(x => x.GetParentIdsWithChildType(LinkedChildType.AutoLinkedAlternateVersion))
            .Returns([primary.Id]);

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _videoVersionManager.Verify(x => x.RemoveVersionLinkAsync(primary, alternate.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Run_OneCopyInADisabledLibrary_DoesNotMerge()
    {
        var hd = AddMovie("/Movies/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");
        var uhd = AddMovie("/Movies 4K/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");
        _libraryManager
            .Setup(x => x.GetLibraryOptions(hd))
            .Returns(new LibraryOptions { EnableAutomaticMovieVersionGrouping = true });
        _libraryManager
            .Setup(x => x.GetLibraryOptions(uhd))
            .Returns(new LibraryOptions { EnableAutomaticMovieVersionGrouping = false });

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _videoVersionManager.Verify(
            x => x.MergeVersionsAsync(It.IsAny<IReadOnlyList<Video>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Run_UserSplitTheVersionsApart_DoesNotMergeThemAgain()
    {
        var hd = AddMovie("/Movies/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");
        var uhd = AddMovie("/Movies 4K/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");
        ExcludePair(hd, uhd);

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _videoVersionManager.Verify(
            x => x.MergeVersionsAsync(It.IsAny<IReadOnlyList<Video>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Run_UserSplitOneOfThreeVersions_MergesTheRemainingOnes()
    {
        var hd = AddMovie("/Movies/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");
        var uhd = AddMovie("/Movies 4K/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");
        var remux = AddMovie("/Remux/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");
        ExcludePair(hd, uhd);

        await _task.Run(new Progress<double>(), CancellationToken.None);

        // hd sorts first (no primary version yet, and ordering is stable by id), so uhd is the one left out.
        _videoVersionManager.Verify(
            x => x.MergeVersionsAsync(
                It.Is<IReadOnlyList<Video>>(v => v.Count == 2 && v.Contains(remux) && (v.Contains(hd) ^ v.Contains(uhd))),
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_UserSplitAnAlreadyMergedPair_LeavesTheUnrelatedMergeIntact()
    {
        // hd + remux are auto-merged; the user split uhd away from hd earlier.
        var hd = AddMovie("/Movies/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");
        var uhd = AddMovie("/Movies 4K/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");
        var remux = AddMovie("/Remux/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");
        Link(hd, remux);
        ExcludePair(hd, uhd);

        _linkedChildrenService
            .Setup(x => x.GetParentIdsWithChildType(LinkedChildType.AutoLinkedAlternateVersion))
            .Returns([hd.Id]);

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _videoVersionManager.Verify(
            x => x.MergeVersionsAsync(It.IsAny<IReadOnlyList<Video>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _videoVersionManager.Verify(
            x => x.RemoveVersionLinkAsync(It.IsAny<Video>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Run_AlreadyMergedGroup_IsLeftAlone()
    {
        var primary = AddMovie("/Movies/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");
        var alternate = AddMovie("/Movies 4K/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");
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
    public async Task Run_AlternateReidentified_StaleLinkIsRemoved()
    {
        var primary = AddMovie("/Movies/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");
        var stillGrouped = AddMovie("/Movies 4K/Blade Runner (1982)/Blade Runner (1982).mkv", tmdbId: "78");
        // This one was corrected to the sequel after the link was created.
        var reidentified = AddMovie("/Movies/Blade Runner 2049 (2017)/Blade Runner 2049 (2017).mkv", tmdbId: "335984");
        Link(primary, stillGrouped, reidentified);

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _videoVersionManager.Verify(x => x.RemoveVersionLinkAsync(primary, reidentified.Id, It.IsAny<CancellationToken>()), Times.Once);
        _videoVersionManager.Verify(
            x => x.RemoveVersionLinkAsync(It.IsAny<Video>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_OrphanedAutoLinks_AreCleanedUp()
    {
        // The alternate copy is gone from the library, so no candidate group remains,
        // but the auto-linked pair is still recorded.
        var primary = CreateMovie("/Movies/Blade Runner (1982)/Blade Runner (1982).mkv", "Blade Runner", 1982, "78");
        var alternate = CreateMovie("/Movies 4K/Blade Runner (1982)/Blade Runner (1982).mkv", "Blade Runner", 1982, "78");
        Link(primary, alternate);

        _linkedChildrenService
            .Setup(x => x.GetParentIdsWithChildType(LinkedChildType.AutoLinkedAlternateVersion))
            .Returns([primary.Id]);

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _videoVersionManager.Verify(x => x.RemoveVersionLinkAsync(primary, alternate.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Run_AutoLinkedEpisodePrimary_IsNotTouched()
    {
        // The episode post-scan task owns episode links; this task must leave them alone.
        var episodePrimary = new MediaBrowser.Controller.Entities.TV.Episode { Id = Guid.NewGuid() };
        episodePrimary.LinkedAlternateVersions =
            [new LinkedChild { ItemId = Guid.NewGuid(), Type = LinkedChildType.AutoLinkedAlternateVersion }];
        _libraryManager.Setup(x => x.GetItemById(episodePrimary.Id)).Returns(episodePrimary);

        _linkedChildrenService
            .Setup(x => x.GetParentIdsWithChildType(LinkedChildType.AutoLinkedAlternateVersion))
            .Returns([episodePrimary.Id]);

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _videoVersionManager.Verify(
            x => x.RemoveVersionLinkAsync(It.IsAny<Video>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private Movie AddMovie(string path, string? name = "Blade Runner", int? year = 1982, string? tmdbId = null)
    {
        var movie = CreateMovie(path, name, year, tmdbId);
        _movies.Add(movie);
        return movie;
    }

    private Movie CreateMovie(string path, string? name, int? year, string? tmdbId)
    {
        var movie = new Movie
        {
            Id = Guid.NewGuid(),
            Path = path,
            Name = name,
            ProductionYear = year
        };

        if (!string.IsNullOrEmpty(tmdbId))
        {
            movie.SetProviderId(MetadataProvider.Tmdb, tmdbId);
        }

        _libraryManager.Setup(x => x.GetItemById(movie.Id)).Returns(movie);
        return movie;
    }

    private void ExcludePair(Movie first, Movie second)
    {
        _linkedChildrenService
            .Setup(x => x.GetAutoMergeExclusions())
            .Returns(new Dictionary<Guid, IReadOnlyList<Guid>>
            {
                [first.Id] = [second.Id],
                [second.Id] = [first.Id]
            });
    }

    private static void Link(Movie primary, params Movie[] alternates)
    {
        foreach (var alternate in alternates)
        {
            alternate.PrimaryVersionId = primary.Id;
        }

        primary.LinkedAlternateVersions =
            [.. alternates.Select(a => new LinkedChild { ItemId = a.Id, Type = LinkedChildType.AutoLinkedAlternateVersion })];
    }
}
