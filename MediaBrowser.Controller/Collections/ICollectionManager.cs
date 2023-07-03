#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        event EventHandler<CollectionCreatedEventArgs>? CollectionCreated;

        /// <summary>
        /// Occurs when [items added to collection].
        /// </summary>
        event EventHandler<CollectionModifiedEventArgs>? ItemsAddedToCollection;

        /// <summary>
        /// Occurs when [items removed from collection].
        /// </summary>
        event EventHandler<CollectionModifiedEventArgs>? ItemsRemovedFromCollection;

        /// <summary>
        /// Creates the collection.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>BoxSet wrapped in an awaitable task.</returns>
        Task<BoxSet> CreateCollectionAsync(CollectionCreationOptions options);

        /// <summary>
        /// Adds to collection.
        /// </summary>
        /// <param name="collectionId">The collection identifier.</param>
        /// <param name="itemIds">The item ids.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        Task AddToCollectionAsync(Guid collectionId, IEnumerable<Guid> itemIds);

        /// <summary>
        /// Removes from collection.
        /// </summary>
        /// <param name="collectionId">The collection identifier.</param>
        /// <param name="itemIds">The item ids.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task RemoveFromCollectionAsync(Guid collectionId, IEnumerable<Guid> itemIds);

        /// <summary>
        /// Collapses the items within box sets.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        IEnumerable<BaseItem> CollapseItemsWithinBoxSets(IEnumerable<BaseItem> items, User user);

        /// <summary>
        /// Gets the folder where collections are stored.
        /// </summary>
        /// <param name="createIfNeeded">Will create the collection folder on the storage if set to true.</param>
        /// <returns>The folder instance referencing the collection storage.</returns>
        Task<Folder?> GetCollectionsFolder(bool createIfNeeded);
    }
}
