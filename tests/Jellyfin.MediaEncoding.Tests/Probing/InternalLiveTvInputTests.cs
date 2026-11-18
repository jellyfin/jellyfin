using System;
using System.Text.Json;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.MediaEncoding.Encoder;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.Probing;

public sealed class InternalLiveTvInputTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetInputPathArgument_InternalAuthorizationDoesNotMutatePublishedMediaSource(bool recording)
    {
        const string raw = "http://127.0.0.1:8096/LiveTv/LiveStreamFiles/resource/stream.ts";
        const string authorized = raw + "?InternalLiveTvToken=server-capability";
        var access = new Mock<IInternalLiveTvAccess>(MockBehavior.Strict);
        access.Setup(value => value.GetInputUrl(raw)).Returns(authorized);
        using var encoder = CreateEncoder(access.Object);
        var source = new MediaSourceInfo
        {
            Path = recording ? "/recordings/active.ts" : raw,
            Protocol = recording ? MediaProtocol.File : MediaProtocol.Http,
            EncoderPath = recording ? raw : null,
            EncoderProtocol = recording ? MediaProtocol.Http : null
        };
        var before = JsonSerializer.Serialize(source);

        var argument = encoder.GetInputPathArgument(raw, source);

        Assert.Contains(authorized, argument, StringComparison.Ordinal);
        Assert.Equal(before, JsonSerializer.Serialize(source));
        Assert.DoesNotContain(IInternalLiveTvAccess.TokenQueryParameter, JsonSerializer.Serialize(source), StringComparison.Ordinal);
        access.Verify(value => value.GetInputUrl(raw), Times.Once);
    }

    [Fact]
    public void GetInputArgument_LocalFileDoesNotRequestAnInternalCapability()
    {
        var access = new Mock<IInternalLiveTvAccess>(MockBehavior.Strict);
        using var encoder = CreateEncoder(access.Object);
        var source = new MediaSourceInfo { Path = "/media/movie.mp4", Protocol = MediaProtocol.File };

        var argument = encoder.GetInputArgument(source.Path, source);

        Assert.Contains(source.Path, argument, StringComparison.Ordinal);
        access.Verify(value => value.GetInputUrl(It.IsAny<string>()), Times.Never);
    }

    private static MediaEncoder CreateEncoder(IInternalLiveTvAccess access)
        => new(
            NullLogger<MediaEncoder>.Instance,
            Mock.Of<IServerConfigurationManager>(),
            Mock.Of<IFileSystem>(),
            Mock.Of<IBlurayExaminer>(),
            Mock.Of<ILocalizationManager>(),
            new ConfigurationBuilder().Build(),
            Mock.Of<IServerConfigurationManager>(),
            access);
}
