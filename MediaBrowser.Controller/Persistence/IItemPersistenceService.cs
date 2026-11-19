using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Persistence;

/// <summary>
/// Provides item persistence operations (save, delete, update).
/// </summary>
public interface IItemPersistenceService
{
    /// <summary>
    /// Deletes items by their IDs.
    /// </summary>
    /// <param name="ids">The IDs to delete.</param>
    void DeleteItem(params IReadOnlyList<Guid> ids);

    /// <summary>
    /// Writes one provider id, leaving the item's other provider ids alone.
    /// </summary>
    /// <remarks>
    /// Saving an item replaces its provider ids with the set the instance holds, so it is only
    /// correct for an item read with them. This is how to change one without holding the rest.
    /// </remarks>
    /// <param name="itemId">The item to write to.</param>
    /// <param name="name">The provider name.</param>
    /// <param name="value">The provider id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the write.</returns>
    Task UpsertProviderIdAsync(Guid itemId, string name, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes one provider id, leaving the item's other provider ids alone.
    /// </summary>
    /// <param name="itemId">The item to remove from.</param>
    /// <param name="name">The provider name to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the removal.</returns>
    Task RemoveProviderIdAsync(Guid itemId, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes one image, leaving the item's other images alone.
    /// </summary>
    /// <remarks>
    /// An image with an <see cref="ItemImageInfo.Id"/> updates that row; one without is matched on
    /// its type and path, and inserted when neither finds it.
    /// </remarks>
    /// <param name="itemId">The item to write to.</param>
    /// <param name="image">The image to store.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the write.</returns>
    Task UpsertImageAsync(Guid itemId, ItemImageInfo image, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes one image, leaving the item's other images alone.
    /// </summary>
    /// <param name="itemId">The item to remove from.</param>
    /// <param name="image">The image to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the removal.</returns>
    Task RemoveImageAsync(Guid itemId, ItemImageInfo image, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves items to the database.
    /// </summary>
    /// <param name="items">The items to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    void SaveItems(IReadOnlyList<BaseItem> items, CancellationToken cancellationToken);

    /// <summary>
    /// Saves image info for an item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveImagesAsync(BaseItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reattaches user data entries to the correct item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReattachUserDataAsync(BaseItem item, CancellationToken cancellationToken);

    /// <summary>
    /// Updates inherited values.
    /// </summary>
    void UpdateInheritedValues();
}
