using System.Xml.Serialization;

namespace Jellyfin.NfoMetadata.Models
{
    /// <summary>
    /// The nfo rating tag.
    /// </summary>
    [XmlType("rating")]
    public class RatingNfo
    {
        /// <summary>
        /// Gets or sets the name of the rating.
        /// </summary>
        [XmlAttribute("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the maximum rating value.
        /// </summary>
        [XmlAttribute("max")]
        public int Max { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this rating is the default.
        /// </summary>
        [XmlAttribute("default")]
        public bool Default { get; set; }

        /// <summary>
        /// Gets or sets the actual rating value.
        /// </summary>
        [XmlElement("value")]
        public float? Value { get; set; }

        /// <summary>
        /// Gets or sets the number of votes.
        /// </summary>
        [XmlElement("votes")]
        public int? Votes { get; set; }
    }
}
