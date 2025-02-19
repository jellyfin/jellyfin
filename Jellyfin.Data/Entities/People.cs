using System;
using System.Collections.Generic;

namespace Jellyfin.Data.Entities;

/// <summary>
/// People entity.
/// </summary>
public class People
{
    /// <summary>
    /// Gets or Sets the PeopleId.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or Sets the Persons Name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or Sets the Type.
    /// </summary>
    public string? PersonType { get; set; }

    /// <summary>
    /// Gets the mapping of People to BaseItems.
    /// </summary>
    public required ICollection<PeopleBaseItemMap> BaseItems { get; init; }
}
