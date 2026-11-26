using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Providers.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

public class PlaybackProbeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AudioProbe_TechnicalScope_PreservesDescriptions(bool empty)
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        var encoder = fixture.Freeze<Mock<IMediaEncoder>>();
        encoder.Setup(e => e.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaBrowser.Model.MediaInfo.MediaInfo
            {
                Name = "Embedded title", Genres = ["Embedded genre"], RunTimeTicks = TimeSpan.TicksPerMinute,
                MediaStreams = [new MediaStream { Type = MediaStreamType.Audio, Codec = "aac" }]
            });
        var streams = fixture.Freeze<Mock<IMediaStreamRepository>>();
        var audio = new Audio { Name = "Edited", Genres = empty ? [] : ["Edited"], Overview = empty ? string.Empty : "Edited" };
        var result = await fixture.Create<AudioFileProber>().Probe(
            audio,
            new MetadataRefreshOptions(Mock.Of<IDirectoryService>()) { RefreshMediaInfoOnly = true },
            TestContext.Current.CancellationToken);
        Assert.Equal(ItemUpdateType.MetadataImport, result);
        Assert.Equal("Edited", audio.Name);
        Assert.Equal(empty ? Array.Empty<string>() : ["Edited"], audio.Genres);
        Assert.Equal(empty ? string.Empty : "Edited", audio.Overview);
        Assert.Equal(TimeSpan.TicksPerMinute, audio.RunTimeTicks);
        streams.Verify(s => s.SaveMediaStreams(audio.Id, It.IsAny<IReadOnlyList<MediaStream>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Probe_NoPrimaryStream_DoesNotClearExistingInformation(bool audio)
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Freeze<Mock<IMediaEncoder>>()
            .Setup(e => e.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaBrowser.Model.MediaInfo.MediaInfo { MediaStreams = [] });
        var streams = fixture.Freeze<Mock<IMediaStreamRepository>>();
        var options = new MetadataRefreshOptions(Mock.Of<IDirectoryService>()) { RefreshMediaInfoOnly = true };
        BaseItem item = audio ? new Audio { RunTimeTicks = 42 } : new Video { RunTimeTicks = 42, Width = 1920 };
        var result = audio
            ? await fixture.Create<AudioFileProber>().Probe((Audio)item, options, TestContext.Current.CancellationToken)
            : await fixture.Create<FFProbeVideoInfo>().ProbeVideo((Video)item, options, TestContext.Current.CancellationToken);
        Assert.Equal(ItemUpdateType.None, result);
        Assert.Equal(42, item.RunTimeTicks);
        if (!audio)
        {
            Assert.Equal(1920, item.Width);
        }

        streams.VerifyNoOtherCalls();
    }
}
