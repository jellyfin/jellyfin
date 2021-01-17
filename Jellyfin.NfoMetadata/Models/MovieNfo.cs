#pragma warning disable CA1819

using System;
using System.Xml.Serialization;

namespace Jellyfin.NfoMetadata.Models
{
    /// <summary>
    /// The video specific nfo tags.
    /// </summary>
    [XmlRoot("movie")]
    public class MovieNfo : BaseNfo
    {
        /// <summary>
        /// Gets or sets the IMDB Top 250 ranking.
        /// </summary>
        [XmlElement("top250")]
        public int? Top250 { get; set; }

        /// <summary>
        /// Gets or sets the movie set.
        /// </summary>
        [XmlElement("set")]
        public SetNfo? Set { get; set; }

        /// <summary>
        /// Gets or sets the connected TV show.
        /// </summary>
        [XmlElement("showlink")]
        public string? ShowLink { get; set; }

        /// <summary>
        /// Gets or sets the imdb id.
        /// </summary>
        [XmlElement("id")]
        public string? Id { get; set; }
    }
}
