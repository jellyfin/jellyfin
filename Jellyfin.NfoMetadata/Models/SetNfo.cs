using System.Xml.Serialization;

namespace Jellyfin.NfoMetadata.Models
{
    /// <summary>
    /// The set nfo tag.
    /// </summary>
    public class SetNfo
    {
        /// <summary>
        /// Gets or sets the movie set name.
        /// </summary>
        [XmlElement("name")]
        [XmlText]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the movie set overview.
        /// </summary>
        [XmlElement("overview")]
        public string? Overview { get; set; }

        /// <summary>
        /// Gets or sets the tmdb collection id.
        /// </summary>
        [XmlAttribute("tmdbcolid")]
        public string? TmdbCollectionId { get; set; }
    }
}
