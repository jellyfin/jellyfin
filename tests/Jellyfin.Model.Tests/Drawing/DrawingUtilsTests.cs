using MediaBrowser.Model.Drawing;
using Xunit;

namespace Jellyfin.Model.Drawing;

public static class DrawingUtilsTests
{
    [Theory]
    // Already inside the box, returned untouched.
    [InlineData(600, 336, 1920, 1080, 600, 336)]
    [InlineData(1920, 1080, 1920, 1080, 1920, 1080)]
    // Scaled down uniformly, requested aspect ratio preserved.
    [InlineData(23100, 23100, 1920, 1080, 1080, 1080)]
    [InlineData(3840, 2160, 1920, 1080, 1920, 1080)]
    [InlineData(1200, 400, 600, 336, 600, 200)]
    // Extreme ratios still produce at least one pixel per axis.
    [InlineData(10000, 1, 100, 100, 100, 1)]
    // Degenerate inputs are passed through rather than dividing by zero.
    [InlineData(600, 336, 0, 0, 600, 336)]
    [InlineData(0, 0, 1920, 1080, 0, 0)]
    public static void ScaleDownToFit_Bounds_WithoutUpscaling(int width, int height, int boxWidth, int boxHeight, int expectedWidth, int expectedHeight)
    {
        var scaled = DrawingUtils.ScaleDownToFit(new ImageDimensions(width, height), new ImageDimensions(boxWidth, boxHeight));

        Assert.Equal(expectedWidth, scaled.Width);
        Assert.Equal(expectedHeight, scaled.Height);
    }
}
