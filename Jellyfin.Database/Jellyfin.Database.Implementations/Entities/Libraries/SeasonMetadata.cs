using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity that holds metadata for seasons.
    /// </summary>
    public class SeasonMetadata : ItemMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SeasonMetadata"/> class.
        /// </summary>
        /// <param name="title">The title or name of the object.</param>
        /// <param name="language">ISO-639-3 3-character language codes.</param>
        public SeasonMetadata(string title, string language) : base(title, language)
        {
        }

        /// <summary>
        /// Gets or sets the outline.
        /// </summary>
        /// <remarks>
        /// Max length = 1024.
        /// </remarks>
        [MaxLength(1024)]
        [StringLength(1024)]
        public string? Outline { get; set; }
    }
}
