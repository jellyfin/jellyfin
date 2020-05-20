using System;
using System.Collections.Generic;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;

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
        BoxSet CreateCollection(CollectionCreationOptions options);

        /// <summary>
        /// Adds to collection.
        /// </summary>
        /// <param name="collectionId">The collection identifier.</param>
        /// <param name="itemIds">The item ids.</param>
        void AddToCollection(Guid collectionId, IEnumerable<string> itemIds);

        /// <summary>
        /// Removes from collection.
        /// </summary>
        /// <param name="collectionId">The collection identifier.</param>
        /// <param name="itemIds">The item ids.</param>
        void RemoveFromCollection(Guid collectionId, IEnumerable<string> itemIds);

        void AddToCollection(Guid collectionId, IEnumerable<Guid> itemIds);
        void RemoveFromCollection(Guid collectionId, IEnumerable<Guid> itemIds);

        /// <summary>
        /// Collapses the items within box sets.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        IEnumerable<BaseItem> CollapseItemsWithinBoxSets(IEnumerable<BaseItem> items, User user);
    }
}
