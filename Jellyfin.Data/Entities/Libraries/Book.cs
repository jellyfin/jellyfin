#pragma warning disable CA2227

using System.Collections.Generic;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a book.
    /// </summary>
    public class Book : LibraryItem, IHasReleases
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Book"/> class.
        /// </summary>
        public Book()
        {
            BookMetadata = new HashSet<BookMetadata>();
            Releases = new HashSet<Release>();
        }

        /// <summary>
        /// Gets or sets a collection containing the metadata for this book.
        /// </summary>
        public virtual ICollection<BookMetadata> BookMetadata { get; protected set; }

        /// <inheritdoc />
        public virtual ICollection<Release> Releases { get; protected set; }
    }
}
