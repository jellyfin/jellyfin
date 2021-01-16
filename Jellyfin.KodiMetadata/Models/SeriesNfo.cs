#pragma warning disable CA1819

using System;
using System.Xml.Serialization;

namespace Jellyfin.KodiMetadata.Models
{
    /// <summary>
    /// The tv series specific nfo tags.
    /// </summary>
    [XmlRoot("tvshow")]
    public class SeriesNfo : BaseNfo
    {
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

        /// <summary>
        /// Gets or sets the series status.
        /// </summary>
        [XmlElement("status")]
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the time the series airs.
        /// </summary>
        [XmlElement("airs_time")]
        public string? AirTime { get; set; }

        /// <summary>
        /// Gets or sets the day the series airs.
        /// </summary>
        [XmlElement("airs_dayofweek")]
        public string? AirDay { get; set; }

        /// <summary>
        /// Gets or sets the external ids.
        /// </summary>
        [XmlElement("id")]
        public string? Id { get; set; }
    }
}
