using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Drawing
{
    /// <summary>
    /// Interface IImageProcessor
    /// </summary>
    public interface IImageProcessor
    {
        /// <summary>
        /// Gets the image enhancers.
        /// </summary>
        /// <value>The image enhancers.</value>
        IEnumerable<IImageEnhancer> ImageEnhancers { get; }

        /// <summary>
        /// Gets the size of the image.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>ImageSize.</returns>
        ImageSize GetImageSize(string path);

        /// <summary>
        /// Gets the size of the image.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="imageDateModified">The image date modified.</param>
        /// <returns>ImageSize.</returns>
        ImageSize GetImageSize(string path, DateTime imageDateModified);

        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="enhancers">The enhancers.</param>
        void AddParts(IEnumerable<IImageEnhancer> enhancers);

        /// <summary>
        /// Gets the supported enhancers.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <returns>IEnumerable{IImageEnhancer}.</returns>
        IEnumerable<IImageEnhancer> GetSupportedEnhancers(IHasImages item, ImageType imageType);

        /// <summary>
        /// Gets the image cache tag.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="image">The image.</param>
        /// <returns>Guid.</returns>
        string GetImageCacheTag(IHasImages item, ItemImageInfo image);

        /// <summary>
        /// Gets the image cache tag.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="originalImagePath">The original image path.</param>
        /// <param name="dateModified">The date modified.</param>
        /// <param name="imageEnhancers">The image enhancers.</param>
        /// <returns>Guid.</returns>
        string GetImageCacheTag(IHasImages item, ImageType imageType, string originalImagePath, DateTime dateModified,
                              List<IImageEnhancer> imageEnhancers);

        /// <summary>
        /// Processes the image.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="toStream">To stream.</param>
        /// <returns>Task.</returns>
        Task ProcessImage(ImageProcessingOptions options, Stream toStream);

        /// <summary>
        /// Gets the enhanced image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>Task{System.String}.</returns>
        Task<string> GetEnhancedImage(IHasImages item, ImageType imageType, int imageIndex);
    }

    public static class ImageProcessorExtensions
    {
        public static string GetImageCacheTag(this IImageProcessor processor, IHasImages item, ImageType imageType)
        {
            return processor.GetImageCacheTag(item, imageType, 0);
        }
        
        public static string GetImageCacheTag(this IImageProcessor processor, IHasImages item, ImageType imageType, int imageIndex)
        {
            var imageInfo = item.GetImageInfo(imageType, imageIndex);

            if (imageInfo == null)
            {
                return null;
            }

            return processor.GetImageCacheTag(item, imageInfo);
        }
    }
}
