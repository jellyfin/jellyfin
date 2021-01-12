#pragma warning disable CA1819

using System.Xml.Serialization;

namespace Jellyfin.KodiMetadata.Models
{
    /// <summary>
    /// The music video specific nfo tags.
    /// </summary>
    [XmlRoot("musicvideo")]
    public class MusicVideoNfo : BaseNfo
    {
        /// <summary>
        /// Gets or sets the music video album.
        /// </summary>
        [XmlElement("album")]
        public string? Album { get; set; }

        /// <summary>
        /// Gets or sets the music video artists.
        /// </summary>
        [XmlElement("artist")]
        public string[]? Artists { get; set; }
    }
}
