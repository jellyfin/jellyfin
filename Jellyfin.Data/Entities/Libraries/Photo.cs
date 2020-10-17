#pragma warning disable CA2227

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
        public Photo()
        {
            PhotoMetadata = new HashSet<PhotoMetadata>();
            Releases = new HashSet<Release>();
        }

        /// <summary>
        /// Gets or sets a collection containing the photo metadata.
        /// </summary>
        public virtual ICollection<PhotoMetadata> PhotoMetadata { get; protected set; }

        /// <inheritdoc />
        public virtual ICollection<Release> Releases { get; protected set; }
    }
}
