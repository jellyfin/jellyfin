using System.Collections.Generic;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a movie.
    /// </summary>
    public class Movie : LibraryItem, IHasReleases
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Movie"/> class.
        /// </summary>
        /// <param name="library">The library.</param>
        public Movie(Library library) : base(library)
        {
            Releases = new HashSet<Release>();
            MovieMetadata = new HashSet<MovieMetadata>();
        }

        /// <inheritdoc />
        public virtual ICollection<Release> Releases { get; private set; }

        /// <summary>
        /// Gets a collection containing the metadata for this movie.
        /// </summary>
        public virtual ICollection<MovieMetadata> MovieMetadata { get; private set; }
    }
}
