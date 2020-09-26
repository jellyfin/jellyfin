#pragma warning disable CA2227

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
        public CustomItem()
        {
            CustomItemMetadata = new HashSet<CustomItemMetadata>();
            Releases = new HashSet<Release>();
        }

        /// <summary>
        /// Gets or sets a collection containing the metadata for this item.
        /// </summary>
        public virtual ICollection<CustomItemMetadata> CustomItemMetadata { get; protected set; }

        /// <inheritdoc />
        public virtual ICollection<Release> Releases { get; protected set; }
    }
}
