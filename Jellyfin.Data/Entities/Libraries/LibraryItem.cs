using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a library item.
    /// </summary>
    public abstract class LibraryItem : IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryItem"/> class.
        /// </summary>
        /// <param name="library">The library of this item.</param>
        protected LibraryItem(Library library)
        {
            DateAdded = DateTime.UtcNow;
            Library = library;
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        /// <remarks>
        /// Identity, Indexed, Required.
        /// </remarks>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        /// <summary>
        /// Gets the date this library item was added.
        /// </summary>
        public DateTime DateAdded { get; private set; }

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; private set; }

        /// <summary>
        /// Gets or sets the library of this item.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public virtual Library Library { get; set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
