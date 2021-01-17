#pragma warning disable CA1819

using System;
using System.Xml.Serialization;

namespace Jellyfin.NfoMetadata.Models
{
    /// <summary>
    /// The art nfo tag.
    /// </summary>
    public class ArtNfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArtNfo"/> class.
        /// </summary>
        public ArtNfo()
        {
            Poster = Array.Empty<string>();
            Fanart = Array.Empty<string>();
        }

        /// <summary>
        /// Gets or sets the posters.
        /// </summary>
        [XmlElement("poster")]
        public string[] Poster { get; set; }

        /// <summary>
        /// Gets or sets the fanart.
        /// </summary>
        [XmlElement("fanart")]
        public string[] Fanart { get; set; }
    }
}
