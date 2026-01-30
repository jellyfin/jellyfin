#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Persistence;

/// <summary>
/// Provides an interface to implement an Item repository.
/// </summary>
public interface IItemRepository
{
    /// <summary>
    /// Deletes the item.
    /// </summary>
    /// <param name="ids">The identifier to delete.</param>
    void DeleteItem(params IReadOnlyList<Guid> ids);

    /// <summary>
    /// Saves the items.
    /// </summary>
    /// <param name="items">The items.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    void SaveItems(IReadOnlyList<BaseItem> items, CancellationToken cancellationToken);

    Task SaveImagesAsync(BaseItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reattaches the user data to the item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous reattachment operation.</returns>
    Task ReattachUserDataAsync(BaseItem item, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the item.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>BaseItem.</returns>
    BaseItem RetrieveItem(Guid id);

    /// <summary>
    /// Gets the items.
    /// </summary>
    /// <param name="filter">The query.</param>
    /// <returns>QueryResult&lt;BaseItem&gt;.</returns>
    QueryResult<BaseItem> GetItems(InternalItemsQuery filter);

    /// <summary>
    /// Gets the item ids list.
    /// </summary>
    /// <param name="filter">The query.</param>
    /// <returns>List&lt;Guid&gt;.</returns>
    IReadOnlyList<Guid> GetItemIdsList(InternalItemsQuery filter);

    /// <summary>
    /// Gets the item list.
    /// </summary>
    /// <param name="filter">The query.</param>
    /// <returns>List&lt;BaseItem&gt;.</returns>
    IReadOnlyList<BaseItem> GetItemList(InternalItemsQuery filter);

    /// <summary>
    /// Gets the item list. Used mainly by the Latest api endpoint.
    /// </summary>
    /// <param name="filter">The query.</param>
    /// <param name="collectionType">Collection Type.</param>
    /// <returns>List&lt;BaseItem&gt;.</returns>
    IReadOnlyList<BaseItem> GetLatestItemList(InternalItemsQuery filter, CollectionType collectionType);

    /// <summary>
    /// Gets the list of series presentation keys for next up.
    /// </summary>
    /// <param name="filter">The query.</param>
    /// <param name="dateCutoff">The minimum date for a series to have been most recently watched.</param>
    /// <returns>The list of keys.</returns>
    IReadOnlyList<string> GetNextUpSeriesKeys(InternalItemsQuery filter, DateTime dateCutoff);

    /// <summary>
    /// Gets next up episodes for multiple series in a single batched query.
    /// Returns the last watched episode, next unwatched episode, specials, and next played episode for each series.
    /// </summary>
    /// <param name="filter">The query filter.</param>
    /// <param name="seriesKeys">The series presentation unique keys to query.</param>
    /// <param name="includeSpecials">Whether to include specials (ParentIndexNumber = 0) in the results.</param>
    /// <param name="includeWatchedForRewatching">Whether to include watched episodes for rewatching mode.</param>
    /// <returns>A dictionary mapping series key to batch result containing episodes needed for NextUp calculation.</returns>
    IReadOnlyDictionary<string, NextUpEpisodeBatchResult> GetNextUpEpisodesBatch(
        InternalItemsQuery filter,
        IReadOnlyList<string> seriesKeys,
        bool includeSpecials,
        bool includeWatchedForRewatching);

    /// <summary>
    /// Updates the inherited values.
    /// </summary>
    void UpdateInheritedValues();

    int GetCount(InternalItemsQuery filter);

    ItemCounts GetItemCounts(InternalItemsQuery filter);

    QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetGenres(InternalItemsQuery filter);

    QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetMusicGenres(InternalItemsQuery filter);

    QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetStudios(InternalItemsQuery filter);

    QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetArtists(InternalItemsQuery filter);

    QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetAlbumArtists(InternalItemsQuery filter);

    QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetAllArtists(InternalItemsQuery filter);

    IReadOnlyList<string> GetMusicGenreNames();

    IReadOnlyList<string> GetStudioNames();

    IReadOnlyList<string> GetGenreNames();

    IReadOnlyList<string> GetAllArtistNames();

    /// <summary>
    /// Checks if an item has been persisted to the database.
    /// </summary>
    /// <param name="id">The id to check.</param>
    /// <returns>True if the item exists, otherwise false.</returns>
    Task<bool> ItemExistsAsync(Guid id);

    /// <summary>
    /// Gets a value indicating wherever all children of the requested Id has been played.
    /// </summary>
    /// <param name="user">The userdata to check against.</param>
    /// <param name="id">The Top id to check.</param>
    /// <param name="recursive">Whever the check should be done recursive. Warning expensive operation.</param>
    /// <returns>A value indicating whever all children has been played.</returns>
    bool GetIsPlayed(User user, Guid id, bool recursive);

    /// <summary>
    /// Gets the count of played items that are descendants of the specified ancestor.
    /// Uses the AncestorIds table for efficient recursive lookup.
    /// Applies user access filtering (library access, parental controls, tags).
    /// </summary>
    /// <param name="filter">The query filter containing user access settings.</param>
    /// <param name="ancestorId">The ancestor item id.</param>
    /// <returns>The count of played descendant items.</returns>
    int GetPlayedCount(InternalItemsQuery filter, Guid ancestorId);

    /// <summary>
    /// Gets the total count of items that are descendants of the specified ancestor.
    /// Uses the AncestorIds table for efficient recursive lookup.
    /// Applies user access filtering (library access, parental controls, tags).
    /// </summary>
    /// <param name="filter">The query filter containing user access settings.</param>
    /// <param name="ancestorId">The ancestor item id.</param>
    /// <returns>The total count of descendant items.</returns>
    int GetTotalCount(InternalItemsQuery filter, Guid ancestorId);

    /// <summary>
    /// Gets both the played count and total count of items that are descendants of the specified ancestor.
    /// Uses the AncestorIds table for efficient recursive lookup.
    /// Applies user access filtering (library access, parental controls, tags).
    /// </summary>
    /// <param name="filter">The query filter containing user access settings.</param>
    /// <param name="ancestorId">The ancestor item id.</param>
    /// <returns>A tuple containing (Played count, Total count).</returns>
    (int Played, int Total) GetPlayedAndTotalCount(InternalItemsQuery filter, Guid ancestorId);

    /// <summary>
    /// Gets both the played count and total count of items that are linked children of the specified parent.
    /// Uses the LinkedChildren table for BoxSets, Playlists, etc.
    /// Applies user access filtering (library access, parental controls, tags).
    /// </summary>
    /// <param name="filter">The query filter containing user access settings.</param>
    /// <param name="parentId">The parent item id (BoxSet, Playlist, etc.).</param>
    /// <returns>A tuple containing (Played count, Total count).</returns>
    (int Played, int Total) GetPlayedAndTotalCountFromLinkedChildren(InternalItemsQuery filter, Guid parentId);

    /// <summary>
    /// Gets the IDs of linked children for the specified parent.
    /// </summary>
    /// <param name="parentId">The parent item ID.</param>
    /// <param name="childType">Optional child type filter (e.g., LocalAlternateVersion, LinkedAlternateVersion).</param>
    /// <returns>List of child item IDs.</returns>
    IReadOnlyList<Guid> GetLinkedChildrenIds(Guid parentId, int? childType = null);

    /// <summary>
    /// Gets all artist matches from the db.
    /// </summary>
    /// <param name="artistNames">The names of the artists.</param>
    /// <returns>A map of the artist name and the potential matches.</returns>
    IReadOnlyDictionary<string, MusicArtist[]> FindArtists(IReadOnlyList<string> artistNames);

    /// <summary>
    /// Batch-fetches child counts for multiple parent folders.
    /// Returns the count of immediate children (non-recursive) for each parent.
    /// </summary>
    /// <param name="parentIds">The list of parent folder IDs.</param>
    /// <param name="userId">The user ID for access filtering.</param>
    /// <returns>Dictionary mapping parent ID to child count.</returns>
    Dictionary<Guid, int> GetChildCountBatch(IReadOnlyList<Guid> parentIds, Guid? userId);

    /// <summary>
    /// Gets parent IDs (Playlists/BoxSets) that reference the specified child with LinkedChildType.Manual.
    /// </summary>
    /// <param name="childId">The child item ID.</param>
    /// <returns>List of parent IDs that reference the child.</returns>
    IReadOnlyList<Guid> GetManualLinkedParentIds(Guid childId);

    /// <summary>
    /// Updates LinkedChildren references from one child to another, preserving SortOrder.
    /// Handles duplicates: if parent already references toChildId, removes the old reference instead.
    /// Used when video versions change to maintain collection integrity.
    /// </summary>
    /// <param name="fromChildId">The child ID to re-route from.</param>
    /// <param name="toChildId">The child ID to re-route to.</param>
    /// <returns>Number of references updated.</returns>
    int RerouteLinkedChildren(Guid fromChildId, Guid toChildId);
}
