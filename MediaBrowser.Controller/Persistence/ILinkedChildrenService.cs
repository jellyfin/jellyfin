using System;
using System.Collections.Generic;
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
    /// Gets all artist matches from the database.
    /// </summary>
    /// <param name="artistNames">The names of the artists.</param>
    /// <returns>A map of the artist name and the potential matches.</returns>
    IReadOnlyDictionary<string, MusicArtist[]> FindArtists(IReadOnlyList<string> artistNames);

    /// <summary>
    /// Gets parent IDs that reference the specified child with LinkedChildType.Manual.
    /// </summary>
    /// <param name="childId">The child item ID.</param>
    /// <returns>List of parent IDs that reference the child.</returns>
    IReadOnlyList<Guid> GetManualLinkedParentIds(Guid childId);

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
}
