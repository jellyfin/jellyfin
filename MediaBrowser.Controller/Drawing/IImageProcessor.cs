#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Drawing
{
    /// <summary>
    /// Interface IImageProcessor.
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
        /// <returns>ImageDimensions.</returns>
        ImageDimensions GetImageDimensions(string path);

        /// <summary>
        /// Gets the dimensions of the image.
        /// </summary>
        /// <param name="item">The base item.</param>
        /// <param name="info">The information.</param>
        /// <returns>ImageDimensions.</returns>
        ImageDimensions GetImageDimensions(BaseItem item, ItemImageInfo info);

        /// <summary>
        /// Gets the blurhash of the image.
        /// </summary>
        /// <param name="path">Path to the image file.</param>
        /// <returns>BlurHash.</returns>
        string GetImageBlurHash(string path);

        /// <summary>
        /// Gets the blurhash of the image.
        /// </summary>
        /// <param name="path">Path to the image file.</param>
        /// <param name="imageDimensions">The image dimensions.</param>
        /// <returns>BlurHash.</returns>
        string GetImageBlurHash(string path, ImageDimensions imageDimensions);

        /// <summary>
        /// Gets the image cache tag.
        /// </summary>
        /// <param name="baseItemPath">The items basePath.</param>
        /// <param name="imageDateModified">The image last modification date.</param>
        /// <returns>Guid.</returns>
        string? GetImageCacheTag(string baseItemPath, DateTime imageDateModified);

        /// <summary>
        /// Gets the image cache tag.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="image">The image.</param>
        /// <returns>Guid.</returns>
        string? GetImageCacheTag(BaseItemDto item, ChapterInfo image);

        /// <summary>
        /// Gets the image cache tag.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="image">The image.</param>
        /// <returns>Guid.</returns>
        string GetImageCacheTag(BaseItem item, ItemImageInfo image);

        /// <summary>
        /// Gets the image cache tag.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="image">The image.</param>
        /// <returns>Guid.</returns>
        string GetImageCacheTag(BaseItemDto item, ItemImageInfo image);

        string? GetImageCacheTag(BaseItem item, ChapterInfo chapter);

        string? GetImageCacheTag(User user);

        /// <summary>
        /// Processes the image.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>Task.</returns>
        Task<(string Path, string? MimeType, DateTime DateModified)> ProcessImage(ImageProcessingOptions options);

        /// <summary>
        /// Gets the supported image output formats.
        /// </summary>
        /// <returns><see cref="IReadOnlyCollection{ImageOutput}" />.</returns>
        IReadOnlyCollection<ImageFormat> GetSupportedImageOutputFormats();

        /// <summary>
        /// Creates the image collage.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="libraryName">The library name to draw onto the collage.</param>
        void CreateImageCollage(ImageCollageOptions options, string? libraryName);
    }
}
