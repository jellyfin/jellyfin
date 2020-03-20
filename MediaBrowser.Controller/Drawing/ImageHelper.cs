using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Drawing
{
    public static class ImageHelper
    {
        public static ImageDimensions GetNewImageSize(ImageProcessingOptions options, ImageDimensions? originalImageSize)
        {
            if (originalImageSize.HasValue)
            {
                // Determine the output size based on incoming parameters
                var newSize = DrawingUtils.Resize(originalImageSize.Value, options.Width ?? 0, options.Height ?? 0, options.MaxWidth ?? 0, options.MaxHeight ?? 0);

                return newSize;
            }
            return GetSizeEstimate(options);
        }

        private static ImageDimensions GetSizeEstimate(ImageProcessingOptions options)
        {
            if (options.Width.HasValue && options.Height.HasValue)
            {
                return new ImageDimensions(options.Width.Value, options.Height.Value);
            }

            double aspect = GetEstimatedAspectRatio(options.Image.Type, options.Item);

            int? width = options.Width ?? options.MaxWidth;

            if (width.HasValue)
            {
                int heightValue = Convert.ToInt32((double)width.Value / aspect);
                return new ImageDimensions(width.Value, heightValue);
            }

            var height = options.Height ?? options.MaxHeight ?? 200;
            int widthValue = Convert.ToInt32(aspect * height);
            return new ImageDimensions(widthValue, height);
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
                    double defaultPrimaryImageAspectRatio = item.GetDefaultPrimaryImageAspectRatio();
                    return defaultPrimaryImageAspectRatio > 0 ? defaultPrimaryImageAspectRatio : 2.0 / 3;
                default:
                    return 1;
            }
        }
    }
}
