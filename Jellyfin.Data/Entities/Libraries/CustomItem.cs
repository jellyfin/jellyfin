using System.Collections.Generic;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a custom item.
    /// </summary>
    public class CustomItem : LibraryItem, IHasReleases
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomItem"/> class.
        /// </summary>
        /// <param name="library">The library.</param>
        public CustomItem(Library library) : base(library)
        {
            CustomItemMetadata = new HashSet<CustomItemMetadata>();
            Releases = new HashSet<Release>();
        }

        /// <summary>
        /// Gets a collection containing the metadata for this item.
        /// </summary>
        public virtual ICollection<CustomItemMetadata> CustomItemMetadata { get; private set; }

        /// <inheritdoc />
        public virtual ICollection<Release> Releases { get; private set; }
    }
}
