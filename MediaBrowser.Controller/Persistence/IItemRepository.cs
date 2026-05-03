#nullable disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Persistence;

/// <summary>
/// Provides an interface to implement an Item repository.
/// </summary>
public interface IItemRepository
{
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
    /// Checks if an item has been persisted to the database.
    /// </summary>
    /// <param name="id">The id to check.</param>
    /// <returns>True if the item exists, otherwise false.</returns>
    Task<bool> ItemExistsAsync(Guid id);

    /// <summary>
    /// Gets genres with item counts.
    /// </summary>
    /// <param name="filter">The query filter.</param>
    /// <returns>The genres and their item counts.</returns>
    QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetGenres(InternalItemsQuery filter);

    /// <summary>
    /// Gets music genres with item counts.
    /// </summary>
    /// <param name="filter">The query filter.</param>
    /// <returns>The music genres and their item counts.</returns>
    QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetMusicGenres(InternalItemsQuery filter);

    /// <summary>
    /// Gets studios with item counts.
    /// </summary>
    /// <param name="filter">The query filter.</param>
    /// <returns>The studios and their item counts.</returns>
    QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetStudios(InternalItemsQuery filter);

    /// <summary>
    /// Gets artists with item counts.
    /// </summary>
    /// <param name="filter">The query filter.</param>
    /// <returns>The artists and their item counts.</returns>
    QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetArtists(InternalItemsQuery filter);

    /// <summary>
    /// Gets album artists with item counts.
    /// </summary>
    /// <param name="filter">The query filter.</param>
    /// <returns>The album artists and their item counts.</returns>
    QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetAlbumArtists(InternalItemsQuery filter);

    /// <summary>
    /// Gets all artists with item counts.
    /// </summary>
    /// <param name="filter">The query filter.</param>
    /// <returns>All artists and their item counts.</returns>
    QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetAllArtists(InternalItemsQuery filter);

    /// <summary>
    /// Gets all music genre names.
    /// </summary>
    /// <returns>The list of music genre names.</returns>
    IReadOnlyList<string> GetMusicGenreNames();

    /// <summary>
    /// Gets all studio names.
    /// </summary>
    /// <returns>The list of studio names.</returns>
    IReadOnlyList<string> GetStudioNames();

    /// <summary>
    /// Gets all genre names.
    /// </summary>
    /// <returns>The list of genre names.</returns>
    IReadOnlyList<string> GetGenreNames();

    /// <summary>
    /// Gets all artist names.
    /// </summary>
    /// <returns>The list of artist names.</returns>
    IReadOnlyList<string> GetAllArtistNames();

    /// <summary>
    /// Gets legacy query filters aggregated from the database.
    /// </summary>
    /// <param name="filter">The query filter.</param>
    /// <returns>Aggregated filter values.</returns>
    QueryFiltersLegacy GetQueryFiltersLegacy(InternalItemsQuery filter);

    /// <summary>
    /// Gets whether all children of the requested item have been played.
    /// </summary>
    /// <param name="user">The user to check against.</param>
    /// <param name="id">The top item id to check.</param>
    /// <param name="recursive">Whether the check should be done recursively.</param>
    /// <returns>A value indicating whether all children have been played.</returns>
    bool GetIsPlayed(User user, Guid id, bool recursive);
}
