using System.Xml.Serialization;

namespace Jellyfin.KodiMetadata.Models
{
    /// <summary>
    /// The album specific nfo tags.
    /// </summary>
    [XmlRoot("album")]
    public class AlbumNfo : BaseNfo
    {
    }
}
