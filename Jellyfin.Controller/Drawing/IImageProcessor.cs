using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Providers;
using Jellyfin.Model.Drawing;
using Jellyfin.Model.Entities;

namespace Jellyfin.Controller.Drawing
{
    /// <summary>
    /// Interface IImageProcessor
    /// </summary>
    public interface IImageProcessor
    {
        /// <summary>
        /// Gets the supported input formats.
        /// </summary>
        /// <value>The supported input formats.</value>
        IReadOnlyCollection<string> SupportedInputFormats { get; }

        /// <summary>
        /// Gets the image enhancers.
        /// </summary>
        /// <value>The image enhancers.</value>
        IImageEnhancer[] ImageEnhancers { get; }

        /// <summary>
        /// Gets the dimensions of the image.
        /// </summary>
        /// <param name="path">Path to the image file.</param>
        /// <returns>ImageDimensions</returns>
        ImageDimensions GetImageDimensions(string path);

        /// <summary>
        /// Gets the dimensions of the image.
        /// </summary>
        /// <param name="item">The base item.</param>
        /// <param name="info">The information.</param>
        /// <returns>ImageDimensions</returns>
        ImageDimensions GetImageDimensions(BaseItem item, ItemImageInfo info);

        /// <summary>
        /// Gets the dimensions of the image.
        /// </summary>
        /// <param name="item">The base item.</param>
        /// <param name="info">The information.</param>
        /// <param name="updateItem">Whether or not the item info should be updated.</param>
        /// <returns>ImageDimensions</returns>
        ImageDimensions GetImageDimensions(BaseItem item, ItemImageInfo info, bool updateItem);

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
        IImageEnhancer[] GetSupportedEnhancers(BaseItem item, ImageType imageType);

        /// <summary>
        /// Gets the image cache tag.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="image">The image.</param>
        /// <returns>Guid.</returns>
        string GetImageCacheTag(BaseItem item, ItemImageInfo image);
        string GetImageCacheTag(BaseItem item, ChapterInfo info);

        /// <summary>
        /// Gets the image cache tag.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="image">The image.</param>
        /// <param name="imageEnhancers">The image enhancers.</param>
        /// <returns>Guid.</returns>
        string GetImageCacheTag(BaseItem item, ItemImageInfo image, IImageEnhancer[] imageEnhancers);

        /// <summary>
        /// Processes the image.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="toStream">To stream.</param>
        /// <returns>Task.</returns>
        Task ProcessImage(ImageProcessingOptions options, Stream toStream);

        /// <summary>
        /// Processes the image.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>Task.</returns>
        Task<(string path, string mimeType, DateTime dateModified)> ProcessImage(ImageProcessingOptions options);

        /// <summary>
        /// Gets the enhanced image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>Task{System.String}.</returns>
        Task<string> GetEnhancedImage(BaseItem item, ImageType imageType, int imageIndex);

        /// <summary>
        /// Gets the supported image output formats.
        /// </summary>
        /// <returns>IReadOnlyCollection{ImageOutput}.</returns>
        IReadOnlyCollection<ImageFormat> GetSupportedImageOutputFormats();

        /// <summary>
        /// Creates the image collage.
        /// </summary>
        /// <param name="options">The options.</param>
        void CreateImageCollage(ImageCollageOptions options);

        /// <summary>
        /// Gets a value indicating whether [supports image collage creation].
        /// </summary>
        /// <value><c>true</c> if [supports image collage creation]; otherwise, <c>false</c>.</value>
        bool SupportsImageCollageCreation { get; }

        IImageEncoder ImageEncoder { get; set; }

        bool SupportsTransparency(string path);
    }
}
