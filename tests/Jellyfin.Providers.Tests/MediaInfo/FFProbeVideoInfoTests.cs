using System;
using AutoFixture;
using AutoFixture.AutoMoq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
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
    [InlineData(1L, 0)]
    [InlineData(TimeSpan.TicksPerMinute * 5, 0)]
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
}
