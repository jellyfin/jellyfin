#pragma warning disable CA1819
#pragma warning disable SA1402

using System;
using System.Globalization;
using System.Xml.Serialization;

namespace Jellyfin.NfoMetadata.Models
{
    /// <summary>
    /// The artist specific nfo tags.
    /// </summary>
    [XmlRoot("artist")]
    public class ArtistNfo : BaseNfo
    {
        /// <summary>
        /// Gets or sets the <see cref="Disbanded"/> property. THIS IS ONLY USED FOR THE XML SERIALIZER.
        /// </summary>
        [XmlElement("disbanded")]
        public string? DisbandedXml
        {
            get
            {
                return Disbanded?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            set
            {
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
                {
                    Disbanded = date;
                }
            }
        }

        /// <summary>
        /// Gets or sets the disbanded date.
        /// </summary>
        public DateTime? Disbanded { get; set; }

        /// <summary>
        /// Gets or sets the artist albums.
        /// </summary>
        [XmlArray("albums")]
        public ArtistAlbumNfo[]? Albums { get; set; }
    }

    /// <summary>
    /// The artist album nfo tag.
    /// </summary>
    public class ArtistAlbumNfo
    {
        /// <summary>
        /// Gets or sets the album name.
        /// </summary>
        [XmlElement("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the album year.
        /// </summary>
        [XmlElement("year")]
        public int? Year { get; set; }
    }
}
