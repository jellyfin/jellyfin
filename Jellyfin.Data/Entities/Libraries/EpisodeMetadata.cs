using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity containing metadata for an <see cref="Episode"/>.
    /// </summary>
    public class EpisodeMetadata : ItemMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EpisodeMetadata"/> class.
        /// </summary>
        /// <param name="title">The title or name of the object.</param>
        /// <param name="language">ISO-639-3 3-character language codes.</param>
        public EpisodeMetadata(string title, string language) : base(title, language)
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

        /// <summary>
        /// Gets or sets the plot.
        /// </summary>
        /// <remarks>
        /// Max length = 65535.
        /// </remarks>
        [MaxLength(65535)]
        [StringLength(65535)]
        public string? Plot { get; set; }

        /// <summary>
        /// Gets or sets the tagline.
        /// </summary>
        /// <remarks>
        /// Max length = 1024.
        /// </remarks>
        [MaxLength(1024)]
        [StringLength(1024)]
        public string? Tagline { get; set; }
    }
}
