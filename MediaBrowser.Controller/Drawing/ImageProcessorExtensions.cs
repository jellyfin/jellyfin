#pragma warning disable CS1591

using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Drawing
{
    public static class ImageProcessorExtensions
    {
        public static string? GetImageCacheTag(this IImageProcessor processor, BaseItem item, ImageType imageType)
        {
            return processor.GetImageCacheTag(item, imageType, 0);
        }

        public static string? GetImageCacheTag(this IImageProcessor processor, BaseItem item, ImageType imageType, int imageIndex)
        {
            var imageInfo = item.GetImageInfo(imageType, imageIndex);

            if (imageInfo is null)
            {
                return null;
            }

            return processor.GetImageCacheTag(item, imageInfo);
        }
    }
}
