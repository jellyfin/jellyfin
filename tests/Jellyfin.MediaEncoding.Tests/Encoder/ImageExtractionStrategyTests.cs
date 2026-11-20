using MediaBrowser.MediaEncoding.Encoder;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.Encoder;

public class ImageExtractionStrategyTests
{
    [Theory]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    public void AttachedPicturesAndAudio_DoNotRunVideoIFrameRetry(
        bool isAudio,
        bool isAttachedPicture,
        bool expected)
    {
        Assert.Equal(
            expected,
            MediaEncoder.ShouldTryIFrameImageExtraction(isAudio, isAttachedPicture));
    }
}
