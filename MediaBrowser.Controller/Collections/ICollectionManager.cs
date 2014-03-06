using System;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Collections
{
    public interface ICollectionManager
    {
        /// <summary>
        /// Creates the collection.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>Task.</returns>
        Task CreateCollection(CollectionCreationOptions options);

        /// <summary>
        /// Adds to collection.
        /// </summary>
        /// <param name="collectionId">The collection identifier.</param>
        /// <param name="itemId">The item identifier.</param>
        /// <returns>Task.</returns>
        Task AddToCollection(Guid collectionId, Guid itemId);

        /// <summary>
        /// Removes from collection.
        /// </summary>
        /// <param name="collectionId">The collection identifier.</param>
        /// <param name="itemId">The item identifier.</param>
        /// <returns>Task.</returns>
        Task RemoveFromCollection(Guid collectionId, Guid itemId);
    }
}
