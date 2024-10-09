using System;
using System.Collections.Generic;

namespace Jellyfin.Data.Entities;
#pragma warning disable CA2227
/// <summary>
/// Enum TrailerTypes.
/// </summary>
public class BaseItemTrailerType
{
    /// <summary>
    /// Gets or Sets Numerical ID of this enumeratable.
    /// </summary>
    public required int Id { get; set; }

    /// <summary>
    /// Gets or Sets all referenced <see cref="BaseItemEntity"/>.
    /// </summary>
    public required Guid ItemId { get; set; }

    /// <summary>
    /// Gets or Sets all referenced <see cref="BaseItemEntity"/>.
    /// </summary>
    public required BaseItemEntity Item { get; set; }
}
