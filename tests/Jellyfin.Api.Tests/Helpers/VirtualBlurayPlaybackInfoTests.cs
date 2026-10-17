using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.Api.Helpers;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.MediaEncoding.BdInfo;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers;

public class VirtualBlurayPlaybackInfoTests
{
    [Theory]
    [InlineData(false, "", true)]
    [InlineData(false, "-mp4", true)]
    [InlineData(false, "mkv", false)]
    [InlineData(false, "iso", true)]
    [InlineData(true, "", false)]
    [InlineData(true, "-mp4", false)]
    [InlineData(true, "mkv", false)]
    [InlineData(true, "iso", true)]
    [InlineData(true, "mkv,ISO", true)]
    [InlineData(true, null, true)]
    public async Task PlaybackInfo_OnlyVirtualIsoRequiresExplicitSupport(
        bool virtualIso,
        string? containers,
        bool directPlay)
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(root.FullName, "BDMV"));
            File.WriteAllBytes(Path.Combine(root.FullName, "BDMV", "index.bdmv"), [1]);
            var movie = new Movie { Id = Guid.NewGuid(), Path = root.FullName };
            var original = new MediaSourceInfo
            {
                Id = movie.Id.ToString("N"),
                Path = virtualIso ? root.FullName : root.FullName + ".iso",
                VideoType = virtualIso ? VideoType.BluRay : VideoType.Iso,
                IsoType = IsoType.BluRay,
                Protocol = MediaProtocol.File,
                Container = "iso",
                Bitrate = 1128000,
                MediaStreams =
                [
                    new MediaStream
                    {
                        Index = 0,
                        Type = MediaStreamType.Video,
                        Codec = "h264",
                        Width = 1920,
                        Height = 1080,
                        BitRate = 1000000,
                    },
                    new MediaStream
                    {
                        Index = 1,
                        Type = MediaStreamType.Audio,
                        Codec = "aac",
                        Channels = 2,
                        BitRate = 128000,
                        SampleRate = 48000,
                    },
                ],
            };
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            fixture
                .Freeze<Mock<IMediaSourceManager>>()
                .Setup(manager =>
                    manager.GetPlaybackMediaSources(
                        movie,
                        null,
                        true,
                        true,
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync([original]);
            fixture
                .Freeze<Mock<IServerConfigurationManager>>()
                .SetupGet(manager => manager.Configuration)
                .Returns(new ServerConfiguration());
            var principal = TestHelpers.SetupUser(
                fixture.Freeze<Mock<IUserManager>>(),
                new Mock<IHttpContextAccessor>(),
                "User");
            using var cache = new MemoryCache(new MemoryCacheOptions());
            using var images = new VirtualBlurayImageManager(
                cache,
                NullLogger<VirtualBlurayImageManager>.Instance,
                _ => false);
            fixture.Inject<IVirtualBlurayImageManager>(images);
            var helper = fixture.Create<MediaInfoHelper>();
            var profile = containers is null
                ? null
                : new DeviceProfile
                {
                    DirectPlayProfiles =
                    [
                        new DirectPlayProfile
                        {
                            Type = DlnaProfileType.Video,
                            Container = containers,
                        },
                    ],
                    TranscodingProfiles =
                    [
                        new TranscodingProfile
                        {
                            Type = DlnaProfileType.Video,
                            Container = "ts",
                            VideoCodec = "h264",
                            AudioCodec = "aac",
                            Protocol = MediaStreamProtocol.http,
                            Context = EncodingContext.Streaming,
                        },
                    ],
                };
            var result = await helper.GetPlaybackInfo(
                movie,
                null,
                new DefaultHttpContext().Request,
                profile: profile);
            var source = Assert.Single(result.MediaSources);
            if (profile is not null)
            {
                helper.SetDeviceSpecificData(
                    movie,
                    source,
                    profile,
                    principal,
                    null,
                    0,
                    source.Id,
                    null,
                    null,
                    null,
                    result.PlaySessionId!,
                    Guid.NewGuid(),
                    true,
                    true,
                    true,
                    true,
                    true,
                    false,
                    IPAddress.Loopback);
            }

            Assert.Equal(directPlay, source.SupportsDirectPlay);
            Assert.Equal(directPlay, source.SupportsDirectStream);
            if (!directPlay)
            {
                Assert.NotNull(source.TranscodingUrl);
                Assert.DoesNotContain(
                    "static=true",
                    source.TranscodingUrl,
                    StringComparison.OrdinalIgnoreCase);
            }

            Assert.True(original.SupportsDirectPlay);
            Assert.Equal(virtualIso ? VideoType.BluRay : VideoType.Iso, original.VideoType);
        }
        finally
        {
            root.Delete(true);
        }
    }
}
