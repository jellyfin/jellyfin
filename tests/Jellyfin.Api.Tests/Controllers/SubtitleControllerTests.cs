using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Controllers;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

// Extracting an embedded subtitle track reads the source container to EOF, which for a remote
// source (.strm/HTTP, rclone mount) means downloading the whole file. These tests pin down that
// the request's cancellation token reaches the encoder, so the extraction dies with the request
// instead of running on after the player was closed.
public sealed class SubtitleControllerTests
{
    private readonly Mock<ILibraryManager> _libraryManager = new();
    private readonly Mock<ISubtitleEncoder> _subtitleEncoder = new();
    private readonly Guid _itemId = Guid.NewGuid();

    [Fact]
    public async Task GetSubtitle_PassesRequestAbortedToEncoder()
    {
        var cts = new CancellationTokenSource();
        var controller = CreateController(cts.Token, new MemoryStream());

        await controller.GetSubtitle(_itemId, "mediaSourceId", 1, "srt", null, null, null, null, null);

        AssertEncodedWith(cts.Token);
    }

    [Fact]
    public async Task GetSubtitleWithVttTimeMap_PassesRequestAbortedToEncoder()
    {
        var cts = new CancellationTokenSource();
        var controller = CreateController(cts.Token, new MemoryStream(Encoding.UTF8.GetBytes("WEBVTT\n")));

        await controller.GetSubtitle(_itemId, "mediaSourceId", 1, "vtt", null, null, null, null, null, addVttTimeMap: true);

        AssertEncodedWith(cts.Token);
    }

    private SubtitleController CreateController(CancellationToken requestAborted, Stream encoded)
    {
        _libraryManager.Setup(m => m.GetItemById<BaseItem>(_itemId)).Returns(new Movie());

        _subtitleEncoder.Setup(e => e.GetSubtitles(
                It.IsAny<BaseItem>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(encoded);

        var httpContext = new DefaultHttpContext { RequestAborted = requestAborted };

        return new SubtitleController(
            Mock.Of<IServerConfigurationManager>(),
            _libraryManager.Object,
            Mock.Of<ISubtitleManager>(),
            _subtitleEncoder.Object,
            Mock.Of<IMediaSourceManager>(),
            Mock.Of<IProviderManager>(),
            Mock.Of<IFileSystem>(),
            NullLogger<SubtitleController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private void AssertEncodedWith(CancellationToken expected)
    {
        _subtitleEncoder.Verify(
            e => e.GetSubtitles(
                It.IsAny<BaseItem>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<bool>(),
                expected),
            Times.Once);
    }
}
