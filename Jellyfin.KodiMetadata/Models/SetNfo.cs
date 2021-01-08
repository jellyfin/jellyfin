using System.Xml.Serialization;

namespace Jellyfin.KodiMetadata.Models
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
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the movie set overview.
        /// </summary>
        [XmlElement("overview")]
        public string? Overview { get; set; }
    }
}
