using System;
using AutoFixture;
using AutoFixture.AutoMoq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

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
