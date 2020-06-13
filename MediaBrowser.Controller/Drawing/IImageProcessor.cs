using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Drawing
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
        /// Gets a value indicating whether [supports image collage creation].
        /// </summary>
        /// <value><c>true</c> if [supports image collage creation]; otherwise, <c>false</c>.</value>
        bool SupportsImageCollageCreation { get; }

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
        /// Gets the blurhash of the image.
        /// </summary>
        /// <param name="path">Path to the image file.</param>
        /// <returns>BlurHash</returns>
        string GetImageBlurHash(string path);

        /// <summary>
        /// Gets the image cache tag.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="image">The image.</param>
        /// <returns>Guid.</returns>
        string GetImageCacheTag(BaseItem item, ItemImageInfo image);

        string GetImageCacheTag(BaseItem item, ChapterInfo info);

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
        /// Gets the supported image output formats.
        /// </summary>
        /// <returns><see cref="IReadOnlyCollection{ImageOutput}" />.</returns>
        IReadOnlyCollection<ImageFormat> GetSupportedImageOutputFormats();

        /// <summary>
        /// Creates the image collage.
        /// </summary>
        /// <param name="options">The options.</param>
        void CreateImageCollage(ImageCollageOptions options);

        bool SupportsTransparency(string path);
    }
}
