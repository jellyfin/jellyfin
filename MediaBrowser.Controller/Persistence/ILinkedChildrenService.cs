using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities.Audio;
using LinkedChildType = MediaBrowser.Controller.Entities.LinkedChildType;

namespace MediaBrowser.Controller.Persistence;

/// <summary>
/// Provides linked children query and manipulation operations.
/// </summary>
public interface ILinkedChildrenService
{
    /// <summary>
    /// Gets the IDs of linked children for the specified parent.
    /// </summary>
    /// <param name="parentId">The parent item ID.</param>
    /// <param name="childType">Optional child type filter.</param>
    /// <returns>List of child item IDs.</returns>
    IReadOnlyList<Guid> GetLinkedChildrenIds(Guid parentId, int? childType = null);

    /// <summary>
    /// Gets the distinct parent IDs that have linked children of the specified type.
    /// </summary>
    /// <param name="childType">The child type to filter by.</param>
    /// <returns>List of distinct parent item IDs.</returns>
    IReadOnlyList<Guid> GetParentIdsWithChildType(LinkedChildType childType);

    /// <summary>
    /// Gets, in a single query, the subset of the supplied items that own at least one alternate
    /// version (local or linked). Items absent from the result have no alternate versions, so their
    /// media source count is one.
    /// </summary>
    /// <param name="itemIds">The item IDs to check.</param>
    /// <returns>The set of item IDs that have alternate versions.</returns>
    IReadOnlySet<Guid> GetItemIdsWithAlternateVersions(IReadOnlyList<Guid> itemIds);

    /// <summary>
    /// Gets all artist matches from the database.
    /// </summary>
    /// <param name="artistNames">The names of the artists.</param>
    /// <returns>A map of the artist name and the potential matches.</returns>
    IReadOnlyDictionary<string, MusicArtist[]> FindArtists(IReadOnlyList<string> artistNames);

    /// <summary>
    /// Gets parent IDs that reference the specified child with LinkedChildType.Manual.
    /// </summary>
    /// <param name="childId">The child item ID.</param>
    /// <param name="parentType">Optional parent item type filter.</param>
    /// <returns>List of parent IDs that reference the child.</returns>
    IReadOnlyList<Guid> GetManualLinkedParentIds(Guid childId, BaseItemKind? parentType = null);

    /// <summary>
    /// Updates LinkedChildren references from one child to another.
    /// </summary>
    /// <param name="fromChildId">The child ID to re-route from.</param>
    /// <param name="toChildId">The child ID to re-route to.</param>
    /// <returns>List of parent item IDs whose LinkedChildren were modified.</returns>
    IReadOnlyList<Guid> RerouteLinkedChildren(Guid fromChildId, Guid toChildId);

    /// <summary>
    /// Creates or updates a LinkedChild entry.
    /// </summary>
    /// <param name="parentId">The parent item ID.</param>
    /// <param name="childId">The child item ID.</param>
    /// <param name="childType">The type of linked child relationship.</param>
    void UpsertLinkedChild(Guid parentId, Guid childId, LinkedChildType childType);

    /// <summary>
    /// Gets every recorded pair of items the user split apart, as an item to excluded items map.
    /// The relationship is symmetric, so both items of a pair are present as a key.
    /// </summary>
    /// <returns>A map of item ID to the IDs it must not be auto-merged with.</returns>
    IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> GetAutoMergeExclusions();

    /// <summary>
    /// Records that the given items must never be auto-merged with <paramref name="itemId"/> again.
    /// Pairs that are already recorded are left untouched.
    /// </summary>
    /// <param name="itemId">The item the user split the others away from.</param>
    /// <param name="excludedItemIds">The items that were split away.</param>
    void AddAutoMergeExclusions(Guid itemId, IReadOnlyList<Guid> excludedItemIds);

    /// <summary>
    /// Drops the recorded exclusions between the given items, re-allowing them to be auto-merged.
    /// </summary>
    /// <param name="itemIds">The items whose mutual exclusions are dropped.</param>
    void RemoveAutoMergeExclusions(IReadOnlyList<Guid> itemIds);
}
