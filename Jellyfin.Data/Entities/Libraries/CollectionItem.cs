using System;
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
        /// <param name="collection">The collection.</param>
        /// <param name="previous">The previous item.</param>
        /// <param name="next">The next item.</param>
        public CollectionItem(Collection collection, CollectionItem previous, CollectionItem next)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            collection.Items.Add(this);

            if (next != null)
            {
                Next = next;
                next.Previous = this;
            }

            if (previous != null)
            {
                Previous = previous;
                previous.Next = this;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionItem"/> class.
        /// </summary>
        /// <remarks>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </remarks>
        protected CollectionItem()
        {
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
        public uint RowVersion { get; set; }

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
        /// TODO check if this properly updated Dependant and has the proper principal relationship.
        /// </remarks>
        public virtual CollectionItem Next { get; set; }

        /// <summary>
        /// Gets or sets the previous item in the collection.
        /// </summary>
        /// <remarks>
        /// TODO check if this properly updated Dependant and has the proper principal relationship.
        /// </remarks>
        public virtual CollectionItem Previous { get; set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
