using System.Xml.Serialization;

namespace Jellyfin.KodiMetadata.Models
{
    /// <summary>
    /// The artist specific nfo tags.
    /// </summary>
    [XmlRoot("artist")]
    public class ArtistNfo : BaseNfo
    {
    }
}
