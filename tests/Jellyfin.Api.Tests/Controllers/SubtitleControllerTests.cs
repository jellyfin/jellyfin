using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class SubtitleControllerTests
{
    private readonly SubtitleController _subject;
    private readonly Mock<ILibraryManager> _mockLibraryManager;
    private readonly Mock<IMediaSourceManager> _mockMediaSourceManager;

    public SubtitleControllerTests()
    {
        _mockLibraryManager = new Mock<ILibraryManager>();
        _mockMediaSourceManager = new Mock<IMediaSourceManager>();

        _subject = new SubtitleController(
            Mock.Of<IServerConfigurationManager>(),
            _mockLibraryManager.Object,
            Mock.Of<ISubtitleManager>(),
            Mock.Of<ISubtitleEncoder>(),
            _mockMediaSourceManager.Object,
            Mock.Of<IProviderManager>(),
            Mock.Of<IFileSystem>(),
            Mock.Of<ILogger<SubtitleController>>());

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(InternalClaimTypes.UserId, Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)),
            new Claim(InternalClaimTypes.Token, "test-token"),
        };

        _subject.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            }
        };
    }

    [Fact]
    public async Task GetSubtitlePlaylist_ShouldUseKeyframeAlignedSegments()
    {
        // Keyframe-aligned video segments for 60s content with ~5s GOP and 6s desired segment length:
        //   Keyframes at: 0, 5.2, 10.8, 15.9, 21.1, 26.4, 31.5, 36.7, 42, 47.2, 52.5, 57.8s
        //   Video segments start at: 0, 10.8, 21.1, 31.5, 42, 52.5s
        //   Fixed 6s subtitle segments start at: 0, 6, 12, 18, 24, 30, 36, 42, 48, 54s
        // The divergence after segment 0 causes persistent subtitle offset on seek.
        var expectedStartTicks = new[]
        {
            TimeSpan.FromMilliseconds(0).Ticks,
            TimeSpan.FromMilliseconds(10800).Ticks,
            TimeSpan.FromMilliseconds(21100).Ticks,
            TimeSpan.FromMilliseconds(31500).Ticks,
            TimeSpan.FromMilliseconds(42000).Ticks,
            TimeSpan.FromMilliseconds(52500).Ticks,
        };

        var itemId = Guid.NewGuid();
        var mediaSourceId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        var runtimeTicks = TimeSpan.FromSeconds(60).Ticks;

        _mockLibraryManager
            .Setup(l => l.GetItemById<Video>(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Returns(new Video());

        _mockMediaSourceManager
            .Setup(m => m.GetMediaSource(
                It.IsAny<BaseItem>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaSourceInfo
            {
                RunTimeTicks = runtimeTicks,
                Path = "/media/test.mkv"
            });

        var result = await _subject.GetSubtitlePlaylist(itemId, 0, mediaSourceId, 6);

        var fileResult = Assert.IsType<FileContentResult>(result);
        var actualStartTicks = ParseStartPositionTicks(
            Encoding.UTF8.GetString(fileResult.FileContents));

        Assert.Equal(expectedStartTicks.Length, actualStartTicks.Count);
        for (var i = 0; i < expectedStartTicks.Length; i++)
        {
            Assert.Equal(expectedStartTicks[i], actualStartTicks[i]);
        }
    }

    private static List<long> ParseStartPositionTicks(string playlist)
    {
        var startPositions = new List<long>();
        foreach (var line in playlist.Split('\n'))
        {
            if (!line.StartsWith("stream.vtt", StringComparison.Ordinal))
            {
                continue;
            }

            var queryString = line[(line.IndexOf('?', StringComparison.Ordinal) + 1)..];
            foreach (var pair in queryString.Split('&'))
            {
                if (pair.StartsWith("StartPositionTicks=", StringComparison.Ordinal))
                {
                    startPositions.Add(long.Parse(
                        pair["StartPositionTicks=".Length..].Trim(),
                        CultureInfo.InvariantCulture));
                }
            }
        }

        return startPositions;
    }
}
