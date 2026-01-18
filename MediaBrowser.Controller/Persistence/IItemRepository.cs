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
    /// Gets all artist matches from the db.
    /// </summary>
    /// <param name="artistNames">The names of the artists.</param>
    /// <returns>A map of the artist name and the potential matches.</returns>
    IReadOnlyDictionary<string, MusicArtist[]> FindArtists(IReadOnlyList<string> artistNames);
}
