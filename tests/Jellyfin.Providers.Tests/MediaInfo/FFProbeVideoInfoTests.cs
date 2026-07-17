using System;
using System.IO;
using AutoFixture;
using AutoFixture.AutoMoq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Providers.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

public class FFProbeVideoInfoTests
{
    private readonly FFProbeVideoInfo _fFProbeVideoInfo;
    private readonly Mock<IMediaEncoder> _mediaEncoderMock;

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
        _mediaEncoderMock = fixture.Freeze<Mock<IMediaEncoder>>();
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

    [Fact]
    public void DetectIsoType_WhenDvdTitlesExist_ReturnsDvd()
    {
        _mediaEncoderMock.SetupGet(x => x.SupportsDvdVideo).Returns(true);
        _mediaEncoderMock.SetupGet(x => x.SupportsLibBluray).Returns(true);
        _mediaEncoderMock.Setup(x => x.GetIsoTitles("/media/movie.iso", IsoType.Dvd))
            .Returns(
            [
                new IsoTitleInfo { TitleNumber = 1 }
            ]);

        var detected = _fFProbeVideoInfo.DetectIsoType(new Video
        {
            Path = "/media/movie.iso",
            VideoType = VideoType.Iso
        });

        Assert.Equal(IsoType.Dvd, detected);
        _mediaEncoderMock.Verify(x => x.GetIsoTitles("/media/movie.iso", IsoType.BluRay), Times.Never());
    }

    [Fact]
    public void DetectIsoType_WhenDvdProbeFailsAndBlurayTitlesExist_ReturnsBluRay()
    {
        _mediaEncoderMock.SetupGet(x => x.SupportsDvdVideo).Returns(true);
        _mediaEncoderMock.SetupGet(x => x.SupportsLibBluray).Returns(true);
        _mediaEncoderMock.Setup(x => x.GetIsoTitles("/media/movie.iso", IsoType.Dvd))
            .Throws(new IOException("in use"));
        _mediaEncoderMock.Setup(x => x.GetIsoTitles("/media/movie.iso", IsoType.BluRay))
            .Returns(
            [
                new IsoTitleInfo { TitleNumber = 1 }
            ]);

        var detected = _fFProbeVideoInfo.DetectIsoType(new Video
        {
            Path = "/media/movie.iso",
            VideoType = VideoType.Iso
        });

        Assert.Equal(IsoType.BluRay, detected);
    }

    [Fact]
    public void DetectIsoType_WhenNoSupportedProbeFindsTitles_ReturnsNull()
    {
        _mediaEncoderMock.SetupGet(x => x.SupportsDvdVideo).Returns(true);
        _mediaEncoderMock.SetupGet(x => x.SupportsLibBluray).Returns(true);
        _mediaEncoderMock.Setup(x => x.GetIsoTitles("/media/movie.iso", IsoType.Dvd))
            .Returns(Array.Empty<IsoTitleInfo>());
        _mediaEncoderMock.Setup(x => x.GetIsoTitles("/media/movie.iso", IsoType.BluRay))
            .Throws(new IOException("still in use"));

        var detected = _fFProbeVideoInfo.DetectIsoType(new Video
        {
            Path = "/media/movie.iso",
            VideoType = VideoType.Iso
        });

        Assert.Null(detected);
    }
}
