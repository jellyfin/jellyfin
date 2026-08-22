#pragma warning disable CA2227 // Collection properties should be read only

using System;
using System.Collections.Generic;

namespace Jellyfin.Database.Implementations.Entities;

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
    /// Gets or Sets the sanitized Name, the key a person is identified by.
    /// </summary>
    /// <remarks>
    /// Matches <see cref="ItemValue.CleanValue"/> and BaseItemEntity.CleanName.
    /// </remarks>
    public required string CleanName { get; set; }

    /// <summary>
    /// Gets or Sets the id of the Person item this credit belongs to.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or Sets the Type.
    /// </summary>
    public string? PersonType { get; set; }

    /// <summary>
    /// Gets or Sets the mapping of People to BaseItems.
    /// </summary>
    public ICollection<PeopleBaseItemMap>? BaseItems { get; set; }
}
