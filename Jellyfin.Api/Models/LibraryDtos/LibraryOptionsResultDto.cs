using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Api.Models.LibraryDtos
{
    /// <summary>
    /// Library options result dto.
    /// </summary>
    public class LibraryOptionsResultDto
    {
        /// <summary>
        /// Gets or sets the metadata savers.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:ReturnArrays", MessageId = "MetadataSavers", Justification = "Imported from ServiceStack")]
        public LibraryOptionInfoDto[] MetadataSavers { get; set; } = null!;

        /// <summary>
        /// Gets or sets the metadata readers.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:ReturnArrays", MessageId = "MetadataReaders", Justification = "Imported from ServiceStack")]
        public LibraryOptionInfoDto[] MetadataReaders { get; set; } = null!;

        /// <summary>
        /// Gets or sets the subtitle fetchers.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:ReturnArrays", MessageId = "SubtitleFetchers", Justification = "Imported from ServiceStack")]
        public LibraryOptionInfoDto[] SubtitleFetchers { get; set; } = null!;

        /// <summary>
        /// Gets or sets the type options.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:ReturnArrays", MessageId = "TypeOptions", Justification = "Imported from ServiceStack")]
        public LibraryTypeOptionsDto[] TypeOptions { get; set; } = null!;
    }
}
