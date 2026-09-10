using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Naming.Common;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Providers.MediaInfo;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

[Collection("BaseItemServiceLocators")]
public class FFProbeVideoInfoTests
{
    private readonly FFProbeVideoInfo _fFProbeVideoInfo;

    public FFProbeVideoInfoTests()
    {
        var serverConfiguration = new ServerConfiguration()
        {
            DummyChapterDuration = (int)TimeSpan.FromMinutes(5).TotalSeconds
        };
        var serverConfig = new Mock<IServerConfigurationManager>();
        serverConfig.Setup(c => c.Configuration)
            .Returns(serverConfiguration);

        IFixture fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
        fixture.Inject(serverConfig);
        _fFProbeVideoInfo = fixture.Create<FFProbeVideoInfo>();
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void CreateDummyChapters_InvalidRuntime_ThrowsArgumentException(long? runtime)
    {
        Assert.Throws<ArgumentException>(
            () => _fFProbeVideoInfo.CreateDummyChapters(new Video()
            {
                RunTimeTicks = runtime
            }));
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData(0L, 0)]
    [InlineData(1L, 1)]
    [InlineData(TimeSpan.TicksPerMinute * 3, 1)]
    [InlineData(TimeSpan.TicksPerMinute * 5, 1)]
    [InlineData((TimeSpan.TicksPerMinute * 5) + 1, 1)]
    [InlineData(TimeSpan.TicksPerMinute * 50, 10)]
    public void CreateDummyChapters_ValidRuntime_CorrectChaptersCount(long? runtime, int chaptersCount)
    {
        var chapters = _fFProbeVideoInfo.CreateDummyChapters(new Video()
        {
            RunTimeTicks = runtime
        });

        Assert.Equal(chaptersCount, chapters.Length);
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(TimeSpan.TicksPerMinute * 3)]
    [InlineData(TimeSpan.TicksPerMinute * 5)]
    [InlineData((TimeSpan.TicksPerMinute * 5) + 1)]
    [InlineData((TimeSpan.TicksPerMinute * 50) + 1)]
    public void CreateDummyChapters_PositiveRuntime_NoChapterBeyondRuntime(long runtime)
    {
        var chapters = _fFProbeVideoInfo.CreateDummyChapters(new Video()
        {
            RunTimeTicks = runtime
        });

        Assert.All(chapters, chapter => Assert.True(chapter.StartPositionTicks < runtime));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FetchEmbeddedInfo_NoExtra_AppliesContainerDates(bool replaceAllMetadata)
    {
        var video = new Video();

        _fFProbeVideoInfo.FetchEmbeddedInfo(video, CreateMediaInfoWithDates(), CreateRefreshOptions(replaceAllMetadata), new LibraryOptions());

        Assert.Equal(2016, video.ProductionYear);
        Assert.Equal(new DateTime(2016, 5, 4, 0, 0, 0, DateTimeKind.Utc), video.PremiereDate);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FetchEmbeddedInfo_Extra_IgnoresContainerDates(bool replaceAllMetadata)
    {
        var video = new Video
        {
            ExtraType = ExtraType.Trailer,
            ProductionYear = 1982,
            PremiereDate = new DateTime(1982, 6, 25, 0, 0, 0, DateTimeKind.Utc)
        };

        _fFProbeVideoInfo.FetchEmbeddedInfo(video, CreateMediaInfoWithDates(), CreateRefreshOptions(replaceAllMetadata), new LibraryOptions());

        Assert.Equal(1982, video.ProductionYear);
        Assert.Equal(new DateTime(1982, 6, 25, 0, 0, 0, DateTimeKind.Utc), video.PremiereDate);
    }

    [Fact]
    public void FetchEmbeddedInfo_ExtraWithoutDates_StaysWithoutDates()
    {
        var video = new Video
        {
            ExtraType = ExtraType.Trailer
        };

        _fFProbeVideoInfo.FetchEmbeddedInfo(video, CreateMediaInfoWithDates(), CreateRefreshOptions(false), new LibraryOptions());

        Assert.Null(video.ProductionYear);
        Assert.Null(video.PremiereDate);
    }

    [Fact]
    public async Task ProbeVideo_ExternalSubtitle_SavesResolvedStream()
    {
        const string videoPath = MediaInfoResolverTests.VideoDirectoryPath + "/Movie.strm";
        const string subtitlePath = MediaInfoResolverTests.VideoDirectoryPath + "/Movie.eng.srt";

        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsAny<string>())).Returns(MediaProtocol.File);
        mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsRegex("^https://"))).Returns(MediaProtocol.Http);

        var mediaEncoder = new Mock<IMediaEncoder>();
        mediaEncoder
            .Setup(x => x.GetMediaInfo(It.Is<MediaInfoRequest>(r => r.MediaType == DlnaProfileType.Video), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new MediaBrowser.Model.MediaInfo.MediaInfo
                {
                    MediaStreams = [new MediaStream { Type = MediaStreamType.Video }]
                });
        mediaEncoder
            .Setup(x => x.GetMediaInfo(It.Is<MediaInfoRequest>(r => r.MediaType == DlnaProfileType.Subtitle), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new MediaBrowser.Model.MediaInfo.MediaInfo
                {
                    MediaStreams = [new MediaStream { Type = MediaStreamType.Subtitle, Codec = "subrip" }]
                });

        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(false);
        fileSystem.Setup(x => x.DirectoryExists(MediaInfoResolverTests.VideoDirectoryPath)).Returns(true);

        var localizationManager = Mock.Of<ILocalizationManager>();
        var audioResolver = new AudioResolver(
            Mock.Of<ILogger<AudioResolver>>(),
            localizationManager,
            mediaEncoder.Object,
            fileSystem.Object,
            new NamingOptions());
        var subtitleResolver = new SubtitleResolver(
            Mock.Of<ILogger<SubtitleResolver>>(),
            localizationManager,
            mediaEncoder.Object,
            fileSystem.Object,
            new NamingOptions());

        IReadOnlyList<MediaStream>? savedStreams = null;
        var streamRepository = new Mock<IMediaStreamRepository>();
        streamRepository
            .Setup(x => x.SaveMediaStreams(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<MediaStream>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, IReadOnlyList<MediaStream>, CancellationToken>((_, streams, _) => savedStreams = streams.ToArray());

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(x => x.GetLibraryOptions(It.IsAny<BaseItem>())).Returns(new LibraryOptions());

        var chapterManager = new Mock<IChapterManager>();
        chapterManager
            .Setup(x => x.RefreshChapterImages(
                It.IsAny<Video>(),
                It.IsAny<IDirectoryService>(),
                It.IsAny<IReadOnlyList<ChapterInfo>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var serverConfig = new Mock<IServerConfigurationManager>();
        serverConfig.Setup(x => x.Configuration).Returns(new ServerConfiguration());

        var probe = new FFProbeVideoInfo(
            Mock.Of<ILogger<FFProbeVideoInfo>>(),
            mediaSourceManager.Object,
            mediaEncoder.Object,
            Mock.Of<IBlurayExaminer>(),
            localizationManager,
            chapterManager.Object,
            serverConfig.Object,
            Mock.Of<MediaBrowser.Controller.Subtitles.ISubtitleManager>(),
            libraryManager.Object,
            audioResolver,
            subtitleResolver,
            Mock.Of<IMediaAttachmentRepository>(),
            streamRepository.Object);

        var video = new Mock<Video> { CallBase = true };
        video.Setup(x => x.ContainingFolderPath).Returns(MediaInfoResolverTests.VideoDirectoryPath);
        video.Setup(x => x.GetInternalMetadataPath()).Returns(MediaInfoResolverTests.MetadataDirectoryPath);
        video.Object.Id = Guid.NewGuid();
        video.Object.ParentId = Guid.Empty;
        video.Object.Path = videoPath;
        video.Object.IsShortcut = true;
        video.Object.ShortcutPath = "https://example.com/Movie.mp4";

        var directoryService = new Mock<IDirectoryService>();
        directoryService.Setup(x => x.GetFilePaths(MediaInfoResolverTests.VideoDirectoryPath, false)).Returns([videoPath, subtitlePath]);

        var previousMediaSourceManager = BaseItem.MediaSourceManager;
        BaseItem.MediaSourceManager = mediaSourceManager.Object;
        try
        {
            await probe.ProbeVideo(
                video.Object,
                new MetadataRefreshOptions(directoryService.Object)
                {
                    EnableRemoteContentProbe = true,
                    MetadataRefreshMode = MetadataRefreshMode.FullRefresh
                },
                CancellationToken.None);
        }
        finally
        {
            BaseItem.MediaSourceManager = previousMediaSourceManager;
        }

        Assert.NotNull(savedStreams);
        var subtitle = Assert.Single(savedStreams, x => x.Type == MediaStreamType.Subtitle);
        Assert.True(subtitle.IsExternal);
        Assert.Equal(subtitlePath, subtitle.Path);
        Assert.Equal([subtitlePath], video.Object.SubtitleFiles);
    }

    private static MediaBrowser.Model.MediaInfo.MediaInfo CreateMediaInfoWithDates()
        => new()
        {
            ProductionYear = 2016,
            PremiereDate = new DateTime(2016, 5, 4, 0, 0, 0, DateTimeKind.Utc)
        };

    private static MetadataRefreshOptions CreateRefreshOptions(bool replaceAllMetadata)
        => new(Mock.Of<IDirectoryService>())
        {
            ReplaceAllMetadata = replaceAllMetadata
        };
}
