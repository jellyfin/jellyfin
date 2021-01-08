using System.Xml.Serialization;

namespace Jellyfin.KodiMetadata.Models
{
    /// <summary>
    /// The tv series specific nfo tags.
    /// </summary>
    public class SeriesNfo
    {
        /// <summary>
        /// Gets or sets the sort title.
        /// </summary>
        [XmlElement("sorttitle")]
        public string? SortTitle { get; set; }

        /// <summary>
        /// Gets or sets the show title / alternative title.
        /// </summary>
        [XmlElement("showtitle")]
        public string? ShowTitle { get; set; }

        /// <summary>
        /// Gets or sets the IMDB Top 250 ranking.
        /// </summary>
        [XmlElement("top250")]
        public int? Top250 { get; set; }

        /// <summary>
        /// Gets or sets the number of seasons.
        /// </summary>
        [XmlElement("season")]
        public int? Season { get; set; }

        /// <summary>
        /// Gets or sets the number of episodes.
        /// </summary>
        [XmlElement("episode")]
        public int? Episode { get; set; }

        // TODO Displayepisode

        // TODO Displayseason

        // TODO namedseason
    }
}
