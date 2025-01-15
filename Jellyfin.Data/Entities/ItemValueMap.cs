using System;
using System.Collections.Generic;

namespace Jellyfin.Data.Entities;

/// <summary>
/// Mapping table for the ItemValue BaseItem relation.
/// </summary>
public class ItemValueMap
{
    /// <summary>
    /// Gets or Sets the ItemId.
    /// </summary>
    public required Guid ItemId { get; set; }

    /// <summary>
    /// Gets or Sets the ItemValueId.
    /// </summary>
    public required Guid ItemValueId { get; set; }

    /// <summary>
    /// Gets or Sets the referenced <see cref="BaseItemEntity"/>.
    /// </summary>
    public required BaseItemEntity Item { get; set; }

    /// <summary>
    /// Gets or Sets the referenced <see cref="ItemValue"/>.
    /// </summary>
    public required ItemValue ItemValue { get; set; }
}
