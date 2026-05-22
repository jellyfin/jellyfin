using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Library;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class LiveStreamHelperTests
{
    [Fact]
    public async Task AddMediaInfoWithProbe_WithCacheKey_FiltersDvbsubStreamsAndPreservesIndices()
    {
        var cacheDir = CreateTempDirectory();
        var cacheKey = "live-tv-test";
        var probedStreams = new List<MediaStream>
        {
            new() { Index = 0, Type = MediaStreamType.Data, Codec = "epg" },
            new() { Index = 1, Type = MediaStreamType.Video, Codec = "h264", Width = 1920, Height = 1080 },
            new() { Index = 2, Type = MediaStreamType.Audio, Codec = "ac3", Channels = 6 },
            new() { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "dut", IsHearingImpaired = true },
            new() { Index = 4, Type = MediaStreamType.Subtitle, Codec = "DVBTXT", Language = "rum" },
            new() { Index = 5, Type = MediaStreamType.Audio, Codec = "aac", Channels = 2 },
        };

        var mediaEncoder = new Mock<IMediaEncoder>();
        mediaEncoder
            .Setup(m => m.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaInfo
            {
                Container = "ts",
                MediaStreams = probedStreams,
            });

        var appPaths = new Mock<IApplicationPaths>();
        appPaths.Setup(p => p.CachePath).Returns(cacheDir);

        var helper = new LiveStreamHelper(
            mediaEncoder.Object,
            Mock.Of<ILogger>(),
            appPaths.Object);

        var mediaSource = new MediaSourceInfo
        {
            Id = "test-stream",
            Path = "http://localhost/stream.ts",
            Protocol = MediaProtocol.Http,
        };

        await helper.AddMediaInfoWithProbe(
            mediaSource,
            isAudio: false,
            cacheKey: cacheKey,
            addProbeDelay: false,
            TestContext.Current.CancellationToken);

        Assert.Equal("ts", mediaSource.Container);
        Assert.Null(mediaSource.DefaultSubtitleStreamIndex);
        Assert.Equal(2, mediaSource.DefaultAudioStreamIndex);

        var subtitle = Assert.Single(mediaSource.MediaStreams, s => s.Type == MediaStreamType.Subtitle);
        Assert.Equal(3, subtitle.Index);
        Assert.Equal("DVBSUB", subtitle.Codec);
        Assert.Equal("dut", subtitle.Language);
        Assert.True(subtitle.IsHearingImpaired);

        Assert.Single(mediaSource.MediaStreams, s => s.Type == MediaStreamType.Video);
        Assert.Single(mediaSource.MediaStreams, s => s.Type == MediaStreamType.Audio);
        Assert.Single(mediaSource.MediaStreams, s => s.Type == MediaStreamType.Data);
        Assert.DoesNotContain(mediaSource.MediaStreams, s => s.Codec == "DVBTXT");
        Assert.Equal(4, mediaSource.MediaStreams.Count);
        Assert.Null(mediaSource.RunTimeTicks);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "jellyfin-livestream-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
