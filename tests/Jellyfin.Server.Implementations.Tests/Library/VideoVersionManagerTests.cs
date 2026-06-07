using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class VideoVersionManagerTests
{
    private readonly Mock<ILibraryManager> _libraryManager;
    private readonly VideoVersionManager _manager;

    public VideoVersionManagerTests()
    {
        _libraryManager = new Mock<ILibraryManager>();
        _libraryManager.Setup(x => x.GetLocalAlternateVersionIds(It.IsAny<Video>())).Returns(Array.Empty<Guid>());
        _libraryManager.Setup(x => x.GetLinkedAlternateVersions(It.IsAny<Video>())).Returns(Array.Empty<Video>());
        _libraryManager.Setup(x => x.RerouteLinkedChildReferencesAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.CompletedTask);

        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsAny<string>())).Returns(MediaProtocol.File);
        mediaSourceManager.Setup(x => x.GetMediaStreams(It.IsAny<Guid>())).Returns(new List<MediaStream>());

        var segmentManager = new Mock<IMediaSegmentManager>();
        segmentManager.Setup(x => x.IsTypeSupported(It.IsAny<BaseItem>())).Returns(false);

        BaseItem.LibraryManager = _libraryManager.Object;
        BaseItem.MediaSourceManager = mediaSourceManager.Object;
        BaseItem.MediaSegmentManager = segmentManager.Object;

        _manager = new VideoVersionManager(_libraryManager.Object, NullLogger<VideoVersionManager>.Instance);
    }

    [Theory]
    [InlineData(true, LinkedChildType.AutoLinkedAlternateVersion)]
    [InlineData(false, LinkedChildType.LinkedAlternateVersion)]
    public async Task MergeVersionsAsync_LinksAlternatesOntoPrimary_WithRequestedLinkType(bool autoGrouped, LinkedChildType expectedType)
    {
        var videoA = CreateVideo("/Shows/Demo S01 (BW)/S01E01.mkv");
        var videoB = CreateVideo("/Shows/Demo S01 (Color)/S01E01.mkv");

        var primary = await _manager.MergeVersionsAsync([videoA, videoB], autoGrouped, CancellationToken.None);

        Assert.NotNull(primary);
        var alternate = primary == videoA ? videoB : videoA;

        Assert.Equal(primary.Id, alternate.PrimaryVersionId);
        Assert.Null(primary.PrimaryVersionId);
        var link = Assert.Single(primary.LinkedAlternateVersions);
        Assert.Equal(alternate.Id, link.ItemId);
        Assert.Equal(expectedType, link.Type);
    }

    [Fact]
    public async Task MergeVersionsAsync_FewerThanTwoVideos_DoesNothing()
    {
        var video = CreateVideo("/Shows/Demo/S01E01.mkv");

        var primary = await _manager.MergeVersionsAsync([video], true, CancellationToken.None);

        Assert.Null(primary);
        Assert.Null(video.PrimaryVersionId);
        Assert.Empty(video.LinkedAlternateVersions);
    }

    [Fact]
    public async Task MergeVersionsAsync_LocalAlternateOfPrimary_IsNotLinked()
    {
        var primary = CreateVideo("/Movies/Movie/Movie.mkv");
        var localAlternate = CreateVideo("/Movies/Movie/Movie - 1080p.mkv");
        _libraryManager.Setup(x => x.GetLocalAlternateVersionIds(primary)).Returns(new[] { localAlternate.Id });

        var result = await _manager.MergeVersionsAsync([primary, localAlternate], false, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result.LinkedAlternateVersions);
        Assert.Null(localAlternate.PrimaryVersionId);
    }

    [Fact]
    public async Task MergeVersionsAsync_TransfersExistingLinksToNewPrimary()
    {
        var oldAlternateId = Guid.NewGuid();
        var videoA = CreateVideo("/Shows/Demo S01 (BW)/S01E01.mkv");
        var videoB = CreateVideo("/Shows/Demo S01 (Color)/S01E01.mkv");
        videoB.LinkedAlternateVersions = [new LinkedChild { ItemId = oldAlternateId, Type = LinkedChildType.LinkedAlternateVersion }];

        var primary = await _manager.MergeVersionsAsync([videoA, videoB], true, CancellationToken.None);

        Assert.NotNull(primary);
        var alternate = primary == videoA ? videoB : videoA;
        Assert.Empty(alternate.LinkedAlternateVersions);
        Assert.Contains(primary.LinkedAlternateVersions, l => l.ItemId.HasValue && l.ItemId.Value.Equals(oldAlternateId));
        Assert.Contains(primary.LinkedAlternateVersions, l => l.ItemId.HasValue && l.ItemId.Value.Equals(alternate.Id));
    }

    [Fact]
    public async Task UnlinkVersionAsync_ClearsPrimaryReferenceAndLink()
    {
        var primary = CreateVideo("/Shows/Demo S01 (BW)/S01E01.mkv");
        var alternate = CreateVideo("/Shows/Demo S01 (Color)/S01E01.mkv");
        alternate.PrimaryVersionId = primary.Id;
        primary.LinkedAlternateVersions = [new LinkedChild { ItemId = alternate.Id, Type = LinkedChildType.AutoLinkedAlternateVersion }];
        _libraryManager.Setup(x => x.GetItemById(primary.Id)).Returns(primary);

        await _manager.UnlinkVersionAsync(alternate, CancellationToken.None);

        Assert.Null(alternate.PrimaryVersionId);
        Assert.Empty(primary.LinkedAlternateVersions);
    }

    [Fact]
    public async Task RemoveVersionLinkAsync_AlternatePointsBack_UnlinksIt()
    {
        var primary = CreateVideo("/Shows/Demo S01 (BW)/S01E01.mkv");
        var alternate = CreateVideo("/Shows/Demo S01 (Color)/S01E01.mkv");
        alternate.PrimaryVersionId = primary.Id;
        primary.LinkedAlternateVersions = [new LinkedChild { ItemId = alternate.Id, Type = LinkedChildType.AutoLinkedAlternateVersion }];
        _libraryManager.Setup(x => x.GetItemById(primary.Id)).Returns(primary);
        _libraryManager.Setup(x => x.GetItemById(alternate.Id)).Returns(alternate);

        await _manager.RemoveVersionLinkAsync(primary, alternate.Id, CancellationToken.None);

        Assert.Null(alternate.PrimaryVersionId);
        Assert.Empty(primary.LinkedAlternateVersions);
    }

    [Fact]
    public async Task RemoveVersionLinkAsync_DanglingLink_DropsTheEntry()
    {
        var primary = CreateVideo("/Shows/Demo S01 (BW)/S01E01.mkv");
        var danglingId = Guid.NewGuid();
        primary.LinkedAlternateVersions = [new LinkedChild { ItemId = danglingId, Type = LinkedChildType.AutoLinkedAlternateVersion }];
        _libraryManager.Setup(x => x.GetItemById(danglingId)).Returns((BaseItem?)null);

        await _manager.RemoveVersionLinkAsync(primary, danglingId, CancellationToken.None);

        Assert.Empty(primary.LinkedAlternateVersions);
    }

    [Fact]
    public async Task ReassignAlternatesAsync_RepointsAlternatesAndReroutesReferences()
    {
        var oldPrimary = CreateVideo("/Movies/Movie/Movie.mkv");
        var newPrimary = CreateVideo("/Movies/Movie/Movie - 4K.mkv");
        var localAlternate = CreateVideo("/Movies/Movie/Movie - 1080p.mkv");
        var linkedAlternate = CreateVideo("/Movies/Movie (Extended)/Movie.mkv");
        newPrimary.PrimaryVersionId = oldPrimary.Id;
        localAlternate.PrimaryVersionId = oldPrimary.Id;
        localAlternate.OwnerId = oldPrimary.Id;
        linkedAlternate.PrimaryVersionId = oldPrimary.Id;

        _libraryManager.Setup(x => x.GetLocalAlternateVersionIds(oldPrimary)).Returns([newPrimary.Id, localAlternate.Id]);
        _libraryManager.Setup(x => x.GetLinkedAlternateVersions(oldPrimary)).Returns([linkedAlternate]);
        _libraryManager.Setup(x => x.GetItemById(newPrimary.Id)).Returns(newPrimary);
        _libraryManager.Setup(x => x.GetItemById(localAlternate.Id)).Returns(localAlternate);
        _libraryManager.Setup(x => x.GetItemById(linkedAlternate.Id)).Returns(linkedAlternate);

        await _manager.ReassignAlternatesAsync(oldPrimary, newPrimary, CancellationToken.None);

        // Local (file-based) alternates become owned by the new primary; linked ones stay independent.
        Assert.Equal(newPrimary.Id, localAlternate.PrimaryVersionId);
        Assert.Equal(newPrimary.Id, localAlternate.OwnerId);
        Assert.Equal(newPrimary.Id, linkedAlternate.PrimaryVersionId);
        Assert.Equal(Guid.Empty, linkedAlternate.OwnerId);

        // The new primary itself is left untouched; disposing of the old primary is up to the caller.
        Assert.Equal(oldPrimary.Id, newPrimary.PrimaryVersionId);

        _libraryManager.Verify(x => x.RerouteLinkedChildReferencesAsync(oldPrimary.Id, newPrimary.Id), Times.Once);
    }

    [Fact]
    public async Task SplitVersionsAsync_FromPrimary_ClearsTheWholeGroup()
    {
        var primary = CreateVideo("/Shows/Demo S01 (BW)/S01E01.mkv");
        var alternate = CreateVideo("/Shows/Demo S01 (Color)/S01E01.mkv");
        alternate.PrimaryVersionId = primary.Id;
        primary.LinkedAlternateVersions = [new LinkedChild { ItemId = alternate.Id, Type = LinkedChildType.LinkedAlternateVersion }];
        _libraryManager.Setup(x => x.GetLinkedAlternateVersions(primary)).Returns([alternate]);

        var result = await _manager.SplitVersionsAsync(primary, CancellationToken.None);

        Assert.True(result);
        Assert.Null(primary.PrimaryVersionId);
        Assert.Empty(primary.LinkedAlternateVersions);
        Assert.Null(alternate.PrimaryVersionId);
        Assert.Empty(alternate.LinkedAlternateVersions);
    }

    [Fact]
    public async Task SplitVersionsAsync_FromAlternate_SplitsThePrimarysGroup()
    {
        var primary = CreateVideo("/Shows/Demo S01 (BW)/S01E01.mkv");
        var alternate = CreateVideo("/Shows/Demo S01 (Color)/S01E01.mkv");
        alternate.PrimaryVersionId = primary.Id;
        primary.LinkedAlternateVersions = [new LinkedChild { ItemId = alternate.Id, Type = LinkedChildType.LinkedAlternateVersion }];
        _libraryManager.Setup(x => x.GetItemById(primary.Id)).Returns(primary);
        _libraryManager.Setup(x => x.GetLinkedAlternateVersions(primary)).Returns([alternate]);

        var result = await _manager.SplitVersionsAsync(alternate, CancellationToken.None);

        Assert.True(result);
        Assert.Null(primary.PrimaryVersionId);
        Assert.Empty(primary.LinkedAlternateVersions);
        Assert.Null(alternate.PrimaryVersionId);
    }

    [Fact]
    public async Task SplitVersionsAsync_PrimaryMissing_ReturnsFalse()
    {
        var alternate = CreateVideo("/Shows/Demo S01 (Color)/S01E01.mkv");
        alternate.PrimaryVersionId = Guid.NewGuid();
        _libraryManager.Setup(x => x.GetItemById(It.IsAny<Guid>())).Returns((BaseItem?)null);

        Assert.False(await _manager.SplitVersionsAsync(alternate, CancellationToken.None));
    }

    private static Video CreateVideo(string path)
    {
        return new Video
        {
            Id = Guid.NewGuid(),
            Path = path,
            VideoType = VideoType.VideoFile
        };
    }
}
