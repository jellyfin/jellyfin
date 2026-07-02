using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Moq;
using Xunit;
using LinkedChildType = MediaBrowser.Controller.Entities.LinkedChildType;

namespace Jellyfin.Controller.Tests.Entities;

public class BaseItemTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("1", "0000000001")]
    [InlineData("t", "t")]
    [InlineData("test", "test")]
    [InlineData("test1", "test0000000001")]
    [InlineData("1test 2", "0000000001test 0000000002")]
    public void BaseItem_ModifySortChunks_Valid(string input, string expected)
        => Assert.Equal(expected, BaseItem.ModifySortChunks(input));

    [Theory]
    [InlineData("/Movies/Ted/Ted.mp4", "/Movies/Ted/Ted - Unrated Edition.mp4", "Ted", "Unrated Edition")]
    [InlineData("/Movies/Deadpool 2 (2018)/Deadpool 2 (2018).mkv", "/Movies/Deadpool 2 (2018)/Deadpool 2 (2018) - Super Duper Cut.mkv", "Deadpool 2 (2018)", "Super Duper Cut")]
    public void GetMediaSourceName_Valid(string primaryPath, string altPath, string name, string altName)
    {
        var video = new Video()
        {
            Path = primaryPath
        };

        var videoAlt = new Video()
        {
            Path = altPath,
        };

        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsAny<string>()))
                .Returns((string x) => MediaProtocol.File);
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(x => x.GetLocalAlternateVersionIds(It.IsAny<Video>()))
                .Returns([Guid.Empty]);
        BaseItem.MediaSourceManager = mediaSourceManager.Object;
        BaseItem.LibraryManager = libraryManager.Object;

        Assert.Equal(name, video.GetMediaSourceName(video));
        Assert.Equal(altName, video.GetMediaSourceName(videoAlt));
    }

    [Theory]
    // Episode versions share a season folder; the common prefix (not the folder name) yields the label.
    // Both files carry a suffix (no bare base name), so the shared "- " must be stripped too.
    [InlineData(
        "Spider-Noir - S01E02 - Wo ist Flint - Greyscale",
        "Spider-Noir - S01E02 - Wo ist Flint - Colorized",
        "Greyscale",
        "Colorized")]
    // One version is the bare base name; the other is suffixed.
    [InlineData(
        "Spider-Noir - S01E02 - Wo ist Flint",
        "Spider-Noir - S01E02 - Wo ist Flint - Greyscale",
        "Spider-Noir - S01E02 - Wo ist Flint",
        "Greyscale")]
    // Suffixes share a leading word ("Grey"); the prefix must retreat to the separator, not split it.
    [InlineData(
        "Demo - S01E01 - Greyscale",
        "Demo - S01E01 - Greyish",
        "Greyscale",
        "Greyish")]
    // Underscore separator.
    [InlineData("Movie (2020)_4K", "Movie (2020)_1080p", "4K", "1080p")]
    // Dot separator.
    [InlineData("Movie (2020).UHD", "Movie (2020).1080p", "UHD", "1080p")]
    // Resolution variants that share leading digits must retreat to the separator, not yield "p"/"i".
    [InlineData("Movie - 1080p", "Movie - 1080i", "1080p", "1080i")]
    // A token shared by the descriptors but separated only by spaces (the resolution) must stay in the
    // label: retreat to the '-' delimiter, not the interior space, so the resolution is kept.
    [InlineData(
        "movie (2020) - 2160p Extended",
        "movie (2020) - 2160p Original",
        "2160p Extended",
        "2160p Original")]
    // Bracketed version labels: the opening bracket is kept in the label.
    [InlineData(
        "Blade Runner (1982) [Final Cut] [1080p HEVC AAC]",
        "Blade Runner (1982) [EE by ADM] [480p HEVC AAC]",
        "[Final Cut] [1080p HEVC AAC]",
        "[EE by ADM] [480p HEVC AAC]")]
    public void GetMediaSourceName_CommonPrefix_Valid(string primaryName, string altName, string expectedPrimary, string expectedAlt)
    {
        var primaryPath = "/Shows/Demo/Season 01/" + primaryName + ".mkv";
        var altPath = "/Shows/Demo/Season 01/" + altName + ".mkv";
        var commonPrefix = BaseItem.GetCommonVersionPrefix([primaryName, altName]);

        var video = new Video()
        {
            Path = primaryPath
        };

        var videoAlt = new Video()
        {
            Path = altPath,
        };

        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsAny<string>()))
                .Returns((string x) => MediaProtocol.File);
        var libraryManager = new Mock<ILibraryManager>();
        // No local alternate versions: these are linked (separate items), so the folder fallback is unavailable.
        libraryManager.Setup(x => x.GetLocalAlternateVersionIds(It.IsAny<Video>()))
                .Returns(Array.Empty<Guid>());
        BaseItem.MediaSourceManager = mediaSourceManager.Object;
        BaseItem.LibraryManager = libraryManager.Object;

        Assert.Equal(expectedPrimary, video.GetMediaSourceName(video, commonPrefix));
        Assert.Equal(expectedAlt, videoAlt.GetMediaSourceName(videoAlt, commonPrefix));
    }

    [Theory]
    // Identical file names in version-suffixed folders: the folder suffix is the only difference,
    // and a fully parenthesized suffix is unwrapped.
    [InlineData(
        "/Shows/Spider Noir S01 (BW)/S01E01.mkv",
        "/Shows/Spider Noir S01 (Color)/S01E01.mkv",
        "BW",
        "Color")]
    // Dash-separated folder suffixes.
    [InlineData(
        "/Shows/Demo/Season 01 - Greyscale/Demo S01E01.mkv",
        "/Shows/Demo/Season 01 - Colorized/Demo S01E01.mkv",
        "Greyscale",
        "Colorized")]
    public void GetMediaSourceName_CrossFolderVersions_LabelsByFolderSuffix(string primaryPath, string altPath, string expectedPrimary, string expectedAlt)
    {
        var video = new Video()
        {
            Path = primaryPath
        };

        var videoAlt = new Video()
        {
            Path = altPath,
        };

        var fileNames = new[] { System.IO.Path.GetFileNameWithoutExtension(primaryPath), System.IO.Path.GetFileNameWithoutExtension(altPath) };
        var folderNames = new[] { System.IO.Path.GetFileName(video.ContainingFolderPath), System.IO.Path.GetFileName(videoAlt.ContainingFolderPath) };
        var commonPrefix = BaseItem.GetCommonVersionPrefix(fileNames);
        var commonFolderPrefix = BaseItem.GetCommonVersionPrefix(folderNames);

        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsAny<string>()))
                .Returns((string x) => MediaProtocol.File);
        var libraryManager = new Mock<ILibraryManager>();
        // No local alternate versions: cross-folder versions are linked (separate items).
        libraryManager.Setup(x => x.GetLocalAlternateVersionIds(It.IsAny<Video>()))
                .Returns(Array.Empty<Guid>());
        BaseItem.MediaSourceManager = mediaSourceManager.Object;
        BaseItem.LibraryManager = libraryManager.Object;

        Assert.Equal(expectedPrimary, video.GetMediaSourceName(video, commonPrefix, commonFolderPrefix));
        Assert.Equal(expectedAlt, videoAlt.GetMediaSourceName(videoAlt, commonPrefix, commonFolderPrefix));
    }

    [Fact]
    public void GetMediaSources_CrossFolderVersions_HaveDistinctNames()
    {
        var (primary, alt) = SetupLinkedVersionPair(LinkedChildType.AutoLinkedAlternateVersion);

        var sources = primary.GetMediaSources(false);

        Assert.Equal(2, sources.Count);
        Assert.Equal("BW", sources.First(s => s.Id == primary.Id.ToString("N")).Name);
        Assert.Equal("Color", sources.First(s => s.Id == alt.Id.ToString("N")).Name);
    }

    [Theory]
    // Scan-managed (auto-linked) versions are not user-splittable, so they surface as plain
    // default sources; user-merged versions keep the splittable grouping marker.
    [InlineData(LinkedChildType.AutoLinkedAlternateVersion, MediaSourceType.Default)]
    [InlineData(LinkedChildType.LinkedAlternateVersion, MediaSourceType.Grouping)]
    public void GetAllItemsForMediaSources_LinkedVersionType_MapsToSourceType(LinkedChildType linkType, MediaSourceType expectedType)
    {
        var (primary, alt) = SetupLinkedVersionPair(linkType);

        var method = typeof(Video).GetMethod("GetAllItemsForMediaSources", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        // From the primary's view the alternate carries the link's source type.
        var fromPrimary = ((IEnumerable<(BaseItem Item, MediaSourceType MediaSourceType)>)method!.Invoke(primary, null)!).ToList();
        Assert.Equal(expectedType, fromPrimary.Single(i => i.Item.Id.Equals(alt.Id)).MediaSourceType);

        // From the alternate's view the primary carries it as well.
        var fromAlt = ((IEnumerable<(BaseItem Item, MediaSourceType MediaSourceType)>)method.Invoke(alt, null)!).ToList();
        Assert.Equal(expectedType, fromAlt.Single(i => i.Item.Id.Equals(primary.Id)).MediaSourceType);
    }

    private static (Video Primary, Video Alt) SetupLinkedVersionPair(LinkedChildType linkType)
    {
        var primary = new Video { Id = Guid.NewGuid(), Path = "/Shows/Spider Noir S01 (BW)/S01E01.mkv" };
        var alt = new Video { Id = Guid.NewGuid(), Path = "/Shows/Spider Noir S01 (Color)/S01E01.mkv", PrimaryVersionId = primary.Id };
        primary.LinkedAlternateVersions = [new LinkedChild { ItemId = alt.Id, Type = linkType }];

        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsAny<string>())).Returns(MediaProtocol.File);
        mediaSourceManager.Setup(x => x.GetMediaStreams(It.IsAny<Guid>())).Returns(new List<MediaStream>());
        mediaSourceManager.Setup(x => x.GetMediaAttachments(It.IsAny<Guid>())).Returns(new List<MediaAttachment>());

        var segmentManager = new Mock<IMediaSegmentManager>();
        segmentManager.Setup(x => x.IsTypeSupported(It.IsAny<BaseItem>())).Returns(false);
        BaseItem.MediaSegmentManager = segmentManager.Object;

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(x => x.GetLinkedAlternateVersions(primary)).Returns(new[] { alt });
        libraryManager.Setup(x => x.GetLinkedAlternateVersions(alt)).Returns(Array.Empty<Video>());
        libraryManager.Setup(x => x.GetLocalAlternateVersionIds(It.IsAny<Video>())).Returns(Array.Empty<Guid>());
        libraryManager.Setup(x => x.GetItemById(primary.Id)).Returns(primary);
        libraryManager.Setup(x => x.GetItemById(alt.Id)).Returns(alt);

        var recordingsManager = new Mock<IRecordingsManager>();
        recordingsManager.Setup(x => x.GetActiveRecordingInfo(It.IsAny<string>())).Returns((ActiveRecordingInfo?)null);
        Video.RecordingsManager = recordingsManager.Object;

        BaseItem.MediaSourceManager = mediaSourceManager.Object;
        BaseItem.LibraryManager = libraryManager.Object;

        return (primary, alt);
    }

    [Fact]
    public void GetAlternateVersion_ReturnsMatchingLocalVersion()
    {
        var (primary, alt1, alt2) = SetupVersionGroup();

        Assert.Same(alt1, primary.GetAlternateVersion(alt1.Id));
        Assert.Same(alt2, primary.GetAlternateVersion(alt2.Id));
        Assert.Same(primary, primary.GetAlternateVersion(primary.Id));
        Assert.Null(primary.GetAlternateVersion(Guid.NewGuid()));
    }

    [Fact]
    public void GetAllVersions_FromAnyVersion_ReturnsEveryVersionOnce()
    {
        var (primary, alt1, alt2) = SetupVersionGroup();

        foreach (var source in new[] { primary, alt1, alt2 })
        {
            var versions = source.GetAllVersions();

            Assert.Equal(3, versions.Count);
            Assert.Contains(versions, v => v.Id.Equals(primary.Id));
            Assert.Contains(versions, v => v.Id.Equals(alt1.Id));
            Assert.Contains(versions, v => v.Id.Equals(alt2.Id));
        }
    }

    [Fact]
    public void PropagatePlayedState_MarksAlternateVersions_AndResetsPositionByDefault()
    {
        var (primary, alt1, alt2) = SetupVersionGroup();

        var saved = CaptureSaves();

        var user = new User("test", "default", "default");
        primary.PropagatePlayedState(user, true);

        // Both alternate versions are marked played, the primary (self) is not, and the position is
        // reset so a watched version does not linger in "Continue Watching".
        Assert.Equal(2, saved.Count);
        Assert.DoesNotContain(saved, e => e.ItemId.Equals(primary.Id));
        Assert.Contains(saved, e => e.ItemId.Equals(alt1.Id));
        Assert.Contains(saved, e => e.ItemId.Equals(alt2.Id));
        Assert.All(saved, e =>
        {
            Assert.True(e.Dto.Played.GetValueOrDefault());
            Assert.Equal(0, e.Dto.PlaybackPositionTicks);
        });
    }

    [Fact]
    public void PropagatePlayedState_WithoutReset_LeavesPositionUntouched()
    {
        var (primary, _, _) = SetupVersionGroup();

        var saved = CaptureSaves();

        primary.PropagatePlayedState(new User("test", "default", "default"), true, resetPosition: false);

        Assert.Equal(2, saved.Count);
        Assert.All(saved, e =>
        {
            Assert.True(e.Dto.Played.GetValueOrDefault());
            Assert.Null(e.Dto.PlaybackPositionTicks);
        });
    }

    [Fact]
    public void PropagatePlayedState_Unwatched_ClearsAllWatchedStateOnVersions()
    {
        var (primary, alt1, alt2) = SetupVersionGroup();

        // Each alternate starts out watched, with a play count, resume point and last-played date.
        var existing = new Dictionary<Guid, UserItemData>
        {
            [alt1.Id] = new UserItemData { Key = "alt1", Played = true, PlayCount = 3, PlaybackPositionTicks = 1000, LastPlayedDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            [alt2.Id] = new UserItemData { Key = "alt2", Played = true, PlayCount = 1, PlaybackPositionTicks = 500, LastPlayedDate = new DateTime(2021, 2, 2, 0, 0, 0, DateTimeKind.Utc) },
        };

        var saved = new List<UserItemData>();
        var userDataManager = new Mock<IUserDataManager>();
        userDataManager.Setup(x => x.GetUserData(It.IsAny<User>(), It.IsAny<BaseItem>()))
            .Returns((User _, BaseItem item) => existing.GetValueOrDefault(item.Id));
        userDataManager
            .Setup(x => x.SaveUserData(It.IsAny<User>(), It.IsAny<BaseItem>(), It.IsAny<UserItemData>(), It.IsAny<UserDataSaveReason>(), It.IsAny<CancellationToken>()))
            .Callback<User, BaseItem, UserItemData, UserDataSaveReason, CancellationToken>((_, _, data, _, _) => saved.Add(data));
        BaseItem.UserDataManager = userDataManager.Object;

        primary.PropagatePlayedState(new User("test", "default", "default"), false);

        // Every alternate is fully reset to an unwatched state, mirroring MarkUnplayed: the played flag,
        // play count, resume point and last-played date are all cleared so no watched state lingers.
        Assert.Equal(2, saved.Count);
        Assert.All(saved, d =>
        {
            Assert.False(d.Played);
            Assert.Equal(0, d.PlayCount);
            Assert.Equal(0, d.PlaybackPositionTicks);
            Assert.Null(d.LastPlayedDate);
        });
    }

    private static List<(Guid ItemId, UpdateUserItemDataDto Dto)> CaptureSaves()
    {
        var saved = new List<(Guid ItemId, UpdateUserItemDataDto Dto)>();
        var userDataManager = new Mock<IUserDataManager>();
        userDataManager
            .Setup(x => x.SaveUserData(It.IsAny<User>(), It.IsAny<BaseItem>(), It.IsAny<UpdateUserItemDataDto>(), It.IsAny<UserDataSaveReason>()))
            .Callback<User, BaseItem, UpdateUserItemDataDto, UserDataSaveReason>((_, item, dto, _) => saved.Add((item.Id, dto)));
        BaseItem.UserDataManager = userDataManager.Object;
        return saved;
    }

    [Fact]
    public void PropagatePlayedState_SingleVersion_DoesNothing()
    {
        var solo = new Video { Id = Guid.NewGuid(), Path = "/Movies/Solo/Solo.mkv" };

        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsAny<string>())).Returns(MediaProtocol.File);
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(x => x.GetLocalAlternateVersionIds(It.IsAny<Video>())).Returns(Array.Empty<Guid>());
        libraryManager.Setup(x => x.GetLinkedAlternateVersions(It.IsAny<Video>())).Returns(Array.Empty<Video>());
        BaseItem.MediaSourceManager = mediaSourceManager.Object;
        BaseItem.LibraryManager = libraryManager.Object;

        var userDataManager = new Mock<IUserDataManager>();
        BaseItem.UserDataManager = userDataManager.Object;

        solo.PropagatePlayedState(new User("test", "default", "default"), true);

        userDataManager.Verify(
            x => x.SaveUserData(It.IsAny<User>(), It.IsAny<BaseItem>(), It.IsAny<UpdateUserItemDataDto>(), It.IsAny<UserDataSaveReason>()),
            Times.Never);
    }

    private static (Video Primary, Video Alt1, Video Alt2) SetupVersionGroup()
    {
        var primary = new Video { Id = Guid.NewGuid(), Path = "/Movies/Movie/Movie.mkv" };
        var alt1 = new Video { Id = Guid.NewGuid(), Path = "/Movies/Movie/Movie - 1080p.mkv", PrimaryVersionId = primary.Id };
        var alt2 = new Video { Id = Guid.NewGuid(), Path = "/Movies/Movie/Movie - 4K.mkv", PrimaryVersionId = primary.Id };

        // 2160p primary, 1080p alternates: width is only the ordering tiebreaker, set so it would place
        // the primary first — letting the tests confirm the queried version's own source still wins.
        var widths = new Dictionary<Guid, int> { [primary.Id] = 3840, [alt1.Id] = 1920, [alt2.Id] = 1920 };
        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsAny<string>())).Returns(MediaProtocol.File);
        mediaSourceManager.Setup(x => x.GetMediaStreams(It.IsAny<Guid>()))
            .Returns((Guid id) => new List<MediaStream> { new MediaStream { Type = MediaStreamType.Video, Width = widths.GetValueOrDefault(id) } });
        mediaSourceManager.Setup(x => x.GetMediaAttachments(It.IsAny<Guid>())).Returns(new List<MediaAttachment>());

        var segmentManager = new Mock<IMediaSegmentManager>();
        segmentManager.Setup(x => x.IsTypeSupported(It.IsAny<BaseItem>())).Returns(false);
        BaseItem.MediaSegmentManager = segmentManager.Object;

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(x => x.GetLinkedAlternateVersions(It.IsAny<Video>())).Returns(Array.Empty<Video>());
        libraryManager.Setup(x => x.GetLocalAlternateVersionIds(primary)).Returns(new[] { alt1.Id, alt2.Id });
        libraryManager.Setup(x => x.GetLocalAlternateVersionIds(alt1)).Returns(Array.Empty<Guid>());
        libraryManager.Setup(x => x.GetLocalAlternateVersionIds(alt2)).Returns(Array.Empty<Guid>());
        libraryManager.Setup(x => x.GetItemById(alt1.Id)).Returns(alt1);
        libraryManager.Setup(x => x.GetItemById(alt2.Id)).Returns(alt2);
        libraryManager.Setup(x => x.GetItemById(primary.Id)).Returns(primary);

        var recordingsManager = new Mock<IRecordingsManager>();
        recordingsManager.Setup(x => x.GetActiveRecordingInfo(It.IsAny<string>())).Returns((ActiveRecordingInfo?)null);
        Video.RecordingsManager = recordingsManager.Object;

        BaseItem.MediaSourceManager = mediaSourceManager.Object;
        BaseItem.LibraryManager = libraryManager.Object;

        return (primary, alt1, alt2);
    }

    [Fact]
    public void GetMediaSources_DefaultsToTheQueriedVersionsOwnSource()
    {
        var (primary, alt1, _) = SetupVersionGroup();

        // Resuming the 1080p alternate must default to the 1080p source, not the higher-resolution
        // 2160p primary that the width ordering would otherwise place first.
        Assert.Equal(alt1.Id.ToString("N"), alt1.GetMediaSources(false)[0].Id);

        // Opening the primary still defaults to the primary's own (here highest-resolution) source.
        Assert.Equal(primary.Id.ToString("N"), primary.GetMediaSources(false)[0].Id);
    }

    [Fact]
    public void GetAllItemsForMediaSources_FromAnyVersion_HasNoDuplicates()
    {
        var (primary, alt1, alt2) = SetupVersionGroup();

        var method = typeof(Video).GetMethod("GetAllItemsForMediaSources", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        // Each version must surface exactly once, regardless of which member the list is built from.
        // Building from an alternate previously re-added that alternate as a "local alternate" of the
        // primary, producing a duplicate entry in the version dropdown.
        foreach (var source in new[] { primary, alt1, alt2 })
        {
            var items = (IEnumerable<(BaseItem Item, MediaSourceType MediaSourceType)>)method!.Invoke(source, null)!;
            var ids = items.Select(i => i.Item.Id).ToList();

            Assert.Equal(3, ids.Count);
            Assert.Equal(ids.Count, ids.Distinct().Count());
            Assert.Contains(primary.Id, ids);
            Assert.Contains(alt1.Id, ids);
            Assert.Contains(alt2.Id, ids);
        }
    }
}
