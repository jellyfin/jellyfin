using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Library;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library
{
    public class LiveStreamHelperTests
    {
        [Fact]
        public async Task AddMediaInfoWithProbe_WithCacheKey_PreservesAllMediaStreams()
        {
            var cachePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(cachePath);

            try
            {
                var mediaInfo = new MediaInfo
                {
                    MediaStreams =
                    [
                        new MediaStream
                        {
                            Index = 0,
                            Type = MediaStreamType.Video
                        },
                        new MediaStream
                        {
                            Index = 1,
                            Type = MediaStreamType.Audio,
                            Language = "fre"
                        },
                        new MediaStream
                        {
                            Index = 2,
                            Type = MediaStreamType.Audio,
                            Language = "qaa"
                        },
                        new MediaStream
                        {
                            Index = 3,
                            Type = MediaStreamType.Audio,
                            Language = "qad"
                        },
                        new MediaStream
                        {
                            Index = 4,
                            Type = MediaStreamType.Subtitle,
                            Language = "fre"
                        }
                    ]
                };

                var mediaEncoder = new Mock<IMediaEncoder>();
                mediaEncoder
                    .Setup(x => x.GetMediaInfo(
                        It.IsAny<MediaInfoRequest>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mediaInfo);

                var appPaths = new Mock<IApplicationPaths>();
                appPaths.SetupGet(x => x.CachePath).Returns(cachePath);

                var mediaSource = new MediaSourceInfo
                {
                    Path = "http://example.com/live.ts",
                    Protocol = MediaProtocol.Http
                };

                var helper = new LiveStreamHelper(
                    mediaEncoder.Object,
                    NullLogger.Instance,
                    appPaths.Object);

                await helper.AddMediaInfoWithProbe(
                    mediaSource,
                    false,
                    "live-tv-multistream-test",
                    false,
                    CancellationToken.None);

                Assert.Equal(5, mediaSource.MediaStreams.Count);

                Assert.Collection(
                    mediaSource.MediaStreams,
                    stream =>
                    {
                        Assert.Equal(MediaStreamType.Video, stream.Type);
                        Assert.Equal(0, stream.Index);
                    },
                    stream =>
                    {
                        Assert.Equal(MediaStreamType.Audio, stream.Type);
                        Assert.Equal(1, stream.Index);
                        Assert.Equal("fre", stream.Language);
                    },
                    stream =>
                    {
                        Assert.Equal(MediaStreamType.Audio, stream.Type);
                        Assert.Equal(2, stream.Index);
                        Assert.Equal("qaa", stream.Language);
                    },
                    stream =>
                    {
                        Assert.Equal(MediaStreamType.Audio, stream.Type);
                        Assert.Equal(3, stream.Index);
                        Assert.Equal("qad", stream.Language);
                    },
                    stream =>
                    {
                        Assert.Equal(MediaStreamType.Subtitle, stream.Type);
                        Assert.Equal(4, stream.Index);
                        Assert.Equal("fre", stream.Language);
                    });

                Assert.Equal(1, mediaSource.DefaultAudioStreamIndex);
            }
            finally
            {
                Directory.Delete(cachePath, true);
            }
        }
    }
}
