using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities
{
    public interface IHasImages : IHasProviderIds
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; set; }

        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <value>The path.</value>
        string Path { get; set; }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        Guid Id { get; }

        /// <summary>
        /// Gets the type of the location.
        /// </summary>
        /// <value>The type of the location.</value>
        LocationType LocationType { get; }

        /// <summary>
        /// Gets the image path.
        /// </summary>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>System.String.</returns>
        string GetImagePath(ImageType imageType, int imageIndex);

        /// <summary>
        /// Gets the image date modified.
        /// </summary>
        /// <param name="imagePath">The image path.</param>
        /// <returns>DateTime.</returns>
        DateTime GetImageDateModified(string imagePath);

        /// <summary>
        /// Sets the image.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="index">The index.</param>
        /// <param name="path">The path.</param>
        void SetImagePath(ImageType type, int index, string path);

        /// <summary>
        /// Determines whether the specified type has image.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns><c>true</c> if the specified type has image; otherwise, <c>false</c>.</returns>
        bool HasImage(ImageType type, int imageIndex);

        /// <summary>
        /// Swaps the images.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="index1">The index1.</param>
        /// <param name="index2">The index2.</param>
        /// <returns>Task.</returns>
        Task SwapImages(ImageType type, int index1, int index2);

        /// <summary>
        /// Gets the display type of the media.
        /// </summary>
        /// <value>The display type of the media.</value>
        string DisplayMediaType { get; set; }

        /// <summary>
        /// Gets or sets the primary image path.
        /// </summary>
        /// <value>The primary image path.</value>
        string PrimaryImagePath { get; }

        /// <summary>
        /// Gets the preferred metadata language.
        /// </summary>
        /// <returns>System.String.</returns>
        string GetPreferredMetadataLanguage();

        /// <summary>
        /// Validates the images and returns true or false indicating if any were removed.
        /// </summary>
        bool ValidateImages();

        /// <summary>
        /// Gets or sets the backdrop image paths.
        /// </summary>
        /// <value>The backdrop image paths.</value>
        List<string> BackdropImagePaths { get; set; }

        /// <summary>
        /// Determines whether [contains image with source URL] [the specified URL].
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns><c>true</c> if [contains image with source URL] [the specified URL]; otherwise, <c>false</c>.</returns>
        bool ContainsImageWithSourceUrl(string url);
    }

    public static class HasImagesExtensions
    {
        /// <summary>
        /// Gets the image path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <returns>System.String.</returns>
        public static string GetImagePath(this IHasImages item, ImageType imageType)
        {
            return item.GetImagePath(imageType, 0);
        }

        public static bool HasImage(this IHasImages item, ImageType imageType)
        {
            return item.HasImage(imageType, 0);
        }

        /// <summary>
        /// Sets the image path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="path">The path.</param>
        public static void SetImagePath(this IHasImages item, ImageType imageType, string path)
        {
            item.SetImagePath(imageType, 0, path);
        }
    }
}
