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
        /// <param name="library">The library.</param>
        public Book(Library library) : base(library)
        {
            BookMetadata = new HashSet<BookMetadata>();
            Releases = new HashSet<Release>();
        }

        /// <summary>
        /// Gets a collection containing the metadata for this book.
        /// </summary>
        public virtual ICollection<BookMetadata> BookMetadata { get; private set; }

        /// <inheritdoc />
        public virtual ICollection<Release> Releases { get; private set; }
    }
}
