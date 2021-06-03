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
        /// <param name="library">The library.</param>
        public MusicAlbum(Library library) : base(library)
        {
            MusicAlbumMetadata = new HashSet<MusicAlbumMetadata>();
            Tracks = new HashSet<Track>();
        }

        /// <summary>
        /// Gets a collection containing the album metadata.
        /// </summary>
        public virtual ICollection<MusicAlbumMetadata> MusicAlbumMetadata { get; private set; }

        /// <summary>
        /// Gets a collection containing the tracks.
        /// </summary>
        public virtual ICollection<Track> Tracks { get; private set; }
    }
}
