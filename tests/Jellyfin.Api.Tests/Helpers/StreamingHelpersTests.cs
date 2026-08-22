using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Helpers;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers;

public class StreamingHelpersTests
{
    [Fact]
    public async Task GetStreamingState_AlwaysBurnInSubtitleWhenTranscoding_UsesUserDefaultSubtitle()
    {
        var item = new Video { Id = Guid.NewGuid() };
        var user = new User("test", "auth", "password");
        var subtitle = new MediaStream
        {
            Index = 8,
            Type = MediaStreamType.Subtitle,
            Codec = "subrip",
            Language = "eng",
            SupportsExternalStream = true
        };
        var graphicalSubtitle = new MediaStream
        {
            Index = 7,
            Type = MediaStreamType.Subtitle,
            Codec = "PGSSUB",
            Language = "eng",
            SupportsExternalStream = true
        };
        var source = new MediaSourceInfo
        {
            Id = item.Id.ToString("N"),
            Path = "/media/movie.mkv",
            Protocol = MediaProtocol.File,
            Container = "mkv",
            RunTimeTicks = TimeSpan.FromMinutes(5).Ticks,
            SupportsDirectPlay = true,
            SupportsDirectStream = true,
            SupportsTranscoding = true,
            DefaultSubtitleStreamIndex = subtitle.Index,
            MediaStreams = new List<MediaStream>
            {
                new()
                {
                    Index = 0,
                    Type = MediaStreamType.Video,
                    Codec = "h264",
                    BitRate = 20_000_000,
                    Width = 1920,
                    Height = 1080,
                    IsAVC = true
                },
                new()
                {
                    Index = 1,
                    Type = MediaStreamType.Audio,
                    Codec = "aac",
                    Channels = 2,
                    BitRate = 192_000,
                    SampleRate = 48_000
                },
                graphicalSubtitle,
                subtitle
            }
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = $"/Videos/{item.Id}/master.m3u8";
        httpContext.Request.Headers[HeaderNames.UserAgent] = "Jellyfin test";
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(InternalClaimTypes.UserId, user.Id.ToString("N"))],
            "Test"));

        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager
            .Setup(manager => manager.GetPlaybackMediaSources(
                item,
                It.Is<User>(requestUser => ReferenceEquals(requestUser, user)),
                false,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([source]);

        var userManager = new Mock<IUserManager>();
        userManager.Setup(manager => manager.GetUserById(user.Id)).Returns(user);

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(manager => manager.GetItemById<BaseItem>(item.Id)).Returns(item);

        var mediaEncoder = new Mock<IMediaEncoder>();
        mediaEncoder.Setup(encoder => encoder.CanEncodeToAudioCodec(It.IsAny<string>())).Returns(true);
        mediaEncoder.SetupGet(encoder => encoder.EncoderVersion).Returns(new Version(7, 1));

        var appPaths = new Mock<IApplicationPaths>();
        var serverConfigurationManager = new Mock<IServerConfigurationManager>();
        serverConfigurationManager.SetupGet(manager => manager.CommonApplicationPaths).Returns(appPaths.Object);
        serverConfigurationManager
            .Setup(manager => manager.GetConfiguration("encoding"))
            .Returns(new EncodingOptions { TranscodingTempPath = "/tmp" });

        var encodingHelper = new EncodingHelper(
            Mock.Of<IApplicationPaths>(),
            mediaEncoder.Object,
            Mock.Of<ISubtitleEncoder>(),
            Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>(),
            Mock.Of<MediaBrowser.Common.Configuration.IConfigurationManager>(),
            Mock.Of<IPathManager>());

        var request = new VideoRequestDto
        {
            Id = item.Id,
            MediaSourceId = source.Id,
            AudioCodec = "aac",
            VideoCodec = "h264",
            AudioStreamIndex = 1,
            VideoBitRate = 8_000_000,
            AlwaysBurnInSubtitleWhenTranscoding = true,
            SubtitleMethod = SubtitleDeliveryMethod.External
        };

        using var state = await StreamingHelpers.GetStreamingState(
            request,
            httpContext,
            mediaSourceManager.Object,
            userManager.Object,
            libraryManager.Object,
            serverConfigurationManager.Object,
            mediaEncoder.Object,
            encodingHelper,
            Mock.Of<ITranscodeManager>(),
            TranscodingJobType.Hls,
            CancellationToken.None);

        Assert.Equal(subtitle.Index, request.SubtitleStreamIndex);
        Assert.Same(subtitle, state.SubtitleStream);
    }
}
