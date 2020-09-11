#pragma warning disable CA2227

using System.Collections.Generic;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a music album.
    /// </summary>
    public class MusicAlbum : LibraryItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MusicAlbum"/> class.
        /// </summary>
        public MusicAlbum()
        {
            MusicAlbumMetadata = new HashSet<MusicAlbumMetadata>();
            Tracks = new HashSet<Track>();
        }

        /// <summary>
        /// Gets or sets a collection containing the album metadata.
        /// </summary>
        public virtual ICollection<MusicAlbumMetadata> MusicAlbumMetadata { get; protected set; }

        /// <summary>
        /// Gets or sets a collection containing the tracks.
        /// </summary>
        public virtual ICollection<Track> Tracks { get; protected set; }
    }
}
