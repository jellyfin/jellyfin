#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
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
    /// Deletes the item.
    /// </summary>
    /// <param name="id">The identifier.</param>
    void DeleteItem(Guid id);

    /// <summary>
    /// Saves the items.
    /// </summary>
    /// <param name="items">The items.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    void SaveItems(IReadOnlyList<BaseItem> items, CancellationToken cancellationToken);

    void SaveImages(BaseItem item);

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
    /// Updates the inherited values.
    /// </summary>
    void UpdateInheritedValues();

    int GetCount(InternalItemsQuery filter);

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
}
