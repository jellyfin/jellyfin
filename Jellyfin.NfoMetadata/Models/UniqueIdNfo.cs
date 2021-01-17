using System.Xml.Serialization;

namespace Jellyfin.NfoMetadata.Models
{
    /// <summary>
    /// The nfo uniqueid tag.
    /// </summary>
    public class UniqueIdNfo
    {
        /// <summary>
        /// Gets or sets the scraper site identifier.
        /// </summary>
        [XmlAttribute("type")]
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is the default scraper.
        /// </summary>
        [XmlAttribute("default")]
        public bool Default { get; set; }

        /// <summary>
        /// Gets or sets the scraper site id.
        /// </summary>
        [XmlText]
        public string? Id { get; set; }
    }
}
