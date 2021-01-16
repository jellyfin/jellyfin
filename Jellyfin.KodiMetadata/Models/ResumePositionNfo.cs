using System.Xml.Serialization;

namespace Jellyfin.KodiMetadata.Models
{
    /// <summary>
    /// The nfo resumeposition tag.
    /// </summary>
    public class ResumePositionNfo
    {
        /// <summary>
        /// Gets or sets the resume point in seconds.
        /// </summary>
        [XmlElement("position")]
        public double Position { get; set; }

        /// <summary>
        /// Gets or sets the total length in seconds.
        /// </summary>
        [XmlElement("total")]
        public double Total { get; set; }
    }
}
