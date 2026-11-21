using System;
using System.Globalization;
using System.IO;
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
    public async Task AddMediaInfoWithProbe_DiscardsIncompleteCachedProbeAndDoesNotCacheAnother()
    {
        var cacheDir = CreateTempDirectory();
        var cacheKey = "digi-4k";
        var cacheFile = Path.Combine(cacheDir, "mediainfo", cacheKey.GetMD5().ToString("N", CultureInfo.InvariantCulture) + ".json");
        Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);
        await File.WriteAllTextAsync(
            cacheFile,
            """{"Container":"ts","MediaStreams":[{"Type":"Video","Codec":"hevc","Width":0,"Height":0,"Index":1}]}""",
            TestContext.Current.CancellationToken);

        var mediaEncoder = new Mock<IMediaEncoder>();
        mediaEncoder
            .SetupSequence(m => m.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaInfo
            {
                Container = "ts",
                MediaStreams =
                [
                    new() { Index = 1, Type = MediaStreamType.Video, Codec = "hevc", Width = 0, Height = 0 },
                    new() { Index = 2, Type = MediaStreamType.Audio, Codec = "aac", Channels = 0, SampleRate = 0 },
                ],
            })
            .ReturnsAsync(new MediaInfo
            {
                Container = "ts",
                MediaStreams =
                [
                    new() { Index = 1, Type = MediaStreamType.Video, Codec = "hevc", Width = 3840, Height = 2160 },
                    new() { Index = 2, Type = MediaStreamType.Audio, Codec = "aac", Channels = 2, SampleRate = 48000 },
                ],
            });

        var appPaths = new Mock<IApplicationPaths>();
        appPaths.Setup(p => p.CachePath).Returns(cacheDir);
        var helper = new LiveStreamHelper(mediaEncoder.Object, Mock.Of<ILogger>(), appPaths.Object);

        var incomplete = new MediaSourceInfo
        {
            Path = "http://localhost/stream.ts",
            Protocol = MediaProtocol.Http,
            SupportsDirectPlay = true,
            SupportsDirectStream = true,
        };
        await helper.AddMediaInfoWithProbe(incomplete, false, cacheKey, false, TestContext.Current.CancellationToken);

        Assert.False(incomplete.SupportsDirectPlay);
        Assert.False(incomplete.SupportsDirectStream);
        Assert.False(File.Exists(cacheFile));

        var complete = new MediaSourceInfo
        {
            Path = "http://localhost/stream.ts",
            Protocol = MediaProtocol.Http,
            SupportsDirectPlay = true,
            SupportsDirectStream = true,
        };
        await helper.AddMediaInfoWithProbe(complete, false, cacheKey, false, TestContext.Current.CancellationToken);

        Assert.Equal(3840, complete.VideoStream.Width);
        Assert.True(complete.SupportsDirectPlay);
        Assert.True(File.Exists(cacheFile));
        mediaEncoder.Verify(m => m.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "jellyfin-livestream-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
