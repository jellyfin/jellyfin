using System.Xml.Serialization;

namespace Jellyfin.KodiMetadata.Models
{
    /// <summary>
    /// The nfo rating tag.
    /// </summary>
    public class RatingNfo
    {
        /// <summary>
        /// Gets or sets the name of the rating.
        /// </summary>
        public string? Name { get; set; }

        // /// <summary>
        // /// Gets or sets the maximum rating value.
        // /// </summary>
        // [XmlAttribute("max")]
        // public int? Max { get; set; }
        //
        // /// <summary>
        // /// Gets or sets if default.
        // /// </summary>
        // [XmlAttribute("default")]
        // public string? Default { get; set; }
        //
        // /// <summary>
        // /// Gets or sets the actual rating value.
        // /// </summary>
        // [XmlElement("value")]
        // public float? Value { get; set; }
        //
        // /// <summary>
        // /// Gets or sets the number of votes.
        // /// </summary>
        // [XmlElement("votes")]
        // public int? Votes { get; set; }
    }
}
