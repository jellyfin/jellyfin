using System.Collections.Generic;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing an episode.
    /// </summary>
    public class Episode : LibraryItem, IHasReleases
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Episode"/> class.
        /// </summary>
        /// <param name="library">The library.</param>
        public Episode(Library library) : base(library)
        {
            Releases = new HashSet<Release>();
            EpisodeMetadata = new HashSet<EpisodeMetadata>();
        }

        /// <summary>
        /// Gets or sets the episode number.
        /// </summary>
        public int? EpisodeNumber { get; set; }

        /// <inheritdoc />
        public virtual ICollection<Release> Releases { get; private set; }

        /// <summary>
        /// Gets a collection containing the metadata for this episode.
        /// </summary>
        public virtual ICollection<EpisodeMetadata> EpisodeMetadata { get; private set; }
    }
}
