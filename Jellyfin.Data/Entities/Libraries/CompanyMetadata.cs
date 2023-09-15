using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity holding metadata for a <see cref="Company"/>.
    /// </summary>
    public class CompanyMetadata : ItemMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyMetadata"/> class.
        /// </summary>
        /// <param name="title">The title or name of the object.</param>
        /// <param name="language">ISO-639-3 3-character language codes.</param>
        public CompanyMetadata(string title, string language) : base(title, language)
        {
        }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <remarks>
        /// Max length = <see cref="ushort.MaxValue"/>.
        /// </remarks>
        [MaxLength(ushort.MaxValue)]
        [StringLength(ushort.MaxValue)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the headquarters.
        /// </summary>
        /// <remarks>
        /// Max length = <see cref="byte.MaxValue"/>.
        /// </remarks>
        [MaxLength(byte.MaxValue)]
        [StringLength(byte.MaxValue)]
        public string? Headquarters { get; set; }

        /// <summary>
        /// Gets or sets the country code.
        /// </summary>
        /// <remarks>
        /// Max length = 2.
        /// </remarks>
        [MaxLength(2)]
        [StringLength(2)]
        public string? Country { get; set; }

        /// <summary>
        /// Gets or sets the homepage.
        /// </summary>
        /// <remarks>
        /// Max length = 1024.
        /// </remarks>
        [MaxLength(1024)]
        [StringLength(1024)]
        public string? Homepage { get; set; }
    }
}
