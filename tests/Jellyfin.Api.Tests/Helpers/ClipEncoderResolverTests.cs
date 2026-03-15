using Jellyfin.Api.Helpers;
using MediaBrowser.Controller.MediaEncoding;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers;

public class ClipEncoderResolverTests
{
    private static Mock<IMediaEncoder> NoEncoders()
    {
        var mock = new Mock<IMediaEncoder>();
        mock.Setup(e => e.SupportsEncoder(It.IsAny<string>())).Returns(false);
        return mock;
    }

    // ── ResolveEncoders — video encoder selection ─────────────────────

    [Theory]
    [InlineData("h264", "libx264")]
    [InlineData("H264", "libx264")]
    [InlineData("h265", "libx265")]
    [InlineData("hevc", "libx265")]
    [InlineData("unknown", "libx264")]
    [InlineData("", "libx264")]
    public void ResolveEncoders_VideoCodec_ReturnsExpectedEncoder(string requestedCodec, string expectedEncoder)
    {
        var (videoEncoder, _, _) = ClipEncoderResolver.ResolveEncoders(NoEncoders().Object, requestedCodec);

        Assert.Equal(expectedEncoder, videoEncoder);
    }

    [Fact]
    public void ResolveEncoders_Av1_PrefersLibSvtAv1WhenAvailable()
    {
        var mock = NoEncoders();
        mock.Setup(e => e.SupportsEncoder("libsvtav1")).Returns(true);

        var (videoEncoder, _, _) = ClipEncoderResolver.ResolveEncoders(mock.Object, "av1");

        Assert.Equal("libsvtav1", videoEncoder);
    }

    [Fact]
    public void ResolveEncoders_Av1_FallsBackToLibAomWhenSvtUnavailable()
    {
        var (videoEncoder, _, _) = ClipEncoderResolver.ResolveEncoders(NoEncoders().Object, "av1");

        Assert.Equal("libaom-av1", videoEncoder);
    }

    [Theory]
    [InlineData("h264")]
    [InlineData("h265")]
    [InlineData("av1")]
    public void ResolveEncoders_AlwaysReturnsMp4Container(string codec)
    {
        var (_, container, _) = ClipEncoderResolver.ResolveEncoders(NoEncoders().Object, codec);

        Assert.Equal("mp4", container);
    }

    // ── ResolveAacEncoder — audio encoder priority ────────────────────

    [Fact]
    public void ResolveAacEncoder_PrefersAacAt()
    {
        var mock = NoEncoders();
        mock.Setup(e => e.SupportsEncoder("aac_at")).Returns(true);

        Assert.Equal("aac_at", ClipEncoderResolver.ResolveAacEncoder(mock.Object));
    }

    [Fact]
    public void ResolveAacEncoder_FallsBackToLibFdkAac()
    {
        var mock = NoEncoders();
        mock.Setup(e => e.SupportsEncoder("libfdk_aac")).Returns(true);

        Assert.Equal("libfdk_aac", ClipEncoderResolver.ResolveAacEncoder(mock.Object));
    }

    [Fact]
    public void ResolveAacEncoder_FallsBackToBuiltinAac()
    {
        Assert.Equal("aac", ClipEncoderResolver.ResolveAacEncoder(NoEncoders().Object));
    }
}
