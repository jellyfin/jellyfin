using System.Xml.Serialization;

namespace Jellyfin.NfoMetadata.Models
{
    /// <summary>
    /// The season specific nfo tags.
    /// </summary>
    public class SeasonNfo : BaseNfo
    {
        /// <summary>
        /// Gets or sets the season number.
        /// </summary>
        [XmlElement("seasonnumber")]
        public int? SeasonNumber { get; set; }
    }
}
