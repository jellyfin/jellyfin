using System.Diagnostics.CodeAnalysis;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Api.Models.LibraryDtos
{
    /// <summary>
    /// Library type options dto.
    /// </summary>
    public class LibraryTypeOptionsDto
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the metadata fetchers.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:ReturnArrays", MessageId = "MetadataFetchers", Justification = "Imported from ServiceStack")]
        public LibraryOptionInfoDto[] MetadataFetchers { get; set; } = null!;

        /// <summary>
        /// Gets or sets the image fetchers.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:ReturnArrays", MessageId = "ImageFetchers", Justification = "Imported from ServiceStack")]
        public LibraryOptionInfoDto[] ImageFetchers { get; set; } = null!;

        /// <summary>
        /// Gets or sets the supported image types.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:ReturnArrays", MessageId = "SupportedImageTypes", Justification = "Imported from ServiceStack")]
        public ImageType[] SupportedImageTypes { get; set; } = null!;

        /// <summary>
        /// Gets or sets the default image options.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:ReturnArrays", MessageId = "DefaultImageOptions", Justification = "Imported from ServiceStack")]
        public ImageOption[] DefaultImageOptions { get; set; } = null!;
    }
}
