#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
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
        /// Sets whether items in the collection are hidden from the main library.
        /// </summary>
        /// <param name="collectionId">The collection identifier.</param>
        /// <param name="hide">Whether to hide collection members from the main library.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SetHideItemsFromLibraryAsync(Guid collectionId, bool hide);

        /// <summary>
        /// Collapses the items within box sets.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        IEnumerable<BaseItem> CollapseItemsWithinBoxSets(IEnumerable<BaseItem> items, User user);

        /// <summary>
        /// Excludes items that belong to a collection with hide-from-library enabled.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns>Items that are not hidden by a collection.</returns>
        IEnumerable<BaseItem> ExcludeItemsHiddenByCollections(IEnumerable<BaseItem> items);

        /// <summary>
        /// Gets the collections accessible to the supplied user that contain the provided item.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="itemId">The item identifier.</param>
        /// <returns>The collections containing the item.</returns>
        IEnumerable<BoxSet> GetCollectionsContainingItem(User user, Guid itemId);

        /// <summary>
        /// Gets the folder where collections are stored.
        /// </summary>
        /// <param name="createIfNeeded">Will create the collection folder on the storage if set to true.</param>
        /// <returns>The folder instance referencing the collection storage.</returns>
        Task<Folder?> GetCollectionsFolder(bool createIfNeeded);
    }
}
