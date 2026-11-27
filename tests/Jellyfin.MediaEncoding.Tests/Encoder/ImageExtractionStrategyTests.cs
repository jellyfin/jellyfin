using MediaBrowser.MediaEncoding.Encoder;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.Encoder;

public class ImageExtractionStrategyTests
{
    [Fact]
    public void AttachmentStreamWithoutVideoMetadata_SkipsIFrameExtraction()
    {
        Assert.False(MediaEncoder.ShouldTryIFrameImageExtraction(false, null, 5));
    }

    [Fact]
    public void EmbeddedImageStream_SkipsIFrameExtraction()
    {
        var imageStream = new MediaStream { Type = MediaStreamType.EmbeddedImage };

        Assert.False(MediaEncoder.ShouldTryIFrameImageExtraction(false, imageStream, 3));
    }

    [Fact]
    public void NormalVideoStream_UsesIFrameExtraction()
    {
        var videoStream = new MediaStream { Type = MediaStreamType.Video };

        Assert.True(MediaEncoder.ShouldTryIFrameImageExtraction(false, videoStream, null));
        Assert.True(MediaEncoder.ShouldTryIFrameImageExtraction(false, videoStream, 0));
    }

    [Fact]
    public void AudioImage_NeverUsesIFrameExtraction()
    {
        Assert.False(MediaEncoder.ShouldTryIFrameImageExtraction(true, null, 1));
    }
}
