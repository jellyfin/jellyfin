using System.Xml.Serialization;

namespace Jellyfin.NfoMetadata.Models
{
    /// <summary>
    /// The collection item nfo tag.
    /// </summary>
    public class CollectionItemNfo
    {
        /// <summary>
        /// Gets or sets the item path.
        /// </summary>
        [XmlElement("path")]
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the item id.
        /// </summary>
        [XmlElement("ItemId")]
        public string? ItemId { get; set; }
    }
}
