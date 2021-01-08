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
        public float? Position { get; set; }

        /// <summary>
        /// Gets or sets the total.
        /// </summary>
        [XmlElement("total")]
        public float? Total { get; set; }
    }
}
