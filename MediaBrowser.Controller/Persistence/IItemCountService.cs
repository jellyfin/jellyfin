using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.Persistence;

/// <summary>
/// Provides item counting and played-status query operations.
/// </summary>
public interface IItemCountService
{
    /// <summary>
    /// Gets the count of items matching the filter.
    /// </summary>
    /// <param name="filter">The query filter.</param>
    /// <returns>The item count.</returns>
    int GetCount(InternalItemsQuery filter);

    /// <summary>
    /// Gets item counts grouped by type.
    /// </summary>
    /// <param name="filter">The query filter.</param>
    /// <returns>The item counts by type.</returns>
    ItemCounts GetItemCounts(InternalItemsQuery filter);

    /// <summary>
    /// Gets item counts for a "by-name" item using an optimized query.
    /// </summary>
    /// <param name="kind">The kind of the name item.</param>
    /// <param name="id">The ID of the name item.</param>
    /// <param name="relatedItemKinds">The item kinds to count.</param>
    /// <param name="accessFilter">A pre-configured query with user access filtering settings.</param>
    /// <returns>The item counts grouped by type.</returns>
    ItemCounts GetItemCountsForNameItem(BaseItemKind kind, Guid id, BaseItemKind[] relatedItemKinds, InternalItemsQuery accessFilter);

    /// <summary>
    /// Gets the count of played items that are descendants of the specified ancestor.
    /// </summary>
    /// <param name="filter">The query filter containing user access settings.</param>
    /// <param name="ancestorId">The ancestor item id.</param>
    /// <returns>The count of played descendant items.</returns>
    int GetPlayedCount(InternalItemsQuery filter, Guid ancestorId);

    /// <summary>
    /// Gets the total count of items that are descendants of the specified ancestor.
    /// </summary>
    /// <param name="filter">The query filter containing user access settings.</param>
    /// <param name="ancestorId">The ancestor item id.</param>
    /// <returns>The total count of descendant items.</returns>
    int GetTotalCount(InternalItemsQuery filter, Guid ancestorId);

    /// <summary>
    /// Gets both the played count and total count of descendant items.
    /// </summary>
    /// <param name="filter">The query filter containing user access settings.</param>
    /// <param name="ancestorId">The ancestor item id.</param>
    /// <returns>A tuple containing (Played count, Total count).</returns>
    (int Played, int Total) GetPlayedAndTotalCount(InternalItemsQuery filter, Guid ancestorId);

    /// <summary>
    /// Gets both the played count and total count from linked children.
    /// </summary>
    /// <param name="filter">The query filter containing user access settings.</param>
    /// <param name="parentId">The parent item id.</param>
    /// <returns>A tuple containing (Played count, Total count).</returns>
    (int Played, int Total) GetPlayedAndTotalCountFromLinkedChildren(InternalItemsQuery filter, Guid parentId);

    /// <summary>
    /// Batch-fetches played and total counts for multiple folder items.
    /// </summary>
    /// <param name="folderIds">The list of folder item IDs to get counts for.</param>
    /// <param name="user">The user for access filtering and played status.</param>
    /// <returns>Dictionary mapping folder ID to (Played count, Total count).</returns>
    Dictionary<Guid, (int Played, int Total)> GetPlayedAndTotalCountBatch(IReadOnlyList<Guid> folderIds, User user);

    /// <summary>
    /// Batch-fetches child counts for multiple parent folders.
    /// </summary>
    /// <param name="parentIds">The list of parent folder IDs.</param>
    /// <param name="userId">The user ID for access filtering.</param>
    /// <returns>Dictionary mapping parent ID to child count.</returns>
    Dictionary<Guid, int> GetChildCountBatch(IReadOnlyList<Guid> parentIds, Guid? userId);
}
