using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity holding the metadata for a music album.
    /// </summary>
    public class MusicAlbumMetadata : ItemMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MusicAlbumMetadata"/> class.
        /// </summary>
        /// <param name="title">The title or name of the album.</param>
        /// <param name="language">ISO-639-3 3-character language codes.</param>
        public MusicAlbumMetadata(string title, string language) : base(title, language)
        {
            Labels = new HashSet<Company>();
        }

        /// <summary>
        /// Gets or sets the barcode.
        /// </summary>
        /// <remarks>
        /// Max length = 255.
        /// </remarks>
        [MaxLength(255)]
        [StringLength(255)]
        public string? Barcode { get; set; }

        /// <summary>
        /// Gets or sets the label number.
        /// </summary>
        /// <remarks>
        /// Max length = 255.
        /// </remarks>
        [MaxLength(255)]
        [StringLength(255)]
        public string? LabelNumber { get; set; }

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
        /// Gets a collection containing the labels.
        /// </summary>
        public virtual ICollection<Company> Labels { get; private set; }
    }
}
