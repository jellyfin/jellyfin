using System;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Controller.Library;

namespace MediaBrowser.Controller.Entities
{
    public interface IHasImages : IHasProviderIds, IHasId
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
        /// Gets the file name without extension.
        /// </summary>
        /// <value>The file name without extension.</value>
        string FileNameWithoutExtension { get; }

        /// <summary>
        /// Gets the type of the location.
        /// </summary>
        /// <value>The type of the location.</value>
        LocationType LocationType { get; }

        /// <summary>
        /// Gets the locked fields.
        /// </summary>
        /// <value>The locked fields.</value>
        List<MetadataFields> LockedFields { get; }

        /// <summary>
        /// Gets the images.
        /// </summary>
        /// <param name="imageType">Type of the image.</param>
        /// <returns>IEnumerable{ItemImageInfo}.</returns>
        IEnumerable<ItemImageInfo> GetImages(ImageType imageType);

        /// <summary>
        /// Gets the image path.
        /// </summary>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>System.String.</returns>
        string GetImagePath(ImageType imageType, int imageIndex);

        /// <summary>
        /// Gets the image information.
        /// </summary>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>ItemImageInfo.</returns>
        ItemImageInfo GetImageInfo(ImageType imageType, int imageIndex);

        /// <summary>
        /// Sets the image.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="index">The index.</param>
        /// <param name="file">The file.</param>
        void SetImagePath(ImageType type, int index, FileSystemMetadata file);

        /// <summary>
        /// Determines whether the specified type has image.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns><c>true</c> if the specified type has image; otherwise, <c>false</c>.</returns>
        bool HasImage(ImageType type, int imageIndex);

        /// <summary>
        /// Allowses the multiple images.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool AllowsMultipleImages(ImageType type);

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
        bool ValidateImages(IDirectoryService directoryService);

        /// <summary>
        /// Gets a value indicating whether this instance is owned item.
        /// </summary>
        /// <value><c>true</c> if this instance is owned item; otherwise, <c>false</c>.</value>
        bool IsOwnedItem { get; }

        /// <summary>
        /// Gets the containing folder path.
        /// </summary>
        /// <value>The containing folder path.</value>
        string ContainingFolderPath { get; }

        /// <summary>
        /// Adds the images.
        /// </summary>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="images">The images.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool AddImages(ImageType imageType, List<FileSystemMetadata> images);

        /// <summary>
        /// Determines whether [is save local metadata enabled].
        /// </summary>
        /// <returns><c>true</c> if [is save local metadata enabled]; otherwise, <c>false</c>.</returns>
        bool IsSaveLocalMetadataEnabled();

        /// <summary>
        /// Gets a value indicating whether [supports local metadata].
        /// </summary>
        /// <value><c>true</c> if [supports local metadata]; otherwise, <c>false</c>.</value>
        bool SupportsLocalMetadata { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is in mixed folder.
        /// </summary>
        /// <value><c>true</c> if this instance is in mixed folder; otherwise, <c>false</c>.</value>
        bool IsInMixedFolder { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is locked.
        /// </summary>
        /// <value><c>true</c> if this instance is locked; otherwise, <c>false</c>.</value>
        bool IsLocked { get; }

        /// <summary>
        /// Gets a value indicating whether [supports remote image downloading].
        /// </summary>
        /// <value><c>true</c> if [supports remote image downloading]; otherwise, <c>false</c>.</value>
        bool SupportsRemoteImageDownloading { get; }

        /// <summary>
        /// Gets the internal metadata path.
        /// </summary>
        /// <returns>System.String.</returns>
        string GetInternalMetadataPath();

        /// <summary>
        /// Gets a value indicating whether [always scan internal metadata path].
        /// </summary>
        /// <value><c>true</c> if [always scan internal metadata path]; otherwise, <c>false</c>.</value>
        bool AlwaysScanInternalMetadataPath { get; }

        /// <summary>
        /// Determines whether [is internet metadata enabled].
        /// </summary>
        /// <returns><c>true</c> if [is internet metadata enabled]; otherwise, <c>false</c>.</returns>
        bool IsInternetMetadataEnabled();

        /// <summary>
        /// Removes the image.
        /// </summary>
        /// <param name="image">The image.</param>
        void RemoveImage(ItemImageInfo image);

        /// <summary>
        /// Updates to repository.
        /// </summary>
        /// <param name="updateReason">The update reason.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateToRepository(ItemUpdateType updateReason, CancellationToken cancellationToken);

        /// <summary>
        /// Sets the image.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="index">The index.</param>
        void SetImage(ItemImageInfo image, int index);
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
        /// <param name="file">The file.</param>
        public static void SetImagePath(this IHasImages item, ImageType imageType, FileSystemMetadata file)
        {
            item.SetImagePath(imageType, 0, file);
        }

        /// <summary>
        /// Sets the image path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="file">The file.</param>
        public static void SetImagePath(this IHasImages item, ImageType imageType, string file)
        {
            if (file.StartsWith("http", System.StringComparison.OrdinalIgnoreCase))
            {
                item.SetImage(new ItemImageInfo
                {
                    Path = file,
                    Type = imageType
                }, 0);
            }
            else
            {
                item.SetImagePath(imageType, BaseItem.FileSystem.GetFileInfo(file));
            }
        }
    }
}
