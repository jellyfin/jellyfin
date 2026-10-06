using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.MediaEncoding;
using Moq;
using Xunit;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Jellyfin.Controller.Tests.MediaEncoding;

public class EncodingHelperInferAudioCodecTests
{
    [Theory]
    // Manifests and other containers that carry no inferable audio codec.
    [InlineData("m3u8", "aac")]
    [InlineData("mpd", "aac")]
    [InlineData("wtv", "aac")]
    [InlineData("", "aac")]
    // Containers with a well known audio codec.
    [InlineData("mp4", "aac")]
    [InlineData("mkv", "aac")]
    [InlineData("webm", "opus")]
    [InlineData("ts", "mp3")]
    // Containers named after the codec they carry.
    [InlineData("flac", "flac")]
    [InlineData("opus", "opus")]
    [InlineData("ac3", "ac3")]
    public void InferAudioCodec_ReturnsAnAudioCodec(string container, string expected)
    {
        Assert.Equal(expected, Create().InferAudioCodec(container));
    }

    private static EncodingHelper Create()
        => new(
            Mock.Of<IApplicationPaths>(),
            Mock.Of<IMediaEncoder>(),
            Mock.Of<ISubtitleEncoder>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<IConfigurationManager>(),
            Mock.Of<IPathManager>());
}
