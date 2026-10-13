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
    /// Gets or Sets <see cref="Name"/> in lower case (invariant culture), which the application stores so that names can
    /// be matched without the database folding case. The value is <c>Name.ToLowerInvariant()</c> and is maintained by the
    /// writers through <c>MatchNormalizer</c>. It is empty until the application has stored it, and a row whose value is
    /// not set is not found by name lookups.
    /// </summary>
    public string NameLower { get; set; } = string.Empty;

    /// <summary>
    /// Gets or Sets the Type.
    /// </summary>
    public string? PersonType { get; set; }

    /// <summary>
    /// Gets or Sets the mapping of People to BaseItems.
    /// </summary>
    public ICollection<PeopleBaseItemMap>? BaseItems { get; set; }
}
