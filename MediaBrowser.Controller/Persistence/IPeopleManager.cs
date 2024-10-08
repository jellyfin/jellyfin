#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Persistence;

public interface IPeopleManager
{
    /// <summary>
    /// Gets the people.
    /// </summary>
    /// <param name="filter">The query.</param>
    /// <returns>List&lt;PersonInfo&gt;.</returns>
    IReadOnlyList<PersonInfo> GetPeople(InternalPeopleQuery filter);

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
    /// <returns>List&lt;System.String&gt;.</returns>
    IReadOnlyList<string> GetPeopleNames(InternalPeopleQuery filter);

}
