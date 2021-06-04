using System.Collections.Generic;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a photo.
    /// </summary>
    public class Photo : LibraryItem, IHasReleases
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Photo"/> class.
        /// </summary>
        /// <param name="library">The library.</param>
        public Photo(Library library) : base(library)
        {
            PhotoMetadata = new HashSet<PhotoMetadata>();
            Releases = new HashSet<Release>();
        }

        /// <summary>
        /// Gets a collection containing the photo metadata.
        /// </summary>
        public virtual ICollection<PhotoMetadata> PhotoMetadata { get; private set; }

        /// <inheritdoc />
        public virtual ICollection<Release> Releases { get; private set; }
    }
}
