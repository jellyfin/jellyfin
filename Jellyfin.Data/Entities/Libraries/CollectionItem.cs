using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a collection item.
    /// </summary>
    public class CollectionItem : IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionItem"/> class.
        /// </summary>
        /// <param name="libraryItem">The library item.</param>
        public CollectionItem(LibraryItem libraryItem)
        {
            LibraryItem = libraryItem;
        }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <remarks>
        /// Identity, Indexed, Required.
        /// </remarks>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; private set; }

        /// <summary>
        /// Gets or sets the library item.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public virtual LibraryItem LibraryItem { get; set; }

        /// <summary>
        /// Gets or sets the next item in the collection.
        /// </summary>
        /// <remarks>
        /// TODO check if this properly updated Dependent and has the proper principal relationship.
        /// </remarks>
        public virtual CollectionItem? Next { get; set; }

        /// <summary>
        /// Gets or sets the previous item in the collection.
        /// </summary>
        /// <remarks>
        /// TODO check if this properly updated Dependent and has the proper principal relationship.
        /// </remarks>
        public virtual CollectionItem? Previous { get; set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
