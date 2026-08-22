#nullable disable

using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Persistence;

/// <summary>
/// Provides methods for accessing Peoples.
/// </summary>
public interface IPeopleRepository
{
    /// <summary>
    /// Gets the people.
    /// </summary>
    /// <param name="filter">The query.</param>
    /// <returns>The list of people matching the filter.</returns>
    QueryResult<PersonInfo> GetPeople(InternalPeopleQuery filter);

    /// <summary>
    /// Updates the people.
    /// </summary>
    /// <param name="itemId">The item identifier.</param>
    /// <param name="people">The people.</param>
    void UpdatePeople(Guid itemId, IReadOnlyList<PersonInfo> people);

    /// <summary>
    /// Gets the people names.
    /// </summary>
    /// <param name="filter">The query.</param>
    /// <returns>The list of people names matching the filter.</returns>
    IReadOnlyList<string> GetPeopleNames(InternalPeopleQuery filter);

    /// <summary>
    /// Gets the names of the credits that are not linked to a person item yet.
    /// </summary>
    /// <returns>The list of credited names whose person item is still unknown.</returns>
    IReadOnlyList<string> GetUnlinkedPeopleNames();

    /// <summary>
    /// Links the credits of a name that have no person item yet to the given one.
    /// </summary>
    /// <param name="name">The credited name.</param>
    /// <param name="personItemId">The person item to link them to.</param>
    /// <returns>The number of credits that were linked.</returns>
    int LinkPeopleToItem(string name, Guid personItemId);

    /// <summary>
    /// Gets the distinct people names per item for multiple items efficiently by querying from the mapping table.
    /// </summary>
    /// <param name="itemIds">The item IDs to get people for.</param>
    /// <param name="personTypes">The person types to include (e.g. "Actor", "Director").</param>
    /// <returns>A dictionary mapping each item ID to its distinct people names, ordered by cast list order. Items with no matching people are omitted.</returns>
    IReadOnlyDictionary<Guid, IReadOnlyList<string>> GetPeopleNamesByItems(IReadOnlyList<Guid> itemIds, IReadOnlyList<string> personTypes);

    /// <summary>
    /// Gets the people for multiple items in a single query, keyed by item id.
    /// </summary>
    /// <param name="itemIds">The item IDs to get people for.</param>
    /// <returns>A dictionary mapping each item ID to its people (with role, type and sort order), ordered by cast list order. Items with no people are omitted.</returns>
    IReadOnlyDictionary<Guid, IReadOnlyList<PersonInfo>> GetPeopleByItems(IReadOnlyList<Guid> itemIds);
}
