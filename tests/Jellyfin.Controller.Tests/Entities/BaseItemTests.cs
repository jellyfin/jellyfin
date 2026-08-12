using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Querying;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

public class BaseItemTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData(200L, false)]
    [InlineData(100L, true)]
    public void RequiresRefresh_FileSizeWithSameModificationDate_ReturnsExpected(long? itemSize, bool expected)
    {
        const string TestPath = "/media/movie.mkv";
        var lastWriteTime = new DateTime(2026, 8, 7, 0, 0, 0, DateTimeKind.Utc);
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(x => x.GetFileSystemInfo(TestPath))
            .Returns(new FileSystemMetadata
            {
                Exists = true,
                IsDirectory = false,
                LastWriteTimeUtc = lastWriteTime,
                Length = 200
            });
        BaseItem.FileSystem = fileSystem.Object;

        var item = new Video
        {
            Path = TestPath,
            DateModified = lastWriteTime,
            Size = itemSize
        };

        Assert.Equal(expected, item.RequiresRefresh());
    }

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
    [InlineData("The Matrix", "matrix")]
    [InlineData("Spider-Man", "spiderman")]
    [InlineData("A Movie: Part 2", "movie: part 0000000002")]
    public void GetSortName_AppliesConfiguredCleaning(string input, string expected)
        => Assert.Equal(expected, BaseItem.GetSortName(input, true, new ServerConfiguration()));

    [Fact]
    public void GetSortName_WithoutAlphaNumericSorting_ReturnsTrimmedInput()
        => Assert.Equal("The Matrix", BaseItem.GetSortName("  The Matrix", false, new ServerConfiguration()));

    [Fact]
    public void SortName_ForcedSortName_IsCleanedLikeAutoSortName()
    {
        var configManager = new Mock<IServerConfigurationManager>();
        configManager.Setup(x => x.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configManager.Object;

        const string Raw = "The Spider-Man: Homecoming";

        var auto = new Video { Name = Raw };
        var forced = new Video { Name = "zzz unrelated name", ForcedSortName = Raw };

        // A forced sort name must be cleaned the same way as an auto-generated one so both sort together (#17388).
        Assert.Equal(auto.SortName, forced.SortName);
        // Sanity: cleaning actually ran (leading article and hyphen removed, colon kept, lowercased).
        Assert.Equal("spiderman: homecoming", forced.SortName);
    }

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

    [Theory]
    // A version file the scan just found beside the episode is not linked yet, so it does not count
    // towards MediaSourceCount. The episode still has to refresh its owned items, as that is what
    // creates the item for the version and links it.
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    public void SupportsOwnedItems_EpisodeWithResolvedVersionOrPart_IsTrue(bool hasLocalVersion, bool isStacked, bool expected)
    {
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(x => x.GetLinkedAlternateVersions(It.IsAny<Video>())).Returns(Array.Empty<Video>());
        libraryManager.Setup(x => x.GetLocalAlternateVersionIds(It.IsAny<Video>())).Returns(Array.Empty<Guid>());
        BaseItem.LibraryManager = libraryManager.Object;

        var episode = new Episode
        {
            Id = Guid.NewGuid(),
            Path = "/TV/Show/Season 1/S01E01 - 1080p.mkv",
            LocalAlternateVersions = hasLocalVersion ? ["/TV/Show/Season 1/S01E01 - 720p.mkv"] : [],
            AdditionalParts = isStacked ? ["/TV/Show/Season 1/S01E01 - 1080p-part2.mkv"] : []
        };

        var property = typeof(Episode).GetProperty("SupportsOwnedItems", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(property);

        Assert.Equal(expected, (bool)property!.GetValue(episode)!);
    }

    [Theory]
    // The season folder is the season's own, so the extras that sit in it are the season's. Whether
    // the season holds one episode or two must not decide where its extras show up.
    [InlineData(false, false)]
    // An episode with a folder of its own keeps the extras in it, as nothing else searches there
    [InlineData(true, true)]
    public async Task RefreshedOwnedItems_EpisodeInAContainersOwnFolder_LeavesExtrasToTheContainer(bool episodeHasOwnFolder, bool expectSearch)
    {
        var seasonPath = Path.Combine("TV", "Show", "Season 1");
        var episodeFolder = episodeHasOwnFolder ? Path.Combine(seasonPath, "S01E01") : seasonPath;
        var episodePath = Path.Combine(episodeFolder, "S01E01 - 1080p.mkv");

        // The season needs a parent of its own, as an item without one maintains no owned items
        var season = new Season { Id = Guid.NewGuid(), ParentId = Guid.NewGuid(), Path = seasonPath };
        var episode = new Episode
        {
            Id = Guid.NewGuid(),
            ParentId = season.Id,
            Path = episodePath,
            // A version file is what makes an episode maintain owned items at all
            LocalAlternateVersions = [Path.Combine(episodeFolder, "S01E01 - 720p.mkv")]
        };

        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsAny<string>())).Returns(MediaProtocol.File);
        BaseItem.MediaSourceManager = mediaSourceManager.Object;

        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
        BaseItem.FileSystem = fileSystem.Object;

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(x => x.GetItemById(season.Id)).Returns(season);
        libraryManager.Setup(x => x.GetLinkedAlternateVersions(It.IsAny<Video>())).Returns(Array.Empty<Video>());
        libraryManager.Setup(x => x.GetLocalAlternateVersionIds(It.IsAny<Video>())).Returns(Array.Empty<Guid>());
        libraryManager.Setup(x => x.GetItemList(It.IsAny<InternalItemsQuery>())).Returns(Array.Empty<BaseItem>());
        libraryManager.Setup(x => x.FindExtras(It.IsAny<BaseItem>(), It.IsAny<IReadOnlyList<FileSystemMetadata>>(), It.IsAny<IDirectoryService>()))
            .Returns(Array.Empty<BaseItem>());
        BaseItem.LibraryManager = libraryManager.Object;

        var method = typeof(BaseItem).GetMethod("RefreshedOwnedItems", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var options = new MetadataRefreshOptions(Mock.Of<IDirectoryService>());
        await (Task<bool>)method!.Invoke(episode, [options, Array.Empty<FileSystemMetadata>(), CancellationToken.None])!;

        libraryManager.Verify(
            x => x.FindExtras(episode, It.IsAny<IReadOnlyList<FileSystemMetadata>>(), It.IsAny<IDirectoryService>()),
            expectSearch ? Times.Once() : Times.Never());
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

    [Fact]
    public void InheritDatesFromOwner_OwnerHasDates_OverwritesOwnedItemDates()
    {
        var owner = new Movie
        {
            ProductionYear = 1982,
            PremiereDate = new DateTime(1982, 6, 25, 0, 0, 0, DateTimeKind.Utc)
        };

        // 2016 is what the container creation date of a re-encoded trailer would have yielded.
        var trailer = new Trailer
        {
            ExtraType = ExtraType.Trailer,
            ProductionYear = 2016,
            PremiereDate = new DateTime(2016, 5, 4, 0, 0, 0, DateTimeKind.Utc)
        };

        Assert.True(BaseItem.InheritDatesFromOwner(owner, trailer));
        Assert.Equal(owner.ProductionYear, trailer.ProductionYear);
        Assert.Equal(owner.PremiereDate, trailer.PremiereDate);
    }

    [Fact]
    public void InheritDatesFromOwner_OwnerHasNoDates_KeepsOwnedItemDates()
    {
        var owner = new Movie();
        var trailer = new Trailer
        {
            ExtraType = ExtraType.Trailer,
            ProductionYear = 1982,
            PremiereDate = new DateTime(1982, 6, 25, 0, 0, 0, DateTimeKind.Utc)
        };

        Assert.False(BaseItem.InheritDatesFromOwner(owner, trailer));
        Assert.Equal(1982, trailer.ProductionYear);
        Assert.Equal(new DateTime(1982, 6, 25, 0, 0, 0, DateTimeKind.Utc), trailer.PremiereDate);
    }

    [Fact]
    public void InheritDatesFromOwner_DatesAlreadyMatch_ReportsNoChange()
    {
        var owner = new Movie
        {
            ProductionYear = 1982,
            PremiereDate = new DateTime(1982, 6, 25, 0, 0, 0, DateTimeKind.Utc)
        };

        var trailer = new Trailer
        {
            ExtraType = ExtraType.Trailer,
            ProductionYear = owner.ProductionYear,
            PremiereDate = owner.PremiereDate
        };

        Assert.False(BaseItem.InheritDatesFromOwner(owner, trailer));
    }

    [Fact]
    public void InheritDatesFromOwner_OwnedItemHasNoDates_TakesOwnerDates()
    {
        var owner = new Movie
        {
            ProductionYear = 1982,
            PremiereDate = new DateTime(1982, 6, 25, 0, 0, 0, DateTimeKind.Utc)
        };

        var trailer = new Trailer
        {
            ExtraType = ExtraType.Trailer
        };

        Assert.True(BaseItem.InheritDatesFromOwner(owner, trailer));
        Assert.Equal(1982, trailer.ProductionYear);
        Assert.Equal(new DateTime(1982, 6, 25, 0, 0, 0, DateTimeKind.Utc), trailer.PremiereDate);
    }

    [Theory]
    // An extra named after a version belongs to that version, not to the primary whose name it
    // also starts with
    [InlineData("/Movies/Movie/Movie - 4K-trailer.mkv", 2)]
    [InlineData("/Movies/Movie/Movie - 1080p-behindthescenes.mkv", 1)]
    // Named after the movie rather than one of its versions
    [InlineData("/Movies/Movie/Movie-trailer.mkv", 0)]
    // In an extras folder, so named after nothing in particular
    [InlineData("/Movies/Movie/trailers/Official.mkv", 0)]
    // A version name is only a match when it is followed by the extra's own suffix
    [InlineData("/Movies/Movie/Movie - 4Kish-trailer.mkv", 0)]
    public void GetOwnerIdForExtra_AssignsExtraToItsVersion(string extraPath, int expectedVersion)
    {
        var (primary, alt1, alt2) = SetupVersionGroup();
        var expectedId = expectedVersion switch
        {
            1 => alt1.Id,
            2 => alt2.Id,
            _ => primary.Id
        };

        var method = typeof(Video).GetMethod("GetOwnerIdForExtra", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var ownerId = (Guid)method!.Invoke(primary, [new Video { Id = Guid.NewGuid(), Path = extraPath }])!;

        Assert.Equal(expectedId, ownerId);
    }

    [Fact]
    public void GetExtraOwnerIds_FromAnyVersion_CoversEveryVersion()
    {
        var (primary, alt1, alt2) = SetupVersionGroup();

        var method = typeof(Video).GetMethod("GetExtraOwnerIds", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        // An extra is owned by the one version it is named after, and the extras of the movie as a
        // whole are owned by the primary, so every version has to read all of them back
        foreach (var version in new[] { primary, alt1, alt2 })
        {
            var ids = (Guid[])method!.Invoke(version, null)!;

            Assert.Equal(3, ids.Length);
            Assert.Contains(primary.Id, ids);
            Assert.Contains(alt1.Id, ids);
            Assert.Contains(alt2.Id, ids);
        }
    }

    [Fact]
    public void GetOwnedVersionIds_CoversEveryLocalVersion()
    {
        var (primary, alt1, alt2) = SetupVersionGroup();

        var method = typeof(Video).GetMethod("GetOwnedVersionIds", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        // The extras of all versions are maintained together, so all of them have to be read back
        var ids = (Guid[])method!.Invoke(primary, null)!;

        Assert.Equal([primary.Id, alt1.Id, alt2.Id], ids);
    }
}
