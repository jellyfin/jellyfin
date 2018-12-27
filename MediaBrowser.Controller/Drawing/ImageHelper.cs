using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Drawing
{
    public static class ImageHelper
    {
        public static ImageSize GetNewImageSize(ImageProcessingOptions options, ImageSize? originalImageSize)
        {
            if (originalImageSize.HasValue)
            {
                // Determine the output size based on incoming parameters
                var newSize = DrawingUtils.Resize(originalImageSize.Value, options.Width ?? 0, options.Height ?? 0, options.MaxWidth ?? 0, options.MaxHeight ?? 0);

                return newSize;
            }
            return GetSizeEstimate(options);
        }

        public static IImageProcessor ImageProcessor { get; set; }

        private static ImageSize GetSizeEstimate(ImageProcessingOptions options)
        {
            if (options.Width.HasValue && options.Height.HasValue)
            {
                return new ImageSize(options.Width.Value, options.Height.Value);
            }

            var aspect = GetEstimatedAspectRatio(options.Image.Type, options.Item);

            var width = options.Width ?? options.MaxWidth;

            if (width.HasValue)
            {
                var heightValue = width.Value / aspect;
                return new ImageSize(width.Value, heightValue);
            }

            var height = options.Height ?? options.MaxHeight ?? 200;
            var widthValue = aspect * height;
            return new ImageSize(widthValue, height);
        }

        private static double GetEstimatedAspectRatio(ImageType type, BaseItem item)
        {
            switch (type)
            {
                case ImageType.Art:
                case ImageType.Backdrop:
                case ImageType.Chapter:
                case ImageType.Screenshot:
                case ImageType.Thumb:
                    return 1.78;
                case ImageType.Banner:
                    return 5.4;
                case ImageType.Box:
                case ImageType.BoxRear:
                case ImageType.Disc:
                case ImageType.Menu:
                    return 1;
                case ImageType.Logo:
                    return 2.58;
                case ImageType.Primary:
                    return item.GetDefaultPrimaryImageAspectRatio();
                default:
                    return 1;
            }
        }
    }
}
