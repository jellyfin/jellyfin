using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.Drawing;
using Xunit;

namespace Jellyfin.Controller.Tests.Drawing;

public static class ImageHelperTests
{
    [Fact]
    public static void GetNewImageSize_ExplicitSizeLargerThanSource_ClampsToSource()
    {
        // Regression test for https://github.com/jellyfin/jellyfin/issues/17056: the caller-supplied
        // width/height were used verbatim, so a single request could ask for a 23100x23100 encode.
        var options = new ImageProcessingOptions { Width = 23100, Height = 23100 };

        var newSize = ImageHelper.GetNewImageSize(options, new ImageDimensions(600, 336));

        Assert.Equal(336, newSize.Width);
        Assert.Equal(336, newSize.Height);
    }

    [Fact]
    public static void GetNewImageSize_WidthLargerThanSource_ClampsToSource()
    {
        var options = new ImageProcessingOptions { Width = 10000 };

        var newSize = ImageHelper.GetNewImageSize(options, new ImageDimensions(600, 336));

        Assert.Equal(600, newSize.Width);
        Assert.Equal(336, newSize.Height);
    }

    [Fact]
    public static void GetNewImageSize_FillLargerThanSource_ClampsToSource()
    {
        // ResizeFill already refused to upscale; this pins that behaviour.
        var options = new ImageProcessingOptions { FillWidth = 23100, FillHeight = 23100 };

        var newSize = ImageHelper.GetNewImageSize(options, new ImageDimensions(600, 336));

        Assert.Equal(600, newSize.Width);
        Assert.Equal(336, newSize.Height);
    }

    [Fact]
    public static void GetNewImageSize_SmallerThanSource_StillDownscales()
    {
        var options = new ImageProcessingOptions { MaxWidth = 300 };

        var newSize = ImageHelper.GetNewImageSize(options, new ImageDimensions(600, 336));

        Assert.Equal(300, newSize.Width);
        Assert.Equal(168, newSize.Height);
    }

    [Fact]
    public static void GetNewImageSize_NoSizeRequested_ReturnsSource()
    {
        var newSize = ImageHelper.GetNewImageSize(new ImageProcessingOptions(), new ImageDimensions(600, 336));

        Assert.Equal(600, newSize.Width);
        Assert.Equal(336, newSize.Height);
    }
}
