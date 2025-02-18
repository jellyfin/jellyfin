#pragma warning disable CS1591

using MediaBrowser.Model.Drawing;

namespace MediaBrowser.Controller.Drawing;

public static class ImageHelper
{
    public static ImageDimensions GetNewImageSize(ImageProcessingOptions options, ImageDimensions originalImageSize)
    {
        // Determine the output size based on incoming parameters
        var newSize = DrawingUtils.Resize(originalImageSize, options.Width ?? 0, options.Height ?? 0, options.MaxWidth ?? 0, options.MaxHeight ?? 0);
        newSize = DrawingUtils.ResizeFill(newSize, options.FillWidth, options.FillHeight);
        return newSize;
    }
}
