using System.Xml.Serialization;

namespace Jellyfin.KodiMetadata.Models
{
    /// <summary>
    /// The id nfo tag for series.
    /// </summary>
    public class IdNfo
    {
        /// <summary>
        /// Gets or sets the imdb id.
        /// </summary>
        [XmlAttribute("IMDB")]
        public string? ImdbId { get; set; }

        /// <summary>
        /// Gets or sets the tmdb id.
        /// </summary>
        [XmlAttribute("TMDB")]
        public string? TmdbId { get; set; }

        /// <summary>
        /// Gets or sets the tvdb id.
        /// </summary>
        [XmlAttribute("TVDB")]
        [XmlText]
        public string? TvdbId { get; set; }
    }
}
