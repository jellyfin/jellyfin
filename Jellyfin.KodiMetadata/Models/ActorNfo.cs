using System.Xml.Serialization;
using MediaBrowser.Controller.Serialization;

namespace Jellyfin.KodiMetadata.Models
{
    /// <summary>
    /// The actor nfo tag.
    /// </summary>
    [XmlRoot("actor")]
    public class ActorNfo
    {
        /// <summary>
        /// Gets or sets the actor name.
        /// </summary>
        [XmlElement("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the actor role.
        /// </summary>
        [XmlElement("role")]
        public string? Role { get; set; }

        /// <summary>
        /// Gets or sets the actor picture.
        /// </summary>
        [XmlElement("thumb")]
        public string? Thumb { get; set; }

        /// <summary>
        /// Gets or sets the order in which the actors should appear.
        /// </summary>
        [XmlElement("order")]
        [XmlSynonyms("sortorder")]
        public int? Order { get; set; }

        /// <summary>
        /// Gets or sets the actor type. (Used for composers, producers).
        /// </summary>
        [XmlElement("type")]
        public string? Type { get; set; }
    }
}
