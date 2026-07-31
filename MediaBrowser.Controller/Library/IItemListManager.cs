using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// Manages per-user named lists and their item membership.
/// </summary>
public interface IItemListManager
{
    /// <summary>
    /// Gets all lists owned by a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The user's lists in list sort order.</returns>
    Task<IReadOnlyList<ItemList>> GetListsAsync(Guid userId);

    /// <summary>
    /// Creates a named item list for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="name">The list name.</param>
    /// <param name="autoRemoveWatched">Whether watched items should be removed automatically.</param>
    /// <returns>The created list.</returns>
    Task<ItemList> CreateListAsync(Guid userId, string name, bool autoRemoveWatched);

    /// <summary>
    /// Updates the supplied properties of a list.
    /// </summary>
    /// <param name="listId">The list identifier.</param>
    /// <param name="name">The new name, or <see langword="null"/> to leave it unchanged.</param>
    /// <param name="sortIndex">The new list sort index, or <see langword="null"/> to leave it unchanged.</param>
    /// <param name="autoRemoveWatched">The new auto-removal setting, or <see langword="null"/> to leave it unchanged.</param>
    /// <returns>A task representing the update operation.</returns>
    Task UpdateListAsync(Guid listId, string? name, int? sortIndex, bool? autoRemoveWatched);

    /// <summary>
    /// Deletes a non-default list.
    /// </summary>
    /// <param name="listId">The list identifier.</param>
    /// <returns>A task representing the delete operation.</returns>
    Task DeleteListAsync(Guid listId);

    /// <summary>
    /// Adds an item to a list.
    /// </summary>
    /// <param name="listId">The list identifier.</param>
    /// <param name="itemId">The item identifier.</param>
    /// <returns>A task representing the add operation.</returns>
    Task AddItemAsync(Guid listId, Guid itemId);

    /// <summary>
    /// Removes an item from a list.
    /// </summary>
    /// <param name="listId">The list identifier.</param>
    /// <param name="itemId">The item identifier.</param>
    /// <returns>A task representing the remove operation.</returns>
    Task RemoveItemAsync(Guid listId, Guid itemId);

    /// <summary>
    /// Moves an item to a new zero-based position within a list.
    /// </summary>
    /// <param name="listId">The list identifier.</param>
    /// <param name="itemId">The item identifier.</param>
    /// <param name="newSortIndex">The new zero-based position.</param>
    /// <returns>A task representing the move operation.</returns>
    Task MoveItemAsync(Guid listId, Guid itemId, int newSortIndex);

    /// <summary>
    /// Gets a user's default watchlist, creating it when necessary.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The user's default watchlist.</returns>
    Task<ItemList> GetOrCreateDefaultListAsync(Guid userId);

    /// <summary>
    /// Gets list membership for a batch of items belonging to a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="itemIds">The item identifiers to resolve.</param>
    /// <returns>A map from each requested item identifier to its list identifiers.</returns>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> GetMembershipAsync(
        Guid userId,
        IReadOnlyList<Guid> itemIds);

    /// <summary>
    /// Gets all item identifiers in a list.
    /// </summary>
    /// <param name="listId">The list identifier.</param>
    /// <returns>A set of item identifiers suitable for constant-time membership checks.</returns>
    Task<IReadOnlySet<Guid>> GetListItemIdsAsync(Guid listId);

    /// <summary>
    /// Gets the date each item was added to the specified list.
    /// </summary>
    /// <param name="listId">The list identifier.</param>
    /// <returns>A map from each item identifier to the date it was added.</returns>
    Task<IReadOnlyDictionary<Guid, DateTime>> GetListItemDatesAsync(Guid listId);

    /// <summary>
    /// The exception thrown when a configured per-item-list or per-list-item cap is reached.
    /// </summary>
    public sealed class ItemListLimitExceededException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemListLimitExceededException"/> class.
        /// </summary>
        public ItemListLimitExceededException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemListLimitExceededException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ItemListLimitExceededException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemListLimitExceededException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that caused the limit failure.</param>
        public ItemListLimitExceededException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// The exception thrown when deletion of a default item list is attempted.
    /// </summary>
    public sealed class DefaultItemListDeletionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultItemListDeletionException"/> class.
        /// </summary>
        public DefaultItemListDeletionException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultItemListDeletionException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DefaultItemListDeletionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultItemListDeletionException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that caused the default-list deletion failure.</param>
        public DefaultItemListDeletionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// The exception thrown when an item list name duplicates another list owned by the same user.
    /// </summary>
    public sealed class DuplicateItemListNameException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateItemListNameException"/> class.
        /// </summary>
        public DuplicateItemListNameException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateItemListNameException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DuplicateItemListNameException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateItemListNameException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that caused the duplicate-name failure.</param>
        public DuplicateItemListNameException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
