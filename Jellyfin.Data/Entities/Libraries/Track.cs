using System.Collections.Generic;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a track.
    /// </summary>
    public class Track : LibraryItem, IHasReleases
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Track"/> class.
        /// </summary>
        /// <param name="library">The library.</param>
        public Track(Library library) : base(library)
        {
            Releases = new HashSet<Release>();
            TrackMetadata = new HashSet<TrackMetadata>();
        }

        /// <summary>
        /// Gets or sets the track number.
        /// </summary>
        public int? TrackNumber { get; set; }

        /// <inheritdoc />
        public virtual ICollection<Release> Releases { get; private set; }

        /// <summary>
        /// Gets a collection containing the track metadata.
        /// </summary>
        public virtual ICollection<TrackMetadata> TrackMetadata { get; private set; }
    }
}
