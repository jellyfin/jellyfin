using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Collections
{
    public interface ICollectionManager
    {
        /// <summary>
        /// Occurs when [collection created].
        /// </summary>
        event EventHandler<CollectionCreatedEventArgs> CollectionCreated;

        /// <summary>
        /// Occurs when [items added to collection].
        /// </summary>
        event EventHandler<CollectionModifiedEventArgs> ItemsAddedToCollection;

        /// <summary>
        /// Occurs when [items removed from collection].
        /// </summary>
        event EventHandler<CollectionModifiedEventArgs> ItemsRemovedFromCollection;

        /// <summary>
        /// Creates the collection.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>Task.</returns>
        Task<BoxSet> CreateCollection(CollectionCreationOptions options);

        /// <summary>
        /// Adds to collection.
        /// </summary>
        /// <param name="collectionId">The collection identifier.</param>
        /// <param name="itemIds">The item ids.</param>
        /// <returns>Task.</returns>
        Task AddToCollection(Guid collectionId, IEnumerable<Guid> itemIds);

        /// <summary>
        /// Removes from collection.
        /// </summary>
        /// <param name="collectionId">The collection identifier.</param>
        /// <param name="itemIds">The item ids.</param>
        /// <returns>Task.</returns>
        Task RemoveFromCollection(Guid collectionId, IEnumerable<Guid> itemIds);

        /// <summary>
        /// Collapses the items within box sets.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        IEnumerable<BaseItem> CollapseItemsWithinBoxSets(IEnumerable<BaseItem> items, User user);

        /// <summary>
        /// Gets the collections folder.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>Folder.</returns>
        Folder GetCollectionsFolder(string userId);

        /// <summary>
        /// Gets the collections.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable&lt;BoxSet&gt;.</returns>
        IEnumerable<BoxSet> GetCollections(User user);
    }
}
