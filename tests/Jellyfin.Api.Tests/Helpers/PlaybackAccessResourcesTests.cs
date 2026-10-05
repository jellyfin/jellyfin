using System;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.Helpers;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.MediaEncoding.Hls.Extractors;
using Jellyfin.MediaEncoding.Hls.Playlist;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers;

public sealed class PlaybackAccessResourcesTests
{
    [Fact]
    public async Task SubtitlePlaylistPreservesThePlaybackCredentialOnEverySegment()
    {
        var userId = Guid.NewGuid();
        var item = new Video { Id = Guid.NewGuid() };
        var library = new Mock<ILibraryManager>();
        library.Setup(manager => manager.GetItemById<Video>(item.Id, userId)).Returns(item);
        var sources = new Mock<IMediaSourceManager>();
        sources.Setup(manager => manager.GetMediaSource(item, "source", null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaSourceInfo { Id = "source", RunTimeTicks = TimeSpan.FromSeconds(90).Ticks });
        var controller = new SubtitleController(
            Mock.Of<IServerConfigurationManager>(),
            library.Object,
            Mock.Of<ISubtitleManager>(),
            Mock.Of<ISubtitleEncoder>(),
            sources.Object,
            Mock.Of<IProviderManager>(),
            Mock.Of<IFileSystem>(),
            NullLogger<SubtitleController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[]
                        {
                            new Claim(InternalClaimTypes.UserId, userId.ToString()),
                            new Claim(InternalClaimTypes.PlaybackToken, "playback"),
                            new Claim(InternalClaimTypes.PlaybackSessionId, "session")
                        },
                        AuthenticationSchemes.PlaybackAccess))
                }
            }
        };

        var result = Assert.IsType<FileContentResult>(await controller.GetSubtitlePlaylist(item.Id, 0, "source", 30));
        var playlist = Encoding.UTF8.GetString(result.FileContents);
        var segments = playlist.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var count = 0;
        foreach (var segment in segments)
        {
            if (!segment.StartsWith("stream.vtt", StringComparison.Ordinal))
            {
                continue;
            }

            count++;
            Assert.Contains("PlaybackToken=playback&PlaySessionId=session", segment, StringComparison.Ordinal);
            Assert.DoesNotContain("ApiKey=", segment, StringComparison.Ordinal);
        }

        Assert.Equal(3, count);
    }

    [Theory]
    [InlineData("ts")]
    [InlineData("mp4")]
    public void HlsSegmentsAndInitializationResourcesPreserveTheGrant(string container)
    {
        var generator = new DynamicHlsPlaylistGenerator(Mock.Of<IServerConfigurationManager>(), Array.Empty<IKeyframeExtractor>());
        const string Query = "?MediaSourceId=source&DeviceId=renderer&PlaySessionId=session&PlaybackToken=playback";
        var playlist = generator.CreateMainPlaylist(new CreateMainPlaylistRequest(null, "/unused", 6000, TimeSpan.FromSeconds(12).Ticks, container, "hls1/main/", Query, false));
        Assert.Contains("hls1/main/0." + container + Query, playlist, StringComparison.Ordinal);
        Assert.Contains("hls1/main/1." + container + Query, playlist, StringComparison.Ordinal);
        if (container == "mp4")
        {
            Assert.Contains("#EXT-X-MAP:URI=\"hls1/main/-1.mp4" + Query, playlist, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("ApiKey=", playlist, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(PermissionKind.EnableVideoPlaybackTranscoding, "h264", "copy")]
    [InlineData(PermissionKind.EnableAudioPlaybackTranscoding, "copy", "aac")]
    [InlineData(PermissionKind.EnablePlaybackRemuxing, "copy", "copy")]
    public void DelegatedPlaybackCannotExceedTheOwnersEncodingPermissions(PermissionKind permission, string videoCodec, string audioCodec)
    {
        var user = new User("caster", "auth", "reset");
        user.AddDefaultPermissions();
        user.SetPermission(permission, false);
        using var state = new StreamState(Mock.Of<IMediaSourceManager>(), TranscodingJobType.Hls, Mock.Of<ITranscodeManager>())
        {
            User = user,
            MediaSource = new MediaSourceInfo(),
            Request = new VideoRequestDto(),
            VideoStream = new MediaStream(),
            AudioStream = new MediaStream(),
            OutputVideoCodec = videoCodec,
            OutputAudioCodec = audioCodec
        };
        Assert.Throws<SecurityException>(() => StreamingHelpers.ValidateDelegatedPlayback(state));
    }
}
