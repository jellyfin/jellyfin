#pragma warning disable CA2227 // Collection properties should be read only

using System;
using System.Collections.Generic;

namespace Jellyfin.Database.Implementations.Entities;

/// <summary>
/// A company credited on a <see cref="BaseItemEntity"/>: a studio, a network, a label, and so on.
/// </summary>
public class CompanyEntity
{
    /// <summary>
    /// Gets or Sets the CompanyId.
    /// </summary>
    /// <remarks>
    /// This is also the id of the by-name item that carries the company's artwork and metadata.
    /// </remarks>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or Sets the company's name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or Sets the sanitized name used to match a company by name.
    /// </summary>
    public required string CleanName { get; set; }

    /// <summary>
    /// Gets or Sets the mapping of Companies to BaseItems.
    /// </summary>
    public ICollection<CompanyBaseItemMap>? BaseItems { get; set; }
}
