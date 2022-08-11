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
        /// Max length = 65535.
        /// </remarks>
        [MaxLength(65535)]
        [StringLength(65535)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the headquarters.
        /// </summary>
        /// <remarks>
        /// Max length = 255.
        /// </remarks>
        [MaxLength(255)]
        [StringLength(255)]
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
