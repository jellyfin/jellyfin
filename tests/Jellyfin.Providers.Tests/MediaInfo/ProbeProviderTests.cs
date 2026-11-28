using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.Common;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Providers.MediaInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

public class ProbeProviderTests
{
    private readonly ProbeProvider _probeProvider;

    public ProbeProviderTests()
    {
        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(m => m.GetPathProtocol(It.IsAny<string>()))
            .Returns(MediaProtocol.File);

        // prep BaseItem and Video for calls made that expect managers
        BaseItem.MediaSourceManager = mediaSourceManager.Object;
        Video.RecordingsManager = Mock.Of<IRecordingsManager>();

        _probeProvider = new ProbeProvider(
            mediaSourceManager.Object,
            Mock.Of<IMediaEncoder>(),
            Mock.Of<IBlurayExaminer>(),
            Mock.Of<ILocalizationManager>(),
            Mock.Of<IChapterManager>(),
            Mock.Of<IServerConfigurationManager>(),
            Mock.Of<ISubtitleManager>(),
            Mock.Of<ILibraryManager>(),
            Mock.Of<IFileSystem>(),
            NullLoggerFactory.Instance,
            new NamingOptions(),
            Mock.Of<ILyricManager>(),
            Mock.Of<IMediaAttachmentRepository>(),
            Mock.Of<IMediaStreamRepository>());
    }

    [Fact]
    public void HasChanged_NeverProbedVideo_ReturnsTrue()
    {
        // A probe that threw leaves the item like this while the refresh is stamped as done, and the
        // file's modification time never changes afterwards, so nothing else would ask for a retry.
        var item = new Episode { Path = "/media/show/S01E01.mkv" };

        Assert.True(_probeProvider.HasChanged(item, Mock.Of<IDirectoryService>()));
    }

    [Fact]
    public void HasChanged_NeverProbedAudio_ReturnsTrue()
    {
        var item = new Audio { Path = "/media/music/track.flac" };

        Assert.True(_probeProvider.HasChanged(item, Mock.Of<IDirectoryService>()));
    }

    [Theory]
    [InlineData(12345L, null)]
    [InlineData(null, 2480000)]
    [InlineData(12345L, 2480000)]
    public void HasChanged_ProbedVideo_ReturnsFalse(long? runTimeTicks, int? totalBitrate)
    {
        var item = new Episode
        {
            Path = "/media/show/S01E01.mkv",
            RunTimeTicks = runTimeTicks,
            TotalBitrate = totalBitrate
        };

        Assert.False(_probeProvider.HasChanged(item, Mock.Of<IDirectoryService>()));
    }

    [Fact]
    public void HasChanged_VirtualItemWithoutMediaInfo_ReturnsFalse()
    {
        var item = new Episode { Path = "/media/show/S01E01.mkv", IsVirtualItem = true };

        Assert.False(_probeProvider.HasChanged(item, Mock.Of<IDirectoryService>()));
    }

    [Fact]
    public void HasChanged_PlaceHolderWithoutMediaInfo_ReturnsFalse()
    {
        var item = new Episode { Path = "/media/show/S01E01.disc", IsPlaceHolder = true };

        Assert.False(_probeProvider.HasChanged(item, Mock.Of<IDirectoryService>()));
    }

    [Fact]
    public void HasChanged_ShortcutWithoutMediaInfo_ReturnsFalse()
    {
        // A .strm is only probed when remote content probing is enabled, so an empty one is expected.
        var item = new Episode { Path = "/media/show/S01E01.strm", IsShortcut = true };

        Assert.False(_probeProvider.HasChanged(item, Mock.Of<IDirectoryService>()));
    }

    [Fact]
    public async Task FetchAsync_OnlyExternalSubtitleAdded_KeepsProbedStreams()
    {
        var context = new ExternalFilesContext();
        // The image subtitle at index 2 was dropped by the embedded subtitle setting, which leaves a gap.
        var movie = context.CreateMovie(
            new MediaStream { Index = 0, Type = MediaStreamType.Video, Codec = "h264", Width = 1920, Height = 1080 },
            new MediaStream { Index = 1, Type = MediaStreamType.Audio, Codec = "aac" },
            new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "subrip" });

        Assert.True(context.Provider.HasChanged(movie, context.DirectoryService));
        await context.Provider.FetchAsync(movie, context.CreateOptions(), CancellationToken.None);

        context.VerifyMediaFileProbed(Times.Never());
        Assert.Equal(new[] { ExternalFilesContext.SubtitlePath }, movie.SubtitleFiles);
        Assert.True(movie.HasSubtitles);
        Assert.Equal(1920, movie.Width);
        Assert.Equal(1, movie.DefaultVideoStreamIndex);
        Assert.Collection(
            context.SavedStreams,
            s => Assert.True(s is { Index: 0, IsExternal: true, Path: ExternalFilesContext.SubtitlePath }),
            s => Assert.True(s is { Index: 1, Type: MediaStreamType.Video, IsExternal: false }),
            s => Assert.True(s is { Index: 2, Type: MediaStreamType.Audio, IsExternal: false }),
            s => Assert.True(s is { Index: 4, Type: MediaStreamType.Subtitle, IsExternal: false }));
    }

    [Fact]
    public async Task FetchAsync_OnlyExternalSubtitleRemoved_KeepsProbedStreams()
    {
        var context = new ExternalFilesContext(withSubtitle: false);
        var movie = context.CreateMovie(
            new MediaStream { Index = 0, Type = MediaStreamType.Subtitle, Codec = "subrip", IsExternal = true, Path = ExternalFilesContext.SubtitlePath },
            new MediaStream { Index = 1, Type = MediaStreamType.Video, Codec = "h264" },
            new MediaStream { Index = 2, Type = MediaStreamType.Audio, Codec = "aac" });
        movie.SubtitleFiles = [ExternalFilesContext.SubtitlePath];

        Assert.True(context.Provider.HasChanged(movie, context.DirectoryService));
        await context.Provider.FetchAsync(movie, context.CreateOptions(), CancellationToken.None);

        context.VerifyMediaFileProbed(Times.Never());
        Assert.Empty(movie.SubtitleFiles);
        Assert.False(movie.HasSubtitles);
        Assert.Collection(
            context.SavedStreams,
            s => Assert.True(s is { Index: 0, Type: MediaStreamType.Video }),
            s => Assert.True(s is { Index: 1, Type: MediaStreamType.Audio }));
    }

    [Theory]
    [InlineData(MetadataRefreshMode.FullRefresh, false)]
    [InlineData(MetadataRefreshMode.Default, true)]
    public async Task FetchAsync_ExternalFilesChangedOnFullRefresh_ProbesMediaFile(MetadataRefreshMode mode, bool replaceAllMetadata)
    {
        var context = new ExternalFilesContext();
        var movie = context.CreateMovie(new MediaStream { Index = 0, Type = MediaStreamType.Video, Codec = "h264" });

        Assert.True(context.Provider.HasChanged(movie, context.DirectoryService));
        var options = context.CreateOptions();
        options.MetadataRefreshMode = mode;
        options.ReplaceAllMetadata = replaceAllMetadata;
        await context.Provider.FetchAsync(movie, options, CancellationToken.None);

        context.VerifyMediaFileProbed(Times.Once());
    }

    [Fact]
    public async Task FetchAsync_MediaFileChangedSinceHasChanged_ProbesMediaFile()
    {
        var context = new ExternalFilesContext();
        var movie = context.CreateMovie(new MediaStream { Index = 0, Type = MediaStreamType.Video, Codec = "h264" });

        Assert.True(context.Provider.HasChanged(movie, context.DirectoryService));
        movie.DateModified = movie.DateModified.AddMinutes(1);
        await context.Provider.FetchAsync(movie, context.CreateOptions(), CancellationToken.None);

        context.VerifyMediaFileProbed(Times.Once());
    }

    [Fact]
    public async Task FetchAsync_ChangeSeenByAnotherRefresh_ProbesMediaFile()
    {
        var context = new ExternalFilesContext();
        var movie = context.CreateMovie(new MediaStream { Index = 0, Type = MediaStreamType.Video, Codec = "h264" });

        Assert.True(context.Provider.HasChanged(movie, context.DirectoryService));
        await context.Provider.FetchAsync(movie, new MetadataRefreshOptions(context.CreateDirectoryService()), CancellationToken.None);

        context.VerifyMediaFileProbed(Times.Once());
    }

    [Fact]
    public async Task FetchAsync_ExternalFilesChangedWithoutStoredStreams_ProbesMediaFile()
    {
        var context = new ExternalFilesContext();
        var movie = context.CreateMovie();

        Assert.True(context.Provider.HasChanged(movie, context.DirectoryService));
        await context.Provider.FetchAsync(movie, context.CreateOptions(), CancellationToken.None);

        context.VerifyMediaFileProbed(Times.Once());
    }

    [Fact]
    public async Task FetchAsync_ExternalSubtitleRewritten_ReprobesOnlyExternalFile()
    {
        var context = new ExternalFilesContext(subtitleWriteTime: ExternalFilesContext.LastRefreshed.AddMinutes(1));
        var movie = context.CreateMovie(
            new MediaStream { Index = 0, Type = MediaStreamType.Subtitle, Codec = "ass", IsExternal = true, Path = ExternalFilesContext.SubtitlePath },
            new MediaStream { Index = 1, Type = MediaStreamType.Video, Codec = "h264" });
        movie.SubtitleFiles = [ExternalFilesContext.SubtitlePath];

        Assert.True(context.Provider.HasChanged(movie, context.DirectoryService));
        await context.Provider.FetchAsync(movie, context.CreateOptions(), CancellationToken.None);

        context.VerifyMediaFileProbed(Times.Never());
        context.VerifyExternalFileProbed(Times.Once());
        Assert.Collection(
            context.SavedStreams,
            s => Assert.True(s is { Index: 0, IsExternal: true, Codec: "subrip" }),
            s => Assert.True(s is { Index: 1, Type: MediaStreamType.Video }));
    }

    [Fact]
    public void HasChanged_ExternalSubtitleWrittenBeforeLastRefresh_ReturnsFalse()
    {
        var context = new ExternalFilesContext(subtitleWriteTime: ExternalFilesContext.LastRefreshed.AddMinutes(-1));
        var movie = context.CreateMovie(new MediaStream { Index = 0, Type = MediaStreamType.Video, Codec = "h264" });
        movie.SubtitleFiles = [ExternalFilesContext.SubtitlePath];

        Assert.False(context.Provider.HasChanged(movie, context.DirectoryService));
    }

    private sealed class ExternalFilesContext
    {
        public const string MoviePath = "/media/Movie/Movie.mkv";
        public const string SubtitlePath = "/media/Movie/Movie.srt";

        public static readonly DateTime LastRefreshed = new(2026, 2, 3, 4, 5, 6, DateTimeKind.Utc);

        // What Video.ContainingFolderPath gives for MoviePath on the platform running the tests.
        private static readonly string _folderPath = System.IO.Path.GetDirectoryName(MoviePath)!;

        private readonly Mock<IMediaEncoder> _mediaEncoder = new();
        private readonly bool _withSubtitle;
        private readonly DateTime? _subtitleWriteTime;

        public ExternalFilesContext(bool withSubtitle = true, DateTime? subtitleWriteTime = null)
        {
            _withSubtitle = withSubtitle;
            _subtitleWriteTime = subtitleWriteTime;

            _mediaEncoder.Setup(m => m.GetMediaInfo(It.Is<MediaInfoRequest>(r => r.MediaType == DlnaProfileType.Subtitle), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new MediaBrowser.Model.MediaInfo.MediaInfo { MediaStreams = [new MediaStream { Type = MediaStreamType.Subtitle, Codec = "subrip" }] });
            _mediaEncoder.Setup(m => m.GetMediaInfo(It.Is<MediaInfoRequest>(r => r.MediaType == DlnaProfileType.Video), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new MediaBrowser.Model.MediaInfo.MediaInfo { MediaStreams = [new MediaStream { Type = MediaStreamType.Video, Codec = "h264" }] });

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(f => f.DirectoryExists(_folderPath)).Returns(true);

            var config = new Mock<IServerConfigurationManager>();
            config.Setup(c => c.Configuration).Returns(new ServerConfiguration { DummyChapterDuration = 0 });

            var libraryManager = new Mock<ILibraryManager>();
            libraryManager.Setup(l => l.GetLibraryOptions(It.IsAny<BaseItem>())).Returns(new LibraryOptions());

            var mediaStreamRepository = new Mock<IMediaStreamRepository>();
            mediaStreamRepository.Setup(r => r.SaveMediaStreams(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<MediaStream>>(), It.IsAny<CancellationToken>()))
                .Callback<Guid, IReadOnlyList<MediaStream>, CancellationToken>((_, streams, _) => SavedStreams = streams.ToList());

            DirectoryService = CreateDirectoryService();
            Provider = new ProbeProvider(
                Mock.Of<IMediaSourceManager>(),
                _mediaEncoder.Object,
                Mock.Of<IBlurayExaminer>(),
                Mock.Of<ILocalizationManager>(),
                Mock.Of<IChapterManager>(),
                config.Object,
                Mock.Of<ISubtitleManager>(),
                libraryManager.Object,
                fileSystem.Object,
                NullLoggerFactory.Instance,
                new NamingOptions(),
                Mock.Of<ILyricManager>(),
                Mock.Of<IMediaAttachmentRepository>(),
                mediaStreamRepository.Object);
        }

        public ProbeProvider Provider { get; }

        public IDirectoryService DirectoryService { get; }

        public List<MediaStream> SavedStreams { get; private set; } = [];

        public IDirectoryService CreateDirectoryService()
        {
            var directoryService = new Mock<IDirectoryService>();
            directoryService.Setup(d => d.GetFilePaths(_folderPath, It.IsAny<bool>()))
                .Returns(_withSubtitle ? [MoviePath, SubtitlePath] : [MoviePath]);
            if (_subtitleWriteTime is not null)
            {
                directoryService.Setup(d => d.GetFile(SubtitlePath))
                    .Returns(new FileSystemMetadata { FullName = SubtitlePath, Exists = true, LastWriteTimeUtc = _subtitleWriteTime.Value });
            }

            return directoryService.Object;
        }

        public MetadataRefreshOptions CreateOptions() => new(DirectoryService);

        public TestMovie CreateMovie(params MediaStream[] storedStreams) => new()
        {
            Id = Guid.NewGuid(),
            Path = MoviePath,
            RunTimeTicks = TimeSpan.TicksPerHour,
            DateModified = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            DateLastRefreshed = LastRefreshed,
            StoredStreams = storedStreams
        };

        public void VerifyMediaFileProbed(Times times)
            => _mediaEncoder.Verify(
                m => m.GetMediaInfo(It.Is<MediaInfoRequest>(r => r.MediaType == DlnaProfileType.Video), It.IsAny<CancellationToken>()),
                times);

        public void VerifyExternalFileProbed(Times times)
            => _mediaEncoder.Verify(
                m => m.GetMediaInfo(It.Is<MediaInfoRequest>(r => r.MediaType == DlnaProfileType.Subtitle), It.IsAny<CancellationToken>()),
                times);
    }

    private sealed class TestMovie : Movie
    {
        public IReadOnlyList<MediaStream> StoredStreams { get; init; } = [];

        public override IReadOnlyList<MediaStream> GetMediaStreams() => StoredStreams;

        public override string GetInternalMetadataPath() => "/metadata/Movie";
    }
}
