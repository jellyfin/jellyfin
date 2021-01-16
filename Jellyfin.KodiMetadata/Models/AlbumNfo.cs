#pragma warning disable SA1402
#pragma warning disable CA1819
using System.Xml.Serialization;

namespace Jellyfin.KodiMetadata.Models
{
    /// <summary>
    /// The album specific nfo tags.
    /// </summary>
    [XmlRoot("album")]
    public class AlbumNfo : BaseNfo
    {
        /// <summary>
        /// Gets or sets the artists.
        /// </summary>
        [XmlArray("artist")]
        public string[]? Artists { get; set; }

        /// <summary>
        /// Gets or sets the album artists.
        /// </summary>
        [XmlArray("albumartist")]
        public string[]? AlbumArtists { get; set; }

        /// <summary>
        /// Gets or sets the album tracks.
        /// </summary>
        [XmlArray("track")]
        public TrackNfo[]? Tracks { get; set; }
    }

    /// <summary>
    /// The track nfo tag.
    /// </summary>
    public class TrackNfo
    {
        /// <summary>
        /// Gets or sets the track title.
        /// </summary>
        [XmlElement("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the track duration in the format mm:ss.
        /// </summary>
        [XmlElement("duration")]
        public string? Duration { get; set; }

        /// <summary>
        /// Gets or sets the track number on the album.
        /// </summary>
        [XmlElement("position")]
        public int? Positoin { get; set; }
    }
}
