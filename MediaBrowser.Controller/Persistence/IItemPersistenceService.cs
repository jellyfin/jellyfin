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
    /// Saves items to the database.
    /// </summary>
    /// <param name="items">The items to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    void SaveItems(IReadOnlyList<BaseItem> items, CancellationToken cancellationToken);

    /// <summary>
    /// Saves items to the database, leaving out the ones the database refuses. Unlike
    /// <see cref="SaveItems(IReadOnlyList{BaseItem}, CancellationToken)"/>, which saves all of the items or none, a batch
    /// that fails because the database refused the data of some of its items is saved again item by item, and only
    /// those items, and the items that belong to them, are left out. Each item is saved whole or not at all. Errors
    /// that do not come from the data of single items, such as a lost connection, are thrown as they are; when one
    /// ends the save part way, the items saved before it stay saved.
    /// </summary>
    /// <param name="items">The items to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ids of the items that were not saved, which is empty when all of them were.</returns>
    IReadOnlyCollection<Guid> SaveItemsSkippingFailures(IReadOnlyList<BaseItem> items, CancellationToken cancellationToken)
    {
        SaveItems(items, cancellationToken);
        return [];
    }

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
