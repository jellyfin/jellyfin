using System;
using System.Collections.Generic;

namespace Jellyfin.Data.Entities;

/// <summary>
/// Represents an ItemValue for a BaseItem.
/// </summary>
public class ItemValue
{
    /// <summary>
    /// Gets or Sets the ItemValueId.
    /// </summary>
    public required Guid ItemValueId { get; set; }

    /// <summary>
    /// Gets or Sets the Type.
    /// </summary>
    public required ItemValueType Type { get; set; }

    /// <summary>
    /// Gets or Sets the Value.
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Gets or Sets the sanatised Value.
    /// </summary>
    public required string CleanValue { get; set; }

    /// <summary>
    /// Gets or Sets all associated BaseItems.
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
    public ICollection<ItemValueMap>? BaseItemsMap { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
}
